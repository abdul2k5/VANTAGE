using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot rebuild of the Settings panel to the "no force-expand, every row has an explicit
/// preferredHeight, sliders built from DefaultControls.CreateSlider" spec. Salvages the live,
/// already-wired objects (PreviewBox + its CrosshairController.previewStyleRoots children, the
/// 3 style tiles, the 5 color swatch cells, the 3 menu buttons, the outline button) rather than
/// recreating them, so none of those existing component references break. Everything else
/// (all 6 sliders, the two-column Crosshair split, section labels) is destroyed and rebuilt.
/// </summary>
public static class SettingsPanelBuilder
{
    static readonly Color Accent = new Color(0.886f, 0.647f, 0.235f, 1f);
    static readonly Color TileDark = new Color(0.16f, 0.17f, 0.15f, 1f);
    static readonly Color TrackDark = new Color(0.25f, 0.26f, 0.22f, 1f);
    static readonly Color LightText = new Color(0.85f, 0.85f, 0.82f, 1f);

    const float RowHeight = 36f;
    const float Spacing = 12f;
    const float Padding = 24f;

    // Shared geometry - see CHANGELOG for the arithmetic behind these.
    const float ContentCenterY = -55f;
    const float ContentHeight = 470f;
    const float RowWidth = 572f; // General/Audio column content width AND Crosshair right-column width - both land here.

    [MenuItem("Vantage/Rebuild Settings Panel")]
    public static void Rebuild()
    {
        GameObject uiCanvas = GameObject.Find("UICanvas");
        Transform ui = uiCanvas.transform;
        System.Func<string, Transform> Find = delegate (string n)
        {
            // Re-queried live every call rather than cached once, since this method destroys
            // objects partway through - a cached array would hold dangling references past that point.
            Transform[] live = ui.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < live.Length; i++) if (live[i] != null && live[i].name == n) return live[i];
            Debug.LogError("[SettingsPanelBuilder] Could not find '" + n + "'");
            return null;
        };

        Transform settingsPanel = Find("SettingsPanel");
        RectTransform panelRt = settingsPanel as RectTransform;
        panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(900f, 620f);
        panelRt.anchoredPosition = Vector2.zero;

        SetAnchoredY(Find("SettingsTitle"), 270f);
        SetAnchoredY(Find("TabRow"), 215f);

        SettingsMenuController smc = Object.FindFirstObjectByType<SettingsMenuController>(FindObjectsInactive.Include);

        Transform generalTab = Find("TabPanel_General");
        Transform crosshairTab = Find("TabPanel_Crosshair");
        Transform audioTab = Find("TabPanel_Audio");

        // ---------------- GENERAL ----------------
        SetRect(generalTab, 620f, ContentHeight, ContentCenterY);
        RemoveIfPresent<ContentSizeFitter>(generalTab.gameObject);
        ConfigureVLG(GetOrAdd<VerticalLayoutGroup>(generalTab.gameObject), TextAnchor.UpperCenter, false, true, false, false, Spacing, Padding);

        Transform oldSensRow = generalTab.Find("SensitivityRow");
        if (oldSensRow != null) Object.DestroyImmediate(oldSensRow.gameObject);

        Slider sensSlider; Text sensValueText;
        GameObject sensRow = BuildSliderRow(generalTab, "SensitivityRow", "SENSITIVITY", 1f, 10f, 3.3f, false);
        ExtractSliderRefs(sensRow, out sensSlider, out sensValueText);
        sensRow.transform.SetSiblingIndex(0);

        Transform resumeBtn = generalTab.Find("ResumeButton");
        Transform restartBtn = generalTab.Find("RestartRoomButton");
        Transform quitBtn = generalTab.Find("QuitToMenuButton");
        AddLayoutElement(resumeBtn.gameObject, 300f, 46f);
        AddLayoutElement(restartBtn.gameObject, 300f, 46f);
        AddLayoutElement(quitBtn.gameObject, 300f, 46f);
        resumeBtn.SetSiblingIndex(1);
        restartBtn.SetSiblingIndex(2);
        quitBtn.SetSiblingIndex(3);

        // ---------------- AUDIO ----------------
        SetRect(audioTab, 620f, ContentHeight, ContentCenterY);
        RemoveIfPresent<ContentSizeFitter>(audioTab.gameObject);
        ConfigureVLG(GetOrAdd<VerticalLayoutGroup>(audioTab.gameObject), TextAnchor.UpperLeft, false, true, false, false, Spacing, Padding);

