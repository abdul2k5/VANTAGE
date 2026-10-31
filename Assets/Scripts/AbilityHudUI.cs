using UnityEngine;
using UnityEngine.UI;

public class AbilityHudUI : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public GameObject root;
        public Image border;
        public Image cooldownOverlay;
        public Image icon;
        public Text keyLabel;
        public Text nameLabel;
        /// <summary>Charge pips, top-right of the tile: filled for each charge left, hollow for each spent.</summary>
        public Text chargesLabel;
    }

    public AbilityController controller;
    public Slot[] slots;

    public Color readyColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    public Color notReadyColor = new Color(0.35f, 0.38f, 0.32f, 1f);

    void Update()
    {
        if (controller == null || slots == null) return;

        var unlocked = controller.GetUnlockedAbilities();

        for (int i = 0; i < slots.Length; i++)
        {
            bool active = i < unlocked.Count;
            if (slots[i].root != null) slots[i].root.SetActive(active);
            if (!active) continue;

            var ability = unlocked[i];

            if (slots[i].keyLabel != null) slots[i].keyLabel.text = ability.keyLabel;
            if (slots[i].nameLabel != null) slots[i].nameLabel.text = ability.abilityName.ToUpper();
            if (slots[i].border != null) slots[i].border.color = ability.IsReady ? readyColor : notReadyColor;
            if (slots[i].icon != null && ability.icon != null) slots[i].icon.sprite = ability.icon;

            if (slots[i].chargesLabel != null)
            {
                slots[i].chargesLabel.text = new string('●', ability.Charges) + new string('○', ability.MaxCharges - ability.Charges);
            }

            if (slots[i].cooldownOverlay != null)
            {
                // Out of charges reads as fully greyed out for the rest of the room, not as a cooldown about to finish.
                float frac = ability.Charges <= 0 ? 1f : (ability.Cooldown <= 0f ? 0f : ability.CooldownRemaining / ability.Cooldown);
                slots[i].cooldownOverlay.fillAmount = frac;
                slots[i].cooldownOverlay.enabled = frac > 0f;
            }
        }
    }
}
