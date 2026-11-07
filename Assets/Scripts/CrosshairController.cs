using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fixed screen-center crosshair (FPS aim is screen-locked, unlike the old mouse-follow version).
/// Drawn in code from pixel settings - line length, thickness, gap and dot size - the way other
/// shooters set crosshairs up, instead of scaling a prebuilt shape by a percentage. The same
/// drawing goes into the HUD and into the settings-menu preview.
/// </summary>
public class CrosshairController : MonoBehaviour
{
    public static CrosshairController Instance;

    public const int StyleCross = 0;
    public const int StyleCrossDot = 1;
    public const int StyleDot = 2;
    public const int StyleT = 3;
    public const int StyleCount = 4;

    public const float MinLength = 2f, MaxLength = 24f;
    public const float MinThickness = 1f, MaxThickness = 6f;
    public const float MinGap = 0f, MaxGap = 20f;
    public const float MinDot = 2f, MaxDot = 16f;

    [Tooltip("Empty, screen-centred root the HUD crosshair parts are built under at runtime.")]
    public RectTransform hudRoot;
    [Tooltip("Empty root inside the settings preview box. Shows the same crosshair, zoomed, never affected by spread or hiding.")]
    public RectTransform previewRoot;
    [Tooltip("Round sprite for the DOT style.")]
    public Sprite dotSprite;
    [Tooltip("Extra gap in px at full spread, so the lines push outwards when shots will scatter.")]
    public float spreadExtraGap = 14f;

    /// <summary>Index-selected via the settings menu - same pattern as style. Orange (index 0) matches the existing HUD accent color.</summary>
    public static readonly Color[] ColorOptions =
    {
        new Color(0.886f, 0.647f, 0.235f), // orange (default)
        Color.white,
        new Color(1f, 0.25f, 0.25f),       // red
        new Color(0.35f, 1f, 0.45f),       // green
        new Color(0.3f, 0.85f, 1f),        // cyan
    };

    /// <summary>Outline thickness in px around every part, when the outline is on.</summary>
    const float OutlinePx = 2f;
    /// <summary>Preview shows the crosshair at 2x HUD size, zooming out if it wouldn't fit inside 88px of the 200x200 box's centre.</summary>
    const float PreviewZoom = 2f;
    const float PreviewFitHalfExtent = 88f;

    const string Prefix = "VantageXhair.";

    int style;
    int colorIndex;
    float length = 16f;
    float thickness = 2f;
    float gap = 4f;
    float dotSize = 6f;
    bool outlineOn;
    bool crosshairVisible = true;
    float spread01;

    /// <summary>One crosshair piece: the coloured fill plus a black copy 2px bigger behind it as the outline. A real backing shape, not a UI Outline effect, so a round dot gets a round outline.</summary>
    class Part
    {
        public Image outline;
        public Image fill;
    }

    // Left, right, bottom, top, centre.
    static readonly string[] PartNames = { "Left", "Right", "Bottom", "Top", "Centre" };
    Part[] hudParts;
    Part[] previewParts;

    void Awake()
    {
        Instance = this;
        LoadSettings();
        hudParts = BuildParts(hudRoot);
        previewParts = BuildParts(previewRoot);
        ApplyColor();
        ApplyAll();
    }

