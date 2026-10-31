using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Valorant-style "you just unlocked X" card: icon, name, key, what it does, and one tip tying
/// it back to "utility before you peek". Shown once per ability the first time
/// AbilityController's corresponding unlocked flag flips true, plus one for the ultimate the
/// first time it reaches full charge (or at the start of the last room if it never has).
/// Pauses the game and unlocks the cursor while up - same mechanism as the settings panel -
/// so dismissing with a click doesn't also fire the weapon.
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

    public AbilityController abilities;
    public UltimateController ultimate;
    public RoomManager rooms;

    public CardData reconCard;
    public CardData smokeCard;
    public CardData flashCard;
    public CardData dashCard;
    public CardData ultimateCard;

    bool prevRecon, prevSmoke, prevFlash, prevDash;
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
    }

    void Update()
    {
        PollUnlocks();
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

    void PollUnlocks()
    {
        if (abilities == null || showing) return;

        if (abilities.reconDartUnlocked && !prevRecon) { prevRecon = true; Show(reconCard); return; }
        if (abilities.smokeWallUnlocked && !prevSmoke) { prevSmoke = true; Show(smokeCard); return; }
        if (abilities.flashUnlocked && !prevFlash) { prevFlash = true; Show(flashCard); return; }
        if (abilities.dashUnlocked && !prevDash) { prevDash = true; Show(dashCard); return; }
        // The flags only ever go true. They used to be re-synced to the live unlock state here, so a
        // checkpoint retry (which re-locks Flash/Dash back in Room 5) reset them and those cards
        // showed a second time on reaching their rooms again.
    }

    void PollUltimate()
    {
        if (ultimate == null || ultimateCardShown || showing) return;

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

    void Show(CardData data)
    {
        if (card == null || data == null) return;

        showing = true;
        showingUltimate = data == ultimateCard;
        IsShowingCard = true;
        shownAtRealtime = Time.unscaledTime;
        UpdateDismissHint();
        if (iconImage != null) iconImage.sprite = data.icon;
        if (keyText != null) keyText.text = "[" + data.key + "]";
        if (nameText != null) nameText.text = data.abilityName;
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
