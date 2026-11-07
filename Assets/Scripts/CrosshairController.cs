using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Fixed screen-center crosshair (FPS aim is screen-locked, unlike the old mouse-follow version).</summary>
public class CrosshairController : MonoBehaviour
{
    public static CrosshairController Instance;

    public RectTransform[] styleRoots;
    [Tooltip("Optional small mirror of styleRoots shown as a live preview inside the settings menu. Same index order as styleRoots. Always visible at the chosen style/color/size/gap - never affected by gameplay spread or crosshairVisible.")]
    public RectTransform[] previewStyleRoots;
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
    float sizeMultiplier = 1f;
    float gapMultiplier = 1f;
    bool outlineOn;

    /// <summary>Preview shows the crosshair at 2x its real HUD size. Was a flat 5.2x, which blew a 200% dot up to an 83px blob and its 1px outline to a 10px black square.</summary>
    const float previewZoom = 2f;
    /// <summary>Half-width of the 200x200 preview box minus a margin. At big size+gap settings the zoom shrinks so the whole crosshair still fits.</summary>
    const float previewFitHalfExtent = 88f;
    /// <summary>On-screen outline thickness in the preview, kept constant whatever the zoom.</summary>
    const float previewOutlinePixels = 1.5f;

    readonly Dictionary<RectTransform, Vector2[]> baseBarPositions = new Dictionary<RectTransform, Vector2[]>();

    void Awake()
    {
        Instance = this;
        currentStyle = PlayerPrefs.GetInt("VantageCrosshairStyle", 0);
        currentColor = PlayerPrefs.GetInt("VantageCrosshairColor", 0);
        sizeMultiplier = PlayerPrefs.GetFloat("VantageCrosshairSize", 1f);
        gapMultiplier = PlayerPrefs.GetFloat("VantageCrosshairGap", 1f);
        outlineOn = PlayerPrefs.GetInt("VantageCrosshairOutline", 0) == 1;

        CaptureBasePositions(styleRoots);
        CaptureBasePositions(previewStyleRoots);

        ApplyStyle();
        ApplyColor();
        ApplyScale();
        ApplyGap();
        ApplyOutline();
    }