    void LoadSettings()
    {
        if (!PlayerPrefs.HasKey(Prefix + "Style")) MigrateOldSettings();
        style = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Style", StyleCross), 0, StyleCount - 1);
        length = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Length", 16f), MinLength, MaxLength);
        thickness = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Thickness", 2f), MinThickness, MaxThickness);
        gap = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Gap", 4f), MinGap, MaxGap);
        dotSize = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "DotSize", 6f), MinDot, MaxDot);
        colorIndex = Mathf.Clamp(PlayerPrefs.GetInt("VantageCrosshairColor", 0), 0, ColorOptions.Length - 1);
        outlineOn = PlayerPrefs.GetInt("VantageCrosshairOutline", 0) == 1;
    }

    /// <summary>
    /// Converts the old percentage settings (scale of a 16x2 line set 12px out, or an 8px dot) to
    /// the nearest pixel values, so an existing crosshair keeps its look. Brackets is gone and
    /// becomes the plain cross.
    /// </summary>
    void MigrateOldSettings()
    {
        if (!PlayerPrefs.HasKey("VantageCrosshairStyle")) return;
        int oldStyle = PlayerPrefs.GetInt("VantageCrosshairStyle", 0);
        float oldSize = PlayerPrefs.GetFloat("VantageCrosshairSize", 1f);
        float oldGap = PlayerPrefs.GetFloat("VantageCrosshairGap", 1f);
        PlayerPrefs.SetInt(Prefix + "Style", oldStyle == 1 ? StyleDot : StyleCross);
        PlayerPrefs.SetFloat(Prefix + "Length", Mathf.Round(Mathf.Clamp(16f * oldSize, MinLength, MaxLength)));
        PlayerPrefs.SetFloat(Prefix + "Thickness", Mathf.Round(Mathf.Clamp(2f * oldSize, MinThickness, MaxThickness)));
        PlayerPrefs.SetFloat(Prefix + "Gap", Mathf.Round(Mathf.Clamp((12f * oldGap - 8f) * oldSize, MinGap, MaxGap)));
        PlayerPrefs.SetFloat(Prefix + "DotSize", Mathf.Round(Mathf.Clamp(8f * oldSize, MinDot, MaxDot)));
    }

    Part[] BuildParts(RectTransform root)
    {
        if (root == null) return null;
        var parts = new Part[PartNames.Length];
        // All outlines first, then all fills, so no outline is ever drawn over a neighbouring line.
        for (int i = 0; i < parts.Length; i++) parts[i] = new Part { outline = MakeImage(root, PartNames[i] + "Outline") };
        for (int i = 0; i < parts.Length; i++) parts[i].fill = MakeImage(root, PartNames[i]);
        return parts;
    }

    static Image MakeImage(RectTransform root, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    /// <summary>0 = tight/accurate, 1 = max spread. Pushes the lines outwards so the player can see why they missed.</summary>
    public void SetSpreadFraction(float frac01)
    {
        frac01 = Mathf.Clamp01(frac01);
        if (Mathf.Approximately(frac01, spread01)) return;
        spread01 = frac01;
        ApplyTo(hudParts, spread01 * spreadExtraGap);
    }

    public void SetCrosshairVisible(bool visible)
    {
        crosshairVisible = visible;
        if (hudRoot != null) hudRoot.gameObject.SetActive(crosshairVisible);
    }

    public void SetStyle(int index)
    {
        if (index < 0 || index >= StyleCount) return;
        style = index;
        PlayerPrefs.SetInt(Prefix + "Style", style);
        ApplyAll();
    }

    public void SetColor(int index)
    {
        if (index < 0 || index >= ColorOptions.Length) return;
        colorIndex = index;
        PlayerPrefs.SetInt("VantageCrosshairColor", colorIndex);
        ApplyColor();
    }

    public void SetLength(float px) { length = Mathf.Clamp(px, MinLength, MaxLength); PlayerPrefs.SetFloat(Prefix + "Length", length); ApplyAll(); }
    public void SetThickness(float px) { thickness = Mathf.Clamp(px, MinThickness, MaxThickness); PlayerPrefs.SetFloat(Prefix + "Thickness", thickness); ApplyAll(); }
    public void SetGap(float px) { gap = Mathf.Clamp(px, MinGap, MaxGap); PlayerPrefs.SetFloat(Prefix + "Gap", gap); ApplyAll(); }
    public void SetDotSize(float px) { dotSize = Mathf.Clamp(px, MinDot, MaxDot); PlayerPrefs.SetFloat(Prefix + "DotSize", dotSize); ApplyAll(); }

    public void SetOutline(bool on)
    {
        outlineOn = on;
        PlayerPrefs.SetInt("VantageCrosshairOutline", on ? 1 : 0);
        ApplyAll();
    }

    public int CurrentStyle { get { return style; } }
    public int CurrentColor { get { return colorIndex; } }
    public float CurrentLength { get { return length; } }
    public float CurrentThickness { get { return thickness; } }
    public float CurrentGap { get { return gap; } }
    public float CurrentDotSize { get { return dotSize; } }
    public bool CurrentOutline { get { return outlineOn; } }

    void ApplyAll()
    {
        ApplyTo(hudParts, spread01 * spreadExtraGap);
        ApplyTo(previewParts, 0f);
        if (hudRoot != null) hudRoot.gameObject.SetActive(crosshairVisible);
        if (previewRoot != null) previewRoot.localScale = Vector3.one * Mathf.Min(PreviewZoom, PreviewFitHalfExtent / Extent());
    }

    /// <summary>How far the crosshair reaches from the centre at rest, outline included.</summary>
    float Extent()
    {
        float reach = style == StyleDot ? dotSize * 0.5f : gap + length;
        return Mathf.Max(1f, reach + (outlineOn ? OutlinePx : 0f));
    }

    void ApplyTo(Part[] parts, float extraGap)
    {
        if (parts == null) return;
        bool lines = style != StyleDot;
        float offset = gap + extraGap + length * 0.5f;
        Vector2 horizontal = new Vector2(length, thickness);
        Vector2 vertical = new Vector2(thickness, length);
        SetPart(parts[0], lines, new Vector2(-offset, 0f), horizontal, null);
        SetPart(parts[1], lines, new Vector2(offset, 0f), horizontal, null);
        SetPart(parts[2], lines, new Vector2(0f, -offset), vertical, null);
        SetPart(parts[3], lines && style != StyleT, new Vector2(0f, offset), vertical, null);

        // DOT is round; the centre dot of CROSS + DOT is a square as thick as the lines, so it matches them.
        bool round = style == StyleDot;
        bool centre = round || style == StyleCrossDot;
        SetPart(parts[4], centre, Vector2.zero, Vector2.one * (round ? dotSize : thickness), round ? dotSprite : null);
    }

    void SetPart(Part p, bool on, Vector2 position, Vector2 size, Sprite sprite)
    {
        p.fill.gameObject.SetActive(on);
        p.outline.gameObject.SetActive(on && outlineOn);
        p.fill.rectTransform.anchoredPosition = p.outline.rectTransform.anchoredPosition = position;
        p.fill.rectTransform.sizeDelta = size;
        p.outline.rectTransform.sizeDelta = size + Vector2.one * OutlinePx * 2f;
        p.fill.sprite = p.outline.sprite = sprite;
    }

    void ApplyColor()
    {
        Color c = ColorOptions[colorIndex];
        ColorParts(hudParts, c);
        ColorParts(previewParts, c);
    }

    static void ColorParts(Part[] parts, Color c)
    {
        if (parts == null) return;
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].fill.color = c;
            parts[i].outline.color = Color.black;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
