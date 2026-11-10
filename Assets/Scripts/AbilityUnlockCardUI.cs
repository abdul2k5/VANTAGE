using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The pop-up card that teaches each room: name, what to do, and one tip. Shown once per run when
/// a room is first entered, after the fade clears. Rooms that introduce an ability show its icon
/// and key, so the room's lesson and the unlock are one card; the rest show the room number. One
/// more card explains the ultimate the first time it reaches full charge (or in the last room if
/// it never has). Pauses the game and unlocks the cursor while up - same mechanism as the settings
/// panel - so dismissing with a click doesn't also fire the weapon.
///
/// Lives on an always-active host (not the card panel itself) - the panel starts disabled, and
/// a disabled GameObject's Update() never runs, which would silently kill the unlock polling.
/// </summary>
public class AbilityUnlockCardUI : MonoBehaviour
{
    [System.Serializable]
    public class CardData
    {
        public string key;
        public string abilityName;
        public string description;
        public string tip;
        public Sprite icon;
    }

    public GameObject card;
    public Image iconImage;
    public Text keyText;
    public Text nameText;
    public Text descriptionText;
    public Text tipText;
    /// <summary>"[SPACE] OR CLICK TO CONTINUE" - hidden during the dismiss grace period, fades in once input is accepted.</summary>
    public Text dismissHint;
    /// <summary>Real seconds (the card pauses the game) before Space/click can close the card. Without this, the
    /// click that fired the ultimate-filling kill - or the next click of a rapid burst - could close the card
    /// on the same frame it opened or a split second later, so it sometimes seemed never to appear at all.</summary>
    public float dismissGraceSeconds = 0.6f;
    public float hintFadeSeconds = 0.25f;

    public UltimateController ultimate;
    public RoomManager rooms;

    [Tooltip("One per room, same order as RoomManager.rooms. Leave the icon empty to show the room number instead.")]
    public CardData[] roomCards;
    public CardData ultimateCard;

    bool[] roomCardShown;
    Text roomNumberText;
    bool ultimateCardShown;
    bool showing;
    bool showingUltimate;
    float shownAtRealtime;

    /// <summary>Lets other gameplay-input gates (e.g. the settings panel's Escape toggle) know not to act while a card is up.</summary>
    public static bool IsShowingCard { get; private set; }

    public static AbilityUnlockCardUI Instance { get; private set; }

    /// <summary>True once the ultimate's card has been shown this run. UltimateHudUI checks this to skip its
    /// "READY" banner on the first charge-up, when the card already says the same thing in full.</summary>
    public bool UltimateCardShown { get { return ultimateCardShown; } }

