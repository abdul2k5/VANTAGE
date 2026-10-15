using UnityEngine;

/// <summary>Fixed screen-center crosshair (FPS aim is screen-locked, unlike the old mouse-follow version).</summary>
public class CrosshairController : MonoBehaviour
{
    public static CrosshairController Instance;

    public RectTransform[] styleRoots;
    [Tooltip("Scale multiplier applied to the active crosshair at full spread (1 = no widening).")]
    public float maxSpreadScale = 2.2f;

    int currentStyle;
    bool crosshairVisible = true;
    float spreadFraction01;

    void Awake()
    {
        Instance = this;
        currentStyle = PlayerPrefs.GetInt("VantageCrosshairStyle", 0);
        ApplyStyle();
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

    void ApplyStyle()
    {
        for (int i = 0; i < styleRoots.Length; i++)
        {
            styleRoots[i].gameObject.SetActive(crosshairVisible && i == currentStyle);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
