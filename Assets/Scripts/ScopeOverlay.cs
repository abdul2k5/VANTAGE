using UnityEngine;
using UnityEngine.UI;

/// <summary>The view through the scope: a round opening with reticle lines and black either side. Sits behind the rest of the HUD, so the crosshair dot, ammo and health stay readable on top. The texture is drawn in code, so there's no image asset to import.</summary>
public class ScopeOverlay : MonoBehaviour
{
    const int TextureSize = 1024;

    [Tooltip("Radius of the opening, as a share of the screen height.")]
    [Range(0.3f, 0.5f)] public float openingRadius = 0.47f;
    [Tooltip("How far the dark edge fades into the opening, as a share of the screen height.")]
    public float edgeSoftness = 0.05f;
    [Tooltip("Reticle lines stop this far from the centre, as a share of the screen height, leaving room for the player's own crosshair.")]
    public float reticleGap = 0.035f;

    CanvasGroup group;

    void Awake()
    {
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var lens = new GameObject("Lens", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
        lens.transform.SetParent(transform, false);
        var lensRect = (RectTransform)lens.transform;
        lensRect.anchorMin = new Vector2(0.5f, 0f);
        lensRect.anchorMax = new Vector2(0.5f, 1f);
        lensRect.offsetMin = Vector2.zero;
        lensRect.offsetMax = Vector2.zero;
        var image = lens.GetComponent<RawImage>();
        image.texture = BuildTexture();
        image.raycastTarget = false;
        var fitter = lens.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        fitter.aspectRatio = 1f;

        AddSideBar(lensRect, 0f, 1f);
        AddSideBar(lensRect, 1f, 0f);

        SetAmount(0f);
    }

    /// <summary>Black from the edge of the square lens image out past the side of any screen shape.</summary>
    void AddSideBar(RectTransform lens, float anchorX, float pivotX)
    {
        var bar = new GameObject("SideBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(lens, false);
        var rect = (RectTransform)bar.transform;
        rect.anchorMin = new Vector2(anchorX, 0f);
        rect.anchorMax = new Vector2(anchorX, 1f);
        rect.pivot = new Vector2(pivotX, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(8000f, 0f);
        var image = bar.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
    }

    public void SetAmount(float amount)
    {
        if (group != null) group.alpha = amount;
    }

    Texture2D BuildTexture()
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[TextureSize * TextureSize];
        float lineHalfWidth = 1.5f / TextureSize;
        for (int y = 0; y < TextureSize; y++)
        {
            float v = (y + 0.5f) / TextureSize - 0.5f;
            for (int x = 0; x < TextureSize; x++)
            {
                float u = (x + 0.5f) / TextureSize - 0.5f;
                float radius = Mathf.Sqrt(u * u + v * v);

                float alpha = Mathf.SmoothStep(0f, 1f, (radius - (openingRadius - edgeSoftness)) / edgeSoftness);

                // Amber rather than black: black lines disappear against the dark walls. Stops at the dark edge.
                bool onLine = (Mathf.Abs(u) < lineHalfWidth || Mathf.Abs(v) < lineHalfWidth) && radius > reticleGap && alpha < 0.5f;

                pixels[y * TextureSize + x] = onLine ? new Color32(226, 165, 60, 200) : new Color32(0, 0, 0, (byte)(alpha * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
