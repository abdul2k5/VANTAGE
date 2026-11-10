using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The overlap check for the in-game HUD, the counterpart of SettingsPanelValidator. Checks three
/// things at the canvas's 1920x1080 reference size: every HUD element stays on screen, no two
/// elements that can be visible together overlap, and no Text needs more room than its box
/// (which would wrap onto a cut-off line or truncate).
/// Elements that are normally hidden are measured as if shown, without changing the scene.
/// </summary>
public static class HudLayoutValidator
{
    const float Epsilon = 0.5f;

    // Direct children of UICanvas that make up the HUD during play. Full-screen overlays, menus and
    // end screens are left out: they cover the HUD on purpose.
    static readonly string[] HudElements =
    {
        "TopHudStack", "HealthBar_BG", "ShieldBar_BG", "AmmoLabel_BG", "SettingsGearButton", "EscHint",
        "AbilityHUD", "UltimateHUD", "UltimateActiveLabel_BG", "PlantProgress", "RoundTimer_BG", "ChargeIcon",
        "KillFeed", "CheckpointBannerPanel", "ReconAimHint", "ObjectiveButton",
    };

    // Pairs that are never on screen together, so their rects may share space.
    static readonly string[,] NeverTogether =
    {
        // The banner shows on entering Room 5; the round timer and Overwatch label belong to later moments.
        { "CheckpointBannerPanel", "RoundTimer_BG" },
        { "CheckpointBannerPanel", "ChargeIcon" },
        { "CheckpointBannerPanel", "UltimateActiveLabel_BG" },
        // The charge icon sits inside the round timer's bar by design.
        { "RoundTimer_BG", "ChargeIcon" },
        // Aiming the dart and planting the charge are both held actions; only one can be held.
        { "PlantProgress", "ReconAimHint" },
    };

    [MenuItem("Vantage/Validate HUD Layout")]
    public static void ValidateMenuItem()
    {
        int errors = Validate();
        if (errors == 0) Debug.Log("[HudLayoutValidator] PASS - 0 errors.");
        else Debug.LogError("[HudLayoutValidator] FAIL - " + errors + " error(s). See above.");
    }

    public static int Validate()
    {
        GameObject canvasGo = GameObject.Find("UICanvas");
        if (canvasGo == null) { Debug.LogError("[HudLayoutValidator] UICanvas not found."); return 1; }
        RectTransform canvasRt = canvasGo.transform as RectTransform;

        // Show everything for the measurement, then put it back.
        var restored = new List<GameObject>();
        var elements = new List<RectTransform>();
        foreach (string name in HudElements)
        {
            Transform t = canvasRt.Find(name);
            if (t == null) continue;
            foreach (Transform d in t.GetComponentsInChildren<Transform>(true))
            {
                // Templates are cloned at runtime and never shown themselves.
                if (d.name.Contains("Template")) continue;
                if (!d.gameObject.activeSelf) { d.gameObject.SetActive(true); restored.Add(d.gameObject); }
            }
            elements.Add(t as RectTransform);
        }
        Canvas.ForceUpdateCanvases();
        foreach (RectTransform e in elements) LayoutRebuilder.ForceRebuildLayoutImmediate(e);
        Canvas.ForceUpdateCanvases();

        int errors = 0;
        Rect screen = canvasRt.rect;
        var bounds = new List<Rect>();
        foreach (RectTransform e in elements)
        {
            Rect r = VisibleBounds(canvasRt, e);
            bounds.Add(r);
            if (r.xMin < screen.xMin - Epsilon || r.xMax > screen.xMax + Epsilon || r.yMin < screen.yMin - Epsilon || r.yMax > screen.yMax + Epsilon)
            {
                Debug.LogError("[HudLayoutValidator] '" + e.name + "' runs off screen. rect=" + r + " screen=" + screen);
                errors++;
            }
            errors += CheckTextFits(e);
        }

        for (int i = 0; i < elements.Count; i++)
        {
            for (int j = i + 1; j < elements.Count; j++)
            {
                if (IsNeverTogether(elements[i].name, elements[j].name)) continue;
                if (OverlapArea(bounds[i], bounds[j]) > Epsilon * Epsilon)
                {
                    Debug.LogError("[HudLayoutValidator] '" + elements[i].name + "' and '" + elements[j].name + "' overlap. a=" + bounds[i] + " b=" + bounds[j]);
                    errors++;
                }
            }
        }

        foreach (GameObject go in restored) go.SetActive(false);
        return errors;
    }

    /// <summary>The box around everything the element actually draws, in canvas space. An element's own rect can be
    /// empty (a layout root) or smaller than its children (a label hanging below a tile), so graphics are measured.</summary>
    static Rect VisibleBounds(RectTransform canvasRt, RectTransform root)
    {
        bool any = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;
        var corners = new Vector3[4];
        foreach (Graphic g in root.GetComponentsInChildren<Graphic>(false))
        {
            g.rectTransform.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = canvasRt.InverseTransformPoint(corners[i]);
                if (!any) { min = p; max = p; any = true; }
                else { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            }
        }
        if (!any)
        {
            root.GetWorldCorners(corners);
            min = canvasRt.InverseTransformPoint(corners[0]);
            max = canvasRt.InverseTransformPoint(corners[2]);
        }
        return new Rect(min, max - min);
    }

    static int CheckTextFits(RectTransform root)
    {
        int errors = 0;
        foreach (Text t in root.GetComponentsInChildren<Text>(false))
        {
            if (t.resizeTextForBestFit || string.IsNullOrEmpty(t.text)) continue;
            Rect r = t.rectTransform.rect;
            bool tooTall = t.verticalOverflow == VerticalWrapMode.Truncate && t.preferredHeight > r.height + Epsilon;
            bool tooWide = t.horizontalOverflow == HorizontalWrapMode.Wrap && r.width <= 0f;
            if (tooTall || tooWide)
            {
                Debug.LogError("[HudLayoutValidator] text '" + root.name + "/.../" + t.name + "' needs " + t.preferredHeight + "px of height but its box is " + r.height + "px.");
                errors++;
            }
        }
        return errors;
    }

    static bool IsNeverTogether(string a, string b)
    {
        for (int i = 0; i < NeverTogether.GetLength(0); i++)
        {
            if ((NeverTogether[i, 0] == a && NeverTogether[i, 1] == b) || (NeverTogether[i, 0] == b && NeverTogether[i, 1] == a)) return true;
        }
        return false;
    }

    static float OverlapArea(Rect a, Rect b)
    {
        float xOverlap = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
        float yOverlap = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
        return xOverlap * yOverlap;
    }
}