    void CaptureBasePositions(RectTransform[] roots)
    {
        if (roots == null) return;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null || baseBarPositions.ContainsKey(roots[i])) continue;
            var bars = GetBars(roots[i]);
            var positions = new Vector2[bars.Length];
            for (int b = 0; b < bars.Length; b++) positions[b] = bars[b].anchoredPosition;
            baseBarPositions[roots[i]] = positions;
        }
    }

    RectTransform[] GetBars(RectTransform root)
    {
        var list = new List<RectTransform>();
        foreach (Transform child in root)
        {
            if (child.name == "Bar") list.Add((RectTransform)child);
        }
        return list.ToArray();
    }

    /// <summary>0 = tight/accurate, 1 = max spread. Widens the active crosshair so the player can see why they missed.</summary>
    public void SetSpreadFraction(float frac01)
    {
        spreadFraction01 = Mathf.Clamp01(frac01);
        ApplyScale();
    }

    public void SetStyle(int index)
    {
        if (styleRoots == null || index < 0 || index >= styleRoots.Length) return;
        currentStyle = index;
        PlayerPrefs.SetInt("VantageCrosshairStyle", currentStyle);
        ApplyStyle();
        ApplyScale();
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

    public void SetSize(float value)
    {
        sizeMultiplier = Mathf.Clamp(value, 0.5f, 2f);
        PlayerPrefs.SetFloat("VantageCrosshairSize", sizeMultiplier);
        ApplyScale();
    }

    public void SetGap(float value)
    {
        gapMultiplier = Mathf.Clamp(value, 0.4f, 3f);
        PlayerPrefs.SetFloat("VantageCrosshairGap", gapMultiplier);
        ApplyGap();
    }

    public void SetOutline(bool on)
    {
        outlineOn = on;
        PlayerPrefs.SetInt("VantageCrosshairOutline", on ? 1 : 0);
        ApplyOutline();
    }

    public int CurrentStyle { get { return currentStyle; } }
    public int CurrentColor { get { return currentColor; } }
    public float CurrentSize { get { return sizeMultiplier; } }
    public float CurrentGap { get { return gapMultiplier; } }
    public bool CurrentOutline { get { return outlineOn; } }

    void ApplyStyle()
    {
        if (styleRoots == null) return;
        for (int i = 0; i < styleRoots.Length; i++)
        {
            if (styleRoots[i] != null) styleRoots[i].gameObject.SetActive(crosshairVisible && i == currentStyle);
        }
        if (previewStyleRoots != null)
        {
            for (int i = 0; i < previewStyleRoots.Length; i++)
            {
                if (previewStyleRoots[i] != null) previewStyleRoots[i].gameObject.SetActive(i == currentStyle);
            }
        }
    }

    /// <summary>Applies the chosen color to every bar in every style - so the pick sticks no matter which style is active now or later.</summary>
    void ApplyColor()
    {
        Color c = ColorOptions[currentColor];
        ApplyColorTo(styleRoots, c);
        ApplyColorTo(previewStyleRoots, c);
    }

    void ApplyColorTo(RectTransform[] roots, Color c)
    {
        if (roots == null) return;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            var images = roots[i].GetComponentsInChildren<Image>(true);
            for (int j = 0; j < images.Length; j++) images[j].color = c;
        }
    }

    /// <summary>Gameplay crosshair scales with size setting * live spread; the preview mirror only ever shows the static size setting.</summary>
    void ApplyScale()
    {
        if (styleRoots != null)
        {
            for (int i = 0; i < styleRoots.Length; i++)
            {
                if (i != currentStyle || styleRoots[i] == null) continue;
                float scale = sizeMultiplier * Mathf.Lerp(1f, maxSpreadScale, spreadFraction01);
                styleRoots[i].localScale = Vector3.one * scale;
            }
        }
        ApplyPreviewScale();
    }

    void ApplyPreviewScale()
    {
        if (previewStyleRoots == null) return;
        for (int i = 0; i < previewStyleRoots.Length; i++)
        {
            if (previewStyleRoots[i] == null) continue;
            float scale = sizeMultiplier * previewZoom;
            float extent = UnscaledExtent(previewStyleRoots[i]) * sizeMultiplier;
            if (extent * previewZoom > previewFitHalfExtent) scale = previewFitHalfExtent / extent;
            previewStyleRoots[i].localScale = Vector3.one * scale;
        }
        ApplyOutlineTo(previewStyleRoots); // outline distance depends on the preview scale
    }

    /// <summary>Furthest a bar edge reaches from the centre at the current gap, before root scaling.</summary>
    float UnscaledExtent(RectTransform root)
    {
        float extent = 1f;
        foreach (var bar in GetBars(root))
        {
            Vector2 half = bar.sizeDelta * 0.5f;
            extent = Mathf.Max(extent, Mathf.Abs(bar.anchoredPosition.x) + half.x, Mathf.Abs(bar.anchoredPosition.y) + half.y);
        }
        return extent;
    }

    void ApplyGap()
    {
        ApplyGapTo(styleRoots);
        ApplyGapTo(previewStyleRoots);
        ApplyPreviewScale(); // a wider gap may need the preview zoomed out to fit
    }

    void ApplyGapTo(RectTransform[] roots)
    {
        if (roots == null) return;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            Vector2[] basePositions;
            if (!baseBarPositions.TryGetValue(roots[i], out basePositions)) continue;
            var bars = GetBars(roots[i]);
            for (int b = 0; b < bars.Length && b < basePositions.Length; b++)
            {
                bars[b].anchoredPosition = basePositions[b] * gapMultiplier;
            }
        }
    }

    void ApplyOutline()
    {
        ApplyOutlineTo(styleRoots);
        ApplyOutlineTo(previewStyleRoots);
    }

    void ApplyOutlineTo(RectTransform[] roots)
    {
        if (roots == null) return;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            // HUD: 1 local px (scales with the size setting like before). Preview: divide out the zoom so it stays thin.
            bool isPreview = roots == previewStyleRoots;
            float distance = isPreview ? previewOutlinePixels / Mathf.Max(0.01f, roots[i].localScale.x) : 1f;
            var images = roots[i].GetComponentsInChildren<Image>(true);
            for (int j = 0; j < images.Length; j++)
            {
                var outline = images[j].GetComponent<Outline>();
                if (outlineOn)
                {
                    if (outline == null) outline = images[j].gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    outline.effectDistance = new Vector2(distance, -distance);
                    outline.enabled = true;
                }
                else if (outline != null)
                {
                    outline.enabled = false;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
