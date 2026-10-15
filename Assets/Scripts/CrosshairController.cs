using UnityEngine;

/// <summary>Fixed screen-center crosshair (FPS aim is screen-locked, unlike the old mouse-follow version).</summary>
public class CrosshairController : MonoBehaviour
{
    public static CrosshairController Instance;

    public RectTransform[] styleRoots;

    int currentStyle;
    bool crosshairVisible = true;

    void Awake()
    {
        Instance = this;
        currentStyle = PlayerPrefs.GetInt("VantageCrosshairStyle", 0);
        ApplyStyle();
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
