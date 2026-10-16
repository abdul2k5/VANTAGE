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

    /// <summary>Lets other gameplay-input gates (e.g. the settings panel's Escape toggle) know not to act while a card is up.</summary>
    public static bool IsShowingCard { get; private set; }

    void Start()
    {
        if (card != null) card.SetActive(false);

        var dismissButton = card != null ? card.GetComponent<Button>() : null;
        if (dismissButton != null) dismissButton.onClick.AddListener(Dismiss);
    }

    void Update()
    {
        PollUnlocks();
        PollUltimate();

        if (showing)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool dismiss = (kb != null && kb.spaceKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (dismiss) Dismiss();
        }
    }

    void PollUnlocks()
    {
        if (abilities == null || showing) return;

        if (abilities.reconDartUnlocked && !prevRecon) { prevRecon = true; Show(reconCard); return; }
        if (abilities.smokeWallUnlocked && !prevSmoke) { prevSmoke = true; Show(smokeCard); return; }
        if (abilities.flashUnlocked && !prevFlash) { prevFlash = true; Show(flashCard); return; }
        if (abilities.dashUnlocked && !prevDash) { prevDash = true; Show(dashCard); return; }

        prevRecon = abilities.reconDartUnlocked;
        prevSmoke = abilities.smokeWallUnlocked;
        prevFlash = abilities.flashUnlocked;
        prevDash = abilities.dashUnlocked;
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
        IsShowingCard = true;
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
        IsShowingCard = false;
        if (card != null) card.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;

        if (CrosshairController.Instance != null) CrosshairController.Instance.SetCrosshairVisible(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }
}
