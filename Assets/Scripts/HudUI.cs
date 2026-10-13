using UnityEngine;
using UnityEngine.UI;

public class HudUI : MonoBehaviour
{
    public PlayerHealth health;
    public Weapon weapon;

    public RectTransform healthFill;
    public Text healthLabel;
    public Text ammoLabel;

    public Color healthyColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    public Color hurtColor = new Color(0.757f, 0.278f, 0.180f, 1f);

    float fullWidth;
    Image fillImage;

    void Start()
    {
        if (healthFill != null)
        {
            fullWidth = healthFill.sizeDelta.x;
            fillImage = healthFill.GetComponent<Image>();
        }
    }

    void Update()
    {
        if (health != null && healthFill != null)
        {
            float fraction = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
            healthFill.sizeDelta = new Vector2(fullWidth * fraction, healthFill.sizeDelta.y);

            if (fillImage != null)
            {
                fillImage.color = fraction <= 0.34f ? hurtColor : healthyColor;
            }

            if (healthLabel != null)
            {
                healthLabel.text = Mathf.CeilToInt(health.CurrentHealth).ToString();
            }
        }

        if (weapon != null && ammoLabel != null)
        {
            ammoLabel.text = weapon.CurrentAmmo + " / " + weapon.MaxAmmo;
        }
    }
}