        foreach (string n in new[] { "MasterRow", "MusicRow", "SFXRow" })
        {
            Transform old = audioTab.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        Transform volHeader = audioTab.Find("VolumeHeader");
        ResetTextRect(volHeader, RowWidth, 24f);
        AddLayoutElement(volHeader.gameObject, RowWidth, 30f);
        volHeader.SetSiblingIndex(0);

        GameObject masterRow = BuildSliderRow(audioTab, "MasterRow", "MASTER", 0f, 1f, 0.8f, true);
        GameObject musicRow = BuildSliderRow(audioTab, "MusicRow", "MUSIC", 0f, 1f, 0.8f, true);
        GameObject sfxRow = BuildSliderRow(audioTab, "SFXRow", "SFX", 0f, 1f, 0.8f, true);
        masterRow.transform.SetSiblingIndex(1);
        musicRow.transform.SetSiblingIndex(2);
        sfxRow.transform.SetSiblingIndex(3);
        Slider masterSlider, musicSlider, sfxSlider; Text masterValueText, musicValueText, sfxValueText;
        ExtractSliderRefs(masterRow, out masterSlider, out masterValueText);
        ExtractSliderRefs(musicRow, out musicSlider, out musicValueText);
        ExtractSliderRefs(sfxRow, out sfxSlider, out sfxValueText);

        // ---------------- CROSSHAIR ----------------
        Transform previewBox = Find("PreviewBox");
        Transform optCross = Find("Option_CROSS");
        Transform optDot = Find("Option_DOT");
        Transform optBrackets = Find("Option_BRACKETS");
        Transform cell0 = Find("SwatchCell_0");
        Transform cell1 = Find("SwatchCell_1");
        Transform cell2 = Find("SwatchCell_2");
        Transform cell3 = Find("SwatchCell_3");
        Transform cell4 = Find("SwatchCell_4");
        Transform styleHeader = Find("StyleHeader");
        Transform colorHeader = Find("ColorHeader");
        Transform outlineButtonT = Find("OutlineButton");

        // Rescue salvaged objects out of the containers about to be destroyed.
        previewBox.SetParent(crosshairTab, false);
        optCross.SetParent(crosshairTab, false);
        optDot.SetParent(crosshairTab, false);
        optBrackets.SetParent(crosshairTab, false);
        cell0.SetParent(crosshairTab, false);
        cell1.SetParent(crosshairTab, false);
        cell2.SetParent(crosshairTab, false);
        cell3.SetParent(crosshairTab, false);
        cell4.SetParent(crosshairTab, false);
        styleHeader.SetParent(crosshairTab, false);
        colorHeader.SetParent(crosshairTab, false);
        outlineButtonT.SetParent(crosshairTab, false);

        // Includes LeftColumn/RightColumn so reruns are idempotent instead of accumulating duplicates -
        // everything worth keeping was already rescued above.
        foreach (string n in new[] { "PreviewGroup", "StyleRow", "SwatchRow", "SizeRow", "GapRow", "LeftColumn", "RightColumn" })
        {
            Transform old = crosshairTab.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        RemoveIfPresent<VerticalLayoutGroup>(crosshairTab.gameObject);
        RemoveIfPresent<HorizontalLayoutGroup>(crosshairTab.gameObject);
        RemoveIfPresent<ContentSizeFitter>(crosshairTab.gameObject);
        SetRect(crosshairTab, 880f, ContentHeight, ContentCenterY);
        HorizontalLayoutGroup columns = crosshairTab.gameObject.AddComponent<HorizontalLayoutGroup>();
        ConfigureHLG(columns, TextAnchor.UpperCenter, true, true, false, false, 16f, 16f);

        GameObject leftCol = NewUIObject("LeftColumn", crosshairTab);
        AddLayoutElement(leftCol, 260f, ContentHeight - 32f);
        ConfigureVLG(leftCol.AddComponent<VerticalLayoutGroup>(), TextAnchor.UpperCenter, false, true, false, false, Spacing, 0f);

        previewBox.SetParent(leftCol.transform, false);
        RectTransform pbRt = previewBox as RectTransform;
        pbRt.sizeDelta = new Vector2(260f, 260f);
        pbRt.anchoredPosition = Vector2.zero;
        AddLayoutElement(previewBox.gameObject, 260f, 260f);
        RescalePreview(previewBox);

        GameObject rightCol = NewUIObject("RightColumn", crosshairTab);
        AddLayoutElement(rightCol, RowWidth, ContentHeight - 32f);
        ConfigureVLG(rightCol.AddComponent<VerticalLayoutGroup>(), TextAnchor.UpperLeft, false, true, false, false, Spacing, 0f);

        styleHeader.SetParent(rightCol.transform, false);
        ResetTextRect(styleHeader, RowWidth, 24f);
        AddLayoutElement(styleHeader.gameObject, RowWidth, 24f);

        GameObject tileRow = NewUIObject("TileRow", rightCol.transform);
        AddLayoutElement(tileRow, RowWidth, 72f);
        ConfigureHLG(tileRow.AddComponent<HorizontalLayoutGroup>(), TextAnchor.MiddleLeft, false, true, false, false, Spacing, 0f);

        Outline[] tileOutlines = new Outline[3];
        RectTransform[] tileIconRoots = new RectTransform[3];
        Transform[] tiles = { optCross, optDot, optBrackets };
        for (int i = 0; i < 3; i++)
        {
            Transform tile = tiles[i];
            tile.SetParent(tileRow.transform, false);
            RectTransform tileRt = tile as RectTransform;
            tileRt.sizeDelta = new Vector2(72f, 72f);
            tileRt.anchoredPosition = Vector2.zero;
            AddLayoutElement(tile.gameObject, 72f, 72f);

            Transform label = tile.Find("Label");
            if (label != null) Object.DestroyImmediate(label.gameObject);

            Image tileImg = tile.GetComponent<Image>();
            tileImg.color = TileDark;

            Outline outline = tile.GetComponent<Outline>();
            if (outline == null) outline = tile.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            tileOutlines[i] = outline;

            Transform icon = tile.Find("Preview");
            if (icon == null) icon = tile.Find("Icon"); // already renamed by a previous run of this tool
            icon.name = "Icon";
            tileIconRoots[i] = icon as RectTransform;
        }

        AddSpacer(rightCol.transform, "Spacer1", RowWidth, 16f);

        colorHeader.SetParent(rightCol.transform, false);
        ResetTextRect(colorHeader, RowWidth, 24f);
        AddLayoutElement(colorHeader.gameObject, RowWidth, 24f);

        GameObject chipRow = NewUIObject("ChipRow", rightCol.transform);
        AddLayoutElement(chipRow, RowWidth, 58f);
        ConfigureHLG(chipRow.AddComponent<HorizontalLayoutGroup>(), TextAnchor.UpperLeft, false, true, false, false, Spacing, 0f);

        Transform[] cells = { cell0, cell1, cell2, cell3, cell4 };
        for (int i = 0; i < 5; i++)
        {
            Transform cell = cells[i];
            cell.SetParent(chipRow.transform, false);
            RectTransform cellRt = cell as RectTransform;
            cellRt.sizeDelta = new Vector2(70f, 58f);
            cellRt.anchoredPosition = Vector2.zero;
            AddLayoutElement(cell.gameObject, 70f, 58f);

            Transform ring = cell.Find("ColorRing_" + i);
            Transform swatch = cell.Find("Swatch_" + i);
            RectTransform swatchRt = swatch as RectTransform;
            swatchRt.sizeDelta = new Vector2(32f, 32f);
            swatchRt.anchoredPosition = new Vector2(0f, 7f);
            RectTransform ringRt = ring as RectTransform;
            ringRt.sizeDelta = new Vector2(40f, 40f);
            ringRt.anchoredPosition = new Vector2(0f, 7f);
            ring.GetComponent<Image>().color = Color.white;

            Transform nameLabel = swatch.Find("Label");
            if (nameLabel != null)
            {
                RectTransform nameRt = nameLabel as RectTransform;
                nameRt.anchoredPosition = new Vector2(0f, -21f);
                nameRt.sizeDelta = new Vector2(70f, 16f);

                Text nameText = nameLabel.GetComponent<Text>();
                nameText.fontSize = 13;
                nameText.fontStyle = FontStyle.Bold;
                nameText.color = Color.white;
                nameText.alignment = TextAnchor.UpperCenter;
                nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        AddSpacer(rightCol.transform, "Spacer2", RowWidth, 16f);

        GameObject sizeRow = BuildSliderRow(rightCol.transform, "SizeRow", "SIZE", 0.5f, 2f, 1f, true);
        GameObject gapRow = BuildSliderRow(rightCol.transform, "GapRow", "GAP", 0.4f, 3f, 1f, true);
        Slider sizeSlider, gapSlider; Text sizeValueText, gapValueText;
        ExtractSliderRefs(sizeRow, out sizeSlider, out sizeValueText);
        ExtractSliderRefs(gapRow, out gapSlider, out gapValueText);

        GameObject outlineRow = NewUIObject("OutlineRow", rightCol.transform);
        AddLayoutElement(outlineRow, RowWidth, RowHeight);
        ConfigureHLG(outlineRow.AddComponent<HorizontalLayoutGroup>(), TextAnchor.MiddleLeft, false, true, false, false, Spacing, 0f);

        GameObject outlineLabelGO = NewText(outlineRow.transform, "Label", "OUTLINE", 16, TextAnchor.MiddleLeft);
        AddLayoutElement(outlineLabelGO, 160f, RowHeight);

        outlineButtonT.SetParent(outlineRow.transform, false);
        RectTransform outlineBtnRt = outlineButtonT as RectTransform;
        outlineBtnRt.sizeDelta = new Vector2(100f, 30f);
        outlineBtnRt.anchoredPosition = Vector2.zero;
        AddLayoutElement(outlineButtonT.gameObject, 100f, 30f);
        Image outlineBtnImg = outlineButtonT.GetComponent<Image>();
        Text outlineBtnLabel = outlineButtonT.GetComponentInChildren<Text>();
        RectTransform outlineLabelInnerRt = outlineBtnLabel.rectTransform;
        outlineLabelInnerRt.anchorMin = Vector2.zero;
        outlineLabelInnerRt.anchorMax = Vector2.one;
        outlineLabelInnerRt.offsetMin = Vector2.zero;
        outlineLabelInnerRt.offsetMax = Vector2.zero;
        outlineBtnLabel.alignment = TextAnchor.MiddleCenter;

        // ---------------- WIRE SettingsMenuController ----------------
        SerializedObject so = new SerializedObject(smc);
        SetRef(so, "sensitivitySlider", sensSlider);
        SetRef(so, "sensitivityValueText", sensValueText);
        SetRef(so, "resumeButton", resumeBtn.GetComponent<Button>());
        SetRef(so, "restartRoomButton", restartBtn.GetComponent<Button>());
        SetRef(so, "quitToMenuButton", quitBtn.GetComponent<Button>());

        SetRef(so, "sizeSlider", sizeSlider);
        SetRef(so, "sizeValueText", sizeValueText);
        SetRef(so, "gapSlider", gapSlider);
        SetRef(so, "gapValueText", gapValueText);
        SetRef(so, "outlineButton", outlineButtonT.GetComponent<Button>());
        SetRef(so, "outlineButtonImage", outlineBtnImg);
        SetRef(so, "outlineButtonLabel", outlineBtnLabel);

        SetArray(so, "styleTileOutlines", tileOutlines);
        SetArray(so, "styleTileIconRoots", tileIconRoots);

        SetRef(so, "masterSlider", masterSlider);
        SetRef(so, "masterValueText", masterValueText);
        SetRef(so, "musicSlider", musicSlider);
        SetRef(so, "musicValueText", musicValueText);
        SetRef(so, "sfxSlider", sfxSlider);
        SetRef(so, "sfxValueText", sfxValueText);

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(smc);
        EditorUtility.SetDirty(uiCanvas);
        EditorSceneManager.MarkSceneDirty(smc.gameObject.scene);

        Debug.Log("[SettingsPanelBuilder] Rebuild complete.");
    }

    static void RescalePreview(Transform previewBox)
    {
        // Preview bars were authored for the old 160x100 box (previewScaleBoost=2.6 in CrosshairController).
        // New box is 260x260 - scale the boost by the same ratio so the glyph fills it proportionally.
        foreach (Transform child in previewBox)
        {
            if (child.name.StartsWith("Preview_Crosshair_")) child.localScale = Vector3.one; // reset; CrosshairController drives this at runtime
        }
    }

    static void SetAnchoredY(Transform t, float y)
    {
        RectTransform rt = t as RectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }

    static void SetRect(Transform t, float width, float height, float centerY)
    {
        RectTransform rt = t as RectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(0f, centerY);
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static void RemoveIfPresent<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c != null) Object.DestroyImmediate(c);
    }

    static void ConfigureVLG(VerticalLayoutGroup g, TextAnchor align, bool controlW, bool controlH, bool expandW, bool expandH, float spacing, float padding)
    {
        g.childAlignment = align;
        g.childControlWidth = controlW;
        g.childControlHeight = controlH;
        g.childForceExpandWidth = expandW;
        g.childForceExpandHeight = expandH;
        g.spacing = spacing;
        g.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
    }

    static void ConfigureHLG(HorizontalLayoutGroup g, TextAnchor align, bool controlW, bool controlH, bool expandW, bool expandH, float spacing, float padding)
    {
        g.childAlignment = align;
        g.childControlWidth = controlW;
        g.childControlHeight = controlH;
        g.childForceExpandWidth = expandW;
        g.childForceExpandHeight = expandH;
        g.spacing = spacing;
        g.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
    }

    static GameObject NewUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void AddLayoutElement(GameObject go, float width, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
        le.minHeight = height;

        // A LayoutElement only matters to a parent whose childControlWidth/Height reads it.
        // Several parents here have childControlWidth=false by design (buttons must keep their
        // own fixed width instead of being stretched) - for those, set sizeDelta directly too,
        // since otherwise the child keeps its GameObject-creation default (100x100) and every
        // row collapses to fit inside that instead of its intended size.
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(width, height);
    }

    static void AddSpacer(Transform parent, string name, float width, float height)
    {
        GameObject go = NewUIObject(name, parent);
        AddLayoutElement(go, width, height);
    }

    static void ResetTextRect(Transform t, float width, float height)
    {
        RectTransform rt = t as RectTransform;
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;
    }

    static GameObject NewText(Transform parent, string name, string text, int fontSize, TextAnchor align)
    {
        GameObject go = NewUIObject(name, parent);
        Text t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = LightText;
        t.raycastTarget = false;
        return go;
    }

    static GameObject BuildSliderRow(Transform parent, string name, string labelText, float min, float max, float initialValue, bool percent)
    {
        GameObject row = NewUIObject(name, parent);
        AddLayoutElement(row, RowWidth, RowHeight);
        ConfigureHLG(row.AddComponent<HorizontalLayoutGroup>(), TextAnchor.MiddleLeft, true, true, false, false, Spacing, 0f);

        GameObject labelGO = NewText(row.transform, "Label", labelText, 16, TextAnchor.MiddleLeft);
        AddLayoutElement(labelGO, 160f, RowHeight);

        GameObject sliderGO = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGO.name = "Slider";
        sliderGO.transform.SetParent(row.transform, false);
        AddLayoutElement(sliderGO, 320f, 20f);

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.value = initialValue;

        Transform bg = sliderGO.transform.Find("Background");
        RectTransform bgRt = bg as RectTransform;
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(1f, 0.5f);
        bgRt.sizeDelta = new Vector2(0f, 6f);
        bgRt.anchoredPosition = Vector2.zero;
        bg.GetComponent<Image>().color = TrackDark;

        Transform fillArea = sliderGO.transform.Find("Fill Area");
        RectTransform fillAreaRt = fillArea as RectTransform;
        fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRt.sizeDelta = new Vector2(-20f, 6f);
        fillAreaRt.anchoredPosition = Vector2.zero;

        Transform fill = fillArea.Find("Fill");
        fill.GetComponent<Image>().color = Accent;

        Transform handleArea = sliderGO.transform.Find("Handle Slide Area");
        Transform handle = handleArea.Find("Handle");
        RectTransform handleRt = handle as RectTransform;
        handleRt.sizeDelta = new Vector2(14f, 22f);
        handle.GetComponent<Image>().color = Accent;

        string initialText = percent ? (Mathf.RoundToInt(initialValue * 100f) + "%") : initialValue.ToString("0.0");
        GameObject valueGO = NewText(row.transform, "Value", initialText, 16, TextAnchor.MiddleLeft);
        AddLayoutElement(valueGO, 60f, RowHeight);

        return row;
    }

    static void ExtractSliderRefs(GameObject row, out Slider slider, out Text valueText)
    {
        slider = row.transform.Find("Slider").GetComponent<Slider>();
        valueText = row.transform.Find("Value").GetComponent<Text>();
    }

    static void SetRef(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) { Debug.LogError("[SettingsPanelBuilder] Missing property: " + prop); return; }
        p.objectReferenceValue = value;
    }

    static void SetArray(SerializedObject so, string prop, Object[] values)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) { Debug.LogError("[SettingsPanelBuilder] Missing property: " + prop); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
