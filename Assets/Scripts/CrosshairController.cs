using UnityEngine;
using UnityEngine.UI;

/// <summary>Fixed screen-center crosshair (FPS aim is screen-locked, unlike the old mouse-follow version).</summary>
public class CrosshairController : MonoBehaviour
{
    public static CrosshairController Instance;

    public RectTransform[] styleRoots;
    [Tooltip("Scale multiplier applied to the active crosshair at full spread (1 = no widening).")]
    public float maxSpreadScale = 2.2f;

    /// <summary>Index-selected via the settings menu - same pattern as style. Orange (index 0) matches the existing HUD accent color.</summary>
    public static readonly Color[] ColorOptions =
    {
        new Color(0.886f, 0.647f, 0.235f), // orange (default)
        Color.white,
        new Color(1f, 0.25f, 0.25f),       // red
        new Color(0.35f, 1f, 0.45f),       // green
        new Color(0.3f, 0.85f, 1f),        // cyan
    };

    int currentStyle;
    int currentColor;
    bool crosshairVisible = true;
    float spreadFraction01;

    void Awake()
    {
        Instance = this;
        currentStyle = PlayerPrefs.GetInt("VantageCrosshairStyle", 0);
        currentColor = PlayerPrefs.GetInt("VantageCrosshairColor", 0);
        ApplyStyle();
        ApplyColor();
    }

    /// <summary>0 = tight/accurate, 1 = max spread. Widens the active crosshair so the player can see why they missed.</summary>
    public void SetSpreadFraction(float frac01)
    {
        spreadFraction01 = Mathf.Clamp01(frac01);
        float scale = Mathf.Lerp(1f, maxSpreadScale, spreadFraction01);
        for (int i = 0; i < styleRoots.Length; i++)
        {
            if (i == currentStyle) styleRoots[i].localScale = Vector3.one * scale;
        }
    }

    public void SetStyle(int index)
    {
        if (index < 0 || index >= styleRoots.Length) return;
        currentStyle = index;
        PlayerPrefs.SetInt("VantageCrosshairStyle", currentStyle);
        ApplyStyle();
    }

    public void SetCrosshairVisible(bool visible)
    {
        crosshairVisible = visible;
        ApplyStyle();
    }

    public void SetColor(int index)
    {
        if (index < 0 || index >= ColorOptions.Length) return;
        currentColor = index;
        PlayerPrefs.SetInt("VantageCrosshairColor", currentColor);
        ApplyColor();
    }

    void ApplyStyle()
    {
        for (int i = 0; i < styleRoots.Length; i++)
        {
            styleRoots[i].gameObject.SetActive(crosshairVisible && i == currentStyle);
        }
    }

    /// <summary>Applies the chosen color to every bar in every style - so the pick sticks no matter which style is active now or later.</summary>
    void ApplyColor()
    {
        Color c = ColorOptions[currentColor];
        for (int i = 0; i < styleRoots.Length; i++)
        {
            var images = styleRoots[i].GetComponentsInChildren<Image>(true);
            for (int j = 0; j < images.Length; j++) images[j].color = c;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
