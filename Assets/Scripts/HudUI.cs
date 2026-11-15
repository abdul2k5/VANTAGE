using UnityEngine;
using UnityEngine.UI;

public class HudUI : MonoBehaviour
{
    public PlayerHealth health;
    public Weapon weapon;

    public RectTransform healthFill;
    public Text healthLabel;
    public Text ammoLabel;
    [Tooltip("Optional - the name of the gun picked for the run.")]
    public Text gunLabel;
    public GameObject gunLabelBacking;
    public RectTransform shieldFill;
    public Text shieldLabel;
    public Color shieldColor = new Color(0.36f, 0.72f, 0.95f, 1f);

    public Color healthyColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    public Color hurtColor = new Color(0.757f, 0.278f, 0.180f, 1f);

    int shownGun = -1;
    float fullWidth;
    float shieldFullWidth;
    Image fillImage;

    void Start()
    {
        if (healthFill != null)
        {
            fullWidth = healthFill.sizeDelta.x;
            fillImage = healthFill.GetComponent<Image>();
        }
        if (shieldFill != null) shieldFullWidth = shieldFill.sizeDelta.x;
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

        if (health != null && shieldFill != null)
        {
            float fraction = health.MaxShield > 0f ? Mathf.Clamp01(health.CurrentShield / health.MaxShield) : 0f;
            shieldFill.sizeDelta = new Vector2(shieldFullWidth * fraction, shieldFill.sizeDelta.y);
            if (shieldLabel != null) shieldLabel.text = Mathf.CeilToInt(health.CurrentShield).ToString();
        }

        if (weapon != null && ammoLabel != null)
        {
            ammoLabel.text = weapon.CurrentAmmo + " / " + weapon.MaxAmmo;
        }

        if (weapon != null && gunLabel != null) UpdateGunLabel();
    }

    /// <summary>The name of the gun picked for the run, above the ammo count.</summary>
    void UpdateGunLabel()
    {
        if (weapon.CurrentGunIndex == shownGun) return;
        shownGun = weapon.CurrentGunIndex;
        bool show = weapon.GunCount > 0;
        gunLabel.gameObject.SetActive(show);
        if (gunLabelBacking != null) gunLabelBacking.SetActive(show);
        gunLabel.text = weapon.GunName(shownGun);
    }
}
