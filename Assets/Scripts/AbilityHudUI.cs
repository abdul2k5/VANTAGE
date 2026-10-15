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

            if (slots[i].cooldownOverlay != null)
            {
                float frac = ability.Cooldown <= 0f ? 0f : ability.CooldownRemaining / ability.Cooldown;
                slots[i].cooldownOverlay.fillAmount = frac;
                slots[i].cooldownOverlay.enabled = frac > 0f;
            }
        }
    }
}