    /// <summary>True while the ultimate's card is the one on screen. Needed alongside UltimateCardShown because
    /// if this script updates before UltimateHudUI on the charge-up frame, the card is already open (and
    /// UltimateCardShown already true) by the time the HUD checks.</summary>
    public bool IsShowingUltimateCard { get { return showing && showingUltimate; } }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        IsShowingCard = false;
    }

    void Start()
    {
        if (card != null) card.SetActive(false);

        var dismissButton = card != null ? card.GetComponent<Button>() : null;
        if (dismissButton != null) dismissButton.onClick.AddListener(TryDismiss);

        roomCardShown = new bool[roomCards != null ? roomCards.Length : 0];
        BuildRoomNumberText();
    }

    /// <summary>Sits in the icon's frame for rooms with no ability to show.</summary>
    void BuildRoomNumberText()
    {
        if (iconImage == null || nameText == null) return;

        var go = new GameObject("RoomNumberText", typeof(RectTransform));
        go.transform.SetParent(iconImage.transform.parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        roomNumberText = go.AddComponent<Text>();
        roomNumberText.font = nameText.font;
        roomNumberText.fontSize = 84;
        roomNumberText.fontStyle = FontStyle.Bold;
        roomNumberText.alignment = TextAnchor.MiddleCenter;
        roomNumberText.color = iconImage.color;
        roomNumberText.raycastTarget = false;
        go.SetActive(false);
    }

    void Update()
    {
        PollRoomCard();
        PollUltimate();

        if (showing)
        {
            UpdateDismissHint();

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool dismiss = (kb != null && kb.spaceKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (dismiss) TryDismiss();
        }
    }

    bool CanDismiss { get { return Time.unscaledTime - shownAtRealtime >= dismissGraceSeconds; } }

    void TryDismiss()
    {
        if (showing && CanDismiss) Dismiss();
    }

    void UpdateDismissHint()
    {
        if (dismissHint == null) return;
        float sinceReady = Time.unscaledTime - shownAtRealtime - dismissGraceSeconds;
        Color c = dismissHint.color;
        c.a = hintFadeSeconds > 0f ? Mathf.Clamp01(sinceReady / hintFadeSeconds) : (sinceReady >= 0f ? 1f : 0f);
        dismissHint.color = c;
    }

    /// <summary>Waits for the fade to clear so the room is visible behind the card, and for any other pause to end -
    /// dismissing sets timeScale back to 1, which would unfreeze the settings panel or Game Over underneath.</summary>
    void PollRoomCard()
    {
        if (rooms == null || showing || roomCardShown == null) return;
        if (MainMenuController.IsOpen || rooms.IsTransitioning || Time.timeScale == 0f) return;

        int room = rooms.CurrentRoom;
        if (room < 0 || room >= roomCardShown.Length || roomCardShown[room]) return;

        // Stays true for the whole run, so a checkpoint retry doesn't make the player re-read a card.
        roomCardShown[room] = true;
        Show(roomCards[room], room + 1);
    }

    void PollUltimate()
    {
        if (ultimate == null || ultimateCardShown || showing) return;
        // The room's own card goes first - otherwise the fallback below opens during the fade into the last room.
        if (rooms != null && roomCardShown != null && rooms.CurrentRoom >= 0 && rooms.CurrentRoom < roomCardShown.Length && !roomCardShown[rooms.CurrentRoom]) return;

        if (ultimate.IsReady)
        {
            ultimateCardShown = true;
            Show(ultimateCard);
            return;
        }

        // Last room ("The Round") is the final chance to see this - force it even if the
        // player never filled the ultimate naturally before reaching it.
        if (rooms != null && rooms.CurrentRoom == rooms.rooms.Length - 1)
        {
            ultimateCardShown = true;
            Show(ultimateCard);
        }
    }

    void Show(CardData data, int roomNumber = 0)
    {
        if (card == null || data == null) return;

        showing = true;
        showingUltimate = data == ultimateCard;
        IsShowingCard = true;
        shownAtRealtime = Time.unscaledTime;
        UpdateDismissHint();
        bool hasIcon = data.icon != null;
        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.gameObject.SetActive(hasIcon);
        }
        if (roomNumberText != null)
        {
            roomNumberText.text = roomNumber.ToString();
            roomNumberText.gameObject.SetActive(!hasIcon);
        }
        // The ultimate's card usually opens because it just charged, so say so. It can also be forced
        // open in the last room before charging, and then it stays a plain explainer.
        bool ultReady = showingUltimate && ultimate != null && ultimate.IsReady;
        if (keyText != null) keyText.text = string.IsNullOrEmpty(data.key) ? "" : (ultReady ? "PRESS [" + data.key + "]" : "[" + data.key + "]");
        if (nameText != null) nameText.text = ultReady ? data.abilityName + " READY" : data.abilityName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (tipText != null) tipText.text = data.tip;

        card.transform.SetAsLastSibling();
        card.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;

        if (CrosshairController.Instance != null) CrosshairController.Instance.SetCrosshairVisible(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }

    public void Dismiss()
    {
        showing = false;
        showingUltimate = false;
        IsShowingCard = false;
        if (card != null) card.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;

        if (CrosshairController.Instance != null) CrosshairController.Instance.SetCrosshairVisible(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }
}
