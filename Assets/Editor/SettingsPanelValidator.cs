using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Walks every tab of the Settings panel and checks two invariants: every control's corners
/// stay inside the panel's bounds, and no two sibling controls' rects overlap. "Sibling" means
/// direct children of the same parent; composite widgets (Slider/Scrollbar/Toggle/Dropdown) are
/// treated as opaque leaves for the overlap check - their internal Background/Fill/Handle parts
/// are siblings by design and are MEANT to overlap, so descending into them would false-positive
/// on every slider. Bounds checking still covers the widget's own outer rect.
/// </summary>
public static class SettingsPanelValidator
{
    const float Epsilon = 0.5f;

    [MenuItem("Vantage/Validate Settings Panel")]
    public static void ValidateMenuItem()
    {
        int errors = Validate();
        if (errors == 0) Debug.Log("[SettingsPanelValidator] PASS - 0 errors across all 3 tabs.");
        else Debug.LogError("[SettingsPanelValidator] FAIL - " + errors + " error(s). See above.");
    }

    public static int Validate()
    {
        GameObject ui = GameObject.Find("UICanvas");
        Transform[] all = ui.GetComponentsInChildren<Transform>(true);
        Transform panel = null;
        Transform[] tabPanels = new Transform[3];
        string[] tabNames = { "TabPanel_General", "TabPanel_Crosshair", "TabPanel_Audio" };

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "SettingsPanel") panel = all[i];
            for (int t = 0; t < tabNames.Length; t++)
            {
                if (all[i].name == tabNames[t]) tabPanels[t] = all[i];
            }
        }

        if (panel == null) { Debug.LogError("[SettingsPanelValidator] SettingsPanel not found."); return 1; }

        RectTransform panelRt = panel as RectTransform;
        int totalErrors = 0;

        // Open the panel and show each tab in turn so inactive-tab RectTransforms still have
        // valid, up-to-date world corners (Unity doesn't update layout for inactive GameObjects).
        bool wasActive = panel.gameObject.activeSelf;
        panel.gameObject.SetActive(true);

        SettingsMenuController smc = Object.FindFirstObjectByType<SettingsMenuController>(FindObjectsInactive.Include);

        for (int t = 0; t < tabPanels.Length; t++)
        {
            if (tabPanels[t] == null) { Debug.LogError("[SettingsPanelValidator] " + tabNames[t] + " not found."); totalErrors++; continue; }

            if (smc != null) smc.ShowTab(t);
            else for (int k = 0; k < tabPanels.Length; k++) tabPanels[k].gameObject.SetActive(k == t);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tabPanels[t] as RectTransform);
            Canvas.ForceUpdateCanvases();

            int tabErrors = ValidateTab(tabNames[t], panelRt, tabPanels[t] as RectTransform);
            totalErrors += tabErrors;
        }

        panel.gameObject.SetActive(wasActive);
        return totalErrors;
    }

    static int ValidateTab(string tabName, RectTransform panelRt, RectTransform tabRt)
    {
        int errors = 0;
        errors += CheckBoundsRecursive(tabName, panelRt, tabRt);
        errors += CheckSiblingOverlapRecursive(tabName, tabRt);
        return errors;
    }

    static bool IsAtomicWidget(Transform t)
    {
        if (t.GetComponent<Slider>() != null
            || t.GetComponent<Scrollbar>() != null
            || t.GetComponent<Toggle>() != null
            || t.GetComponent<Dropdown>() != null)
        {
            return true;
        }
        // Crosshair glyph containers (style tile icons, the live preview): their Bar children
        // are deliberately overlapping line segments drawn to form a single continuous glyph
        // (e.g. the BRACKETS corner joints), not independent layout rows.
        if (t.name == "Icon" || t.name.StartsWith("Preview_Crosshair_")) return true;
        return false;
    }

    static Rect GetLocalRect(RectTransform panelRt, RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 min = panelRt.InverseTransformPoint(corners[0]);
        Vector2 max = min;
        for (int i = 1; i < 4; i++)
        {
            Vector2 p = panelRt.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        return new Rect(min, max - min);
    }

    static int CheckBoundsRecursive(string tabName, RectTransform panelRt, Transform t)
    {
        int errors = 0;
        Rect panelBounds = new Rect(panelRt.rect.xMin, panelRt.rect.yMin, panelRt.rect.width, panelRt.rect.height);

        foreach (Transform child in t)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            RectTransform childRt = child as RectTransform;
            if (childRt != null)
            {
                Rect r = GetLocalRect(panelRt, childRt);
                if (r.xMin < panelBounds.xMin - Epsilon || r.xMax > panelBounds.xMax + Epsilon
                    || r.yMin < panelBounds.yMin - Epsilon || r.yMax > panelBounds.yMax + Epsilon)
                {
                    Debug.LogError("[SettingsPanelValidator] [" + tabName + "] '" + GetPath(child) + "' corners fall outside panel bounds. rect=" + r + " panelBounds=" + panelBounds);
                    errors++;
                }
            }
            errors += CheckBoundsRecursive(tabName, panelRt, child);
        }
        return errors;
    }

    static int CheckSiblingOverlapRecursive(string tabName, Transform parent)
    {
        int errors = 0;
        RectTransform parentRt = parent as RectTransform;
        List<Transform> siblings = new List<Transform>();
        foreach (Transform child in parent)
        {
            if (child.gameObject.activeInHierarchy && child as RectTransform != null) siblings.Add(child);
        }

        for (int i = 0; i < siblings.Count && parentRt != null; i++)
        {
            for (int j = i + 1; j < siblings.Count; j++)
            {
                Rect a = GetLocalRect(parentRt, siblings[i] as RectTransform);
                Rect b = GetLocalRect(parentRt, siblings[j] as RectTransform);
                // Both rects are expressed in the same local space (parent's).
                if (OverlapArea(a, b) > Epsilon * Epsilon)
                {
                    Debug.LogError("[SettingsPanelValidator] [" + tabName + "] siblings overlap: '" + GetPath(siblings[i]) + "' and '" + GetPath(siblings[j]) + "'. a=" + a + " b=" + b);
                    errors++;
                }
            }
        }

        foreach (Transform child in parent)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            if (IsAtomicWidget(child)) continue; // don't descend into a slider's intentionally-overlapping internals
            errors += CheckSiblingOverlapRecursive(tabName, child);
        }
        return errors;
    }

    static float OverlapArea(Rect a, Rect b)
    {
        float xOverlap = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
        float yOverlap = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
        return xOverlap * yOverlap;
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        Transform p = t.parent;
        int depth = 0;
        while (p != null && depth < 4)
        {
            path = p.name + "/" + path;
            p = p.parent;
            depth++;
        }
        return path;
    }
}
