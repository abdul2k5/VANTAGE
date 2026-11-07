using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays out the Settings panel: a sidebar of tabs on the left, a header (tab title + one-line
/// subtitle) and body on the right, and a footer holding the hover hint plus QUIT TO MENU / RESUME.
/// Every setting is one full-width row: label left, control right, divider underneath.
///
/// Works on the live, already-wired controls - found through SettingsMenuController's own
/// references, not by path - and only moves and restyles them, so no reference breaks and the
/// menu can be re-run after tweaking any value below.
/// </summary>
public static class SettingsPanelBuilder
{
    static readonly Color Accent = new Color(0.886f, 0.647f, 0.235f, 1f);
    static readonly Color PanelBg = new Color(0.075f, 0.08f, 0.075f, 0.97f);
    static readonly Color SidebarBg = new Color(0.05f, 0.055f, 0.05f, 1f);
    static readonly Color ButtonDark = new Color(0.16f, 0.17f, 0.15f, 1f);
    static readonly Color TrackDark = new Color(0.2f, 0.21f, 0.19f, 1f);
    static readonly Color ValueBoxBg = new Color(0.04f, 0.045f, 0.04f, 1f);
    static readonly Color LightText = new Color(0.85f, 0.85f, 0.82f, 1f);
    static readonly Color DarkText = new Color(0.08f, 0.09f, 0.07f, 1f);
    static readonly Color MutedText = new Color(0.55f, 0.55f, 0.52f, 1f);
    static readonly Color Rule = new Color(1f, 1f, 1f, 0.06f);

    const float PanelW = 980f;
    const float PanelH = 640f;
    const float SidebarW = 220f;
    const float HeaderH = 108f;
    const float FooterH = 80f;
    const float BodyPadX = 32f;   // body is 980 - 220 - 2*32 = 696 wide, 640 - 108 - 80 = 452 tall
    const float RowH = 52f;
    const float TabH = 48f;

    [MenuItem("Vantage/Rebuild Settings Panel")]
    public static void Rebuild()
    {
        var smc = Object.FindFirstObjectByType<SettingsMenuController>(FindObjectsInactive.Include);
        var rooms = Object.FindFirstObjectByType<RoomSelectMenu>(FindObjectsInactive.Include);
        var panel = (RectTransform)smc.panel.transform;
        Font font = smc.sensitivityValueText.font;

        // ---------------- FRAME ----------------
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(PanelW, PanelH);
        panel.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = PanelBg;

        var sidebar = Child(panel, "Sidebar");
        Place(sidebar, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(SidebarW, 0f));
        Fill(sidebar, SidebarBg);
        sidebar.SetSiblingIndex(0);

        var sidebarRule = Child(sidebar, "SidebarRule");
        Place(sidebarRule, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(1f, 0f));
        Fill(sidebarRule, Rule);

        var topAccent = Child(panel, "TopAccent");
        Place(topAccent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 2f));
        Fill(topAccent, Accent);

        var eyebrow = Label(sidebar, "Eyebrow", font, "PAUSED", 11, Accent, TextAnchor.UpperLeft);
        Place(eyebrow.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(28f, -30f), new Vector2(160f, 16f));

        Transform titleT = panel.Find("SettingsTitle");
        if (titleT == null) titleT = sidebar.Find("SettingsTitle");
        titleT.SetParent(sidebar, false);
        var title = titleT.GetComponent<Text>();
        StyleText(title, font, "SETTINGS", 24, LightText, TextAnchor.UpperLeft);
        Place(title.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(28f, -46f), new Vector2(180f, 32f));

        var tabList = Child(sidebar, "TabList");
        Place(tabList, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(0f, smc.tabButtons.Length * (TabH + 2f)));
        var tabLayout = GetOrAdd<VerticalLayoutGroup>(tabList.gameObject);
        Configure(tabLayout, TextAnchor.UpperLeft, 2f, 0);
        tabLayout.childForceExpandWidth = true;

        foreach (var tab in smc.tabButtons) StyleTab(tab, tabList, font);
        Transform oldTabRow = panel.Find("TabRow");
        if (oldTabRow != null) Object.DestroyImmediate(oldTabRow.gameObject);

        var escHint = Label(sidebar, "EscHint", font, "ESC  ·  RESUME", 12, MutedText, TextAnchor.LowerLeft);
        Place(escHint.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(28f, 26f), new Vector2(170f, 18f));

        var content = Child(panel, "Content");
        Place(content, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        content.offsetMin = new Vector2(SidebarW, 0f);
        content.offsetMax = Vector2.zero;

        var headerTitle = Label(content, "HeaderTitle", font, "GENERAL", 22, LightText, TextAnchor.UpperLeft);
        Place(headerTitle.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(36f, -30f), new Vector2(560f, 30f));
        var headerSub = Label(content, "HeaderSubtitle", font, "", 13, MutedText, TextAnchor.UpperLeft);
        Place(headerSub.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(36f, -62f), new Vector2(680f, 20f));
        var headerRule = Child(content, "HeaderRule");
        Inset(headerRule, new Vector2(0f, 1f), -92f, 36f);
        Fill(headerRule, Rule);

        var body = Child(content, "Body");
        Place(body, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        body.offsetMin = new Vector2(BodyPadX, FooterH);
        body.offsetMax = new Vector2(-BodyPadX, -HeaderH);

        var footer = Child(content, "Footer");
        Place(footer, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, FooterH));
        var footerRule = Child(footer, "FooterRule");
        Inset(footerRule, new Vector2(0f, 1f), 0f, 36f);
        Fill(footerRule, Rule);

        var hint = Label(footer, "HintText", font, smc.defaultHint, 13, MutedText, TextAnchor.MiddleLeft);
        Place(hint.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        hint.rectTransform.offsetMin = new Vector2(36f, 0f);
        hint.rectTransform.offsetMax = new Vector2(-(36f + 150f + 12f + 150f + 24f), 0f);
        hint.horizontalOverflow = HorizontalWrapMode.Wrap;

        FooterButton(smc.resumeButton, footer, font, "RESUME", -36f, Accent, DarkText, "Back to the game. Esc does the same.");
        FooterButton(smc.quitToMenuButton, footer, font, "QUIT TO MENU", -36f - 150f - 12f, ButtonDark, LightText, "Back to the title screen. This run's progress is lost.");

        foreach (var tp in smc.tabPanels)
        {
            var rt = (RectTransform)tp.transform;
            rt.SetParent(body, false);
            Place(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            GetOrAdd<CanvasGroup>(tp);
        }

        // ---------------- GENERAL ----------------
        var general = smc.sensitivitySlider.transform.parent.parent;
        Configure(GetOrAdd<VerticalLayoutGroup>(general.gameObject), TextAnchor.UpperLeft, 0f, 0);
        SliderRow(smc.sensitivitySlider, smc.sensitivityValueText, font, 280f, "How fast the view turns with the mouse. Aiming down the scope turns at about half this.");
        ToggleRow(smc.checkpointsToggleButton, font, "ON: dying from Room 5 onwards lets you retry from the Room 5 checkpoint. OFF: any death sends you back to Room 1.");

        // ---------------- AUDIO ----------------
        var audio = smc.masterSlider.transform.parent.parent;
        Configure(GetOrAdd<VerticalLayoutGroup>(audio.gameObject), TextAnchor.UpperLeft, 0f, 0);
        DestroyChild(audio, "VolumeHeader");
        SliderRow(smc.masterSlider, smc.masterValueText, font, 280f, "Everything: music, effects and menu sounds.");
        SliderRow(smc.musicSlider, smc.musicValueText, font, 280f, "Background music only.");
        SliderRow(smc.sfxSlider, smc.sfxValueText, font, 280f, "Gunfire, abilities, drones and menu sounds.");

        // ---------------- ROOMS ----------------
        var roomsTab = rooms.transform;
        Configure(GetOrAdd<VerticalLayoutGroup>(roomsTab.gameObject), TextAnchor.UpperLeft, 20f, 0);
        DestroyChild(roomsTab, "RoomsHeader");
        DestroyChild(roomsTab, "RoomsHint");
        var grid = Child(roomsTab, "RoomGrid");
        var gridLayout = GetOrAdd<GridLayoutGroup>(grid.gameObject);
        gridLayout.cellSize = new Vector2(162f, 150f);
        gridLayout.spacing = new Vector2(16f, 16f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        Size(grid.gameObject, 696f, 316f);
        grid.SetSiblingIndex(0);

        var restart = smc.restartRoomButton;
        restart.transform.SetParent(roomsTab, false);
        restart.transform.SetSiblingIndex(1);
        Size(restart.gameObject, 240f, 40f);
        StyleButton(restart, font, "RESTART CURRENT ROOM", ButtonDark, LightText);
        Hint(restart.gameObject, null, "Reset the room you're in and start it again from the entrance.");

        rooms.grid = grid;
        rooms.font = font;
        rooms.settings = smc;
        EditorUtility.SetDirty(rooms);

        // ---------------- CROSSHAIR ----------------
        var crosshair = smc.lengthSlider.transform.parent.parent.parent; // row -> RightColumn -> tab
        var columns = GetOrAdd<HorizontalLayoutGroup>(crosshair.gameObject);
        Configure(columns, TextAnchor.UpperLeft, 24f, 0);
        var left = crosshair.Find("LeftColumn");
        var right = crosshair.Find("RightColumn");
        Size(left.gameObject, 200f, 452f);
        Size(right.gameObject, 472f, 452f);
        Configure(GetOrAdd<VerticalLayoutGroup>(left.gameObject), TextAnchor.UpperLeft, 0f, 0);
        Configure(GetOrAdd<VerticalLayoutGroup>(right.gameObject), TextAnchor.UpperLeft, 6f, 0);
        var previewBox = left.Find("PreviewBox");
        Size(previewBox.gameObject, 200f, 200f);
        // Mid-grey, not near-black: the crosshair outline is black, so a dark box hid it entirely.
        previewBox.GetComponent<Image>().color = new Color(0.36f, 0.38f, 0.37f, 1f);
        EditorUtility.SetDirty(previewBox.GetComponent<Image>());

        var cc = Object.FindFirstObjectByType<CrosshairController>(FindObjectsInactive.Include);
        Sprite dotSprite = BuildCrosshairRoots(cc, previewBox);

        // Heights: 22 + 64 + 8 + 22 + 58 + 8 + 4 rows x 52, plus 9 gaps of 6 = 444 of the 452 available.
        // At most 4 rows show at once - DOT hides the 3 line rows and shows Dot Size instead.
        SectionHeader(right.Find("StyleHeader"), font, "STYLE");
        BuildStyleTiles(smc, right.Find("TileRow"), dotSprite);
        Size(right.Find("Spacer1").gameObject, 472f, 8f);
        SectionHeader(right.Find("ColorHeader"), font, "COLOR");
        Size(right.Find("ChipRow").gameObject, 472f, 58f);
        Size(right.Find("Spacer2").gameObject, 472f, 8f);

        var gapRow = smc.gapSlider.transform.parent;
        var so0 = new SerializedObject(smc);
        if (smc.thicknessSlider == null) CloneRow(gapRow, "ThicknessRow", so0, "thicknessSlider", "thicknessValueText");
        if (smc.dotSizeSlider == null) CloneRow(gapRow, "DotSizeRow", so0, "dotSizeSlider", "dotSizeValueText");
        so0.ApplyModifiedPropertiesWithoutUndo();
        smc.lengthSlider.transform.parent.name = "LengthRow";

        PixelRow(smc.lengthSlider, smc.lengthValueText, font, "LENGTH", CrosshairController.MinLength, CrosshairController.MaxLength, "How long each line is, in pixels.");
        PixelRow(smc.thicknessSlider, smc.thicknessValueText, font, "THICKNESS", CrosshairController.MinThickness, CrosshairController.MaxThickness, "How thick each line is, in pixels. Also sets the centre dot of CROSS + DOT.");
        PixelRow(smc.gapSlider, smc.gapValueText, font, "GAP", CrosshairController.MinGap, CrosshairController.MaxGap, "Space between the centre and the lines, in pixels. The lines also push out while your shots are spreading.");
        PixelRow(smc.dotSizeSlider, smc.dotSizeValueText, font, "DOT SIZE", CrosshairController.MinDot, CrosshairController.MaxDot, "Width of the dot, in pixels.");
        ToggleRow(smc.outlineButton, font, "2px black border around the crosshair so it stays visible on bright walls.");

        int order = right.Find("Spacer2").GetSiblingIndex() + 1;
        foreach (var s in new[] { smc.lengthSlider, smc.thicknessSlider, smc.gapSlider, smc.dotSizeSlider })
            s.transform.parent.SetSiblingIndex(order++);
        smc.outlineButton.transform.parent.SetSiblingIndex(order);
        // Saved for the default CROSS style; the menu swaps line rows and the dot row per style when it opens.
        foreach (var s in new[] { smc.lengthSlider, smc.thicknessSlider, smc.gapSlider }) s.transform.parent.gameObject.SetActive(true);
        smc.dotSizeSlider.transform.parent.gameObject.SetActive(false);

        foreach (var ring in smc.colorChipSelectedRings)
        {
            var swatch = ring.transform.parent.Find(ring.name.Replace("ColorRing", "Swatch"));
            if (swatch != null) Hint(swatch.gameObject, null, "Crosshair colour.");
        }

        // ---------------- WIRE ----------------
        var so = new SerializedObject(smc);
        so.FindProperty("headerTitle").objectReferenceValue = headerTitle;
        so.FindProperty("headerSubtitle").objectReferenceValue = headerSub;
        so.FindProperty("hintText").objectReferenceValue = hint;
        var titles = so.FindProperty("tabTitles");
        var subtitles = so.FindProperty("tabSubtitles");
        titles.arraySize = subtitles.arraySize = smc.tabPanels.Length;
        for (int i = 0; i < smc.tabPanels.Length; i++)
        {
            string t, s;
            TabText(smc.tabPanels[i].name, out t, out s);
            titles.GetArrayElementAtIndex(i).stringValue = t;
            subtitles.GetArrayElementAtIndex(i).stringValue = s;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(smc);
        EditorSceneManager.MarkSceneDirty(smc.gameObject.scene);
        Debug.Log("[SettingsPanelBuilder] Rebuild complete.");
    }

    static readonly Vector2 TopLeft = new Vector2(0f, 1f);

    static void TabText(string panelName, out string title, out string subtitle)
    {
        switch (panelName)
        {
            case "TabPanel_Rooms": title = "ROOM SELECT"; subtitle = "Replay a cleared room. A room resets to how you first found it when you enter."; return;
            case "TabPanel_Crosshair": title = "CROSSHAIR"; subtitle = "Changes show in the preview straight away."; return;
            case "TabPanel_Audio": title = "AUDIO"; subtitle = "100% is the standard mix."; return;
            default: title = "GENERAL"; subtitle = "Mouse and run options."; return;
        }
    }

    // ---------------- ROWS ----------------

    static void SliderRow(Slider slider, Text value, Font font, float sliderWidth, string hint)
    {
        var row = (RectTransform)slider.transform.parent;
        StyleRow(row, font, hint);

        Size(slider.gameObject, sliderWidth, 20f);
        var bg = slider.transform.Find("Background");
        if (bg != null) bg.GetComponent<Image>().color = TrackDark;
        if (slider.fillRect != null) slider.fillRect.GetComponent<Image>().color = Accent;
        if (slider.handleRect != null)
        {
            slider.handleRect.sizeDelta = new Vector2(12f, 0f);
            slider.handleRect.GetComponent<Image>().color = LightText;
        }

        // The number sits in a small dark box so it reads as a value, not a second label.
        Transform box = value.transform.parent;
        if (box.name != "ValueBox")
        {
            var boxGo = new GameObject("ValueBox", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(row, false);
            boxGo.transform.SetSiblingIndex(value.transform.GetSiblingIndex());
            value.transform.SetParent(boxGo.transform, false);
            box = boxGo.transform;
        }
        box.GetComponent<Image>().color = ValueBoxBg;
        box.GetComponent<Image>().raycastTarget = false;
        Size(box.gameObject, 72f, 32f);
        Stretch(value.rectTransform);
        StyleText(value, font, null, 15, Accent, TextAnchor.MiddleCenter);
    }

    static void ToggleRow(Button toggle, Font font, string hint)
    {
        StyleRow((RectTransform)toggle.transform.parent, font, hint);
        Size(toggle.gameObject, 96f, 32f);
        var label = toggle.GetComponentInChildren<Text>(true);
        Stretch(label.rectTransform);
        StyleText(label, font, null, 14, LightText, TextAnchor.MiddleCenter);
    }

    static void PixelRow(Slider slider, Text value, Font font, string label, float min, float max, string hint)
    {
        SliderRow(slider, value, font, 200f, hint);
        slider.transform.parent.Find("Label").GetComponent<Text>().text = label;
        slider.wholeNumbers = true;
        slider.minValue = min;
        slider.maxValue = max;
        EditorUtility.SetDirty(slider);
    }

    /// <summary>Copies an existing slider row (keeps its look and layout) and wires the copy into the given SettingsMenuController fields.</summary>
    static void CloneRow(Transform template, string name, SerializedObject so, string sliderField, string valueField)
    {
        var clone = Object.Instantiate(template.gameObject, template.parent);
        clone.name = name;
        var slider = clone.GetComponentInChildren<Slider>(true);
        slider.onValueChanged = new Slider.SliderEvent();
        so.FindProperty(sliderField).objectReferenceValue = slider;
        so.FindProperty(valueField).objectReferenceValue = clone.transform.Find("ValueBox/Value").GetComponent<Text>();
    }

    // ---------------- CROSSHAIR SHAPES ----------------

    static readonly string[] TileKeys = { "CROSS", "CROSSDOT", "DOT", "T" };
    static readonly string[] TileHints =
    {
        "Cross: four lines around the centre.",
        "Cross + Dot: four lines with a small square in the middle.",
        "Dot: a single round point.",
        "T-Shape: a cross without the top line, so nothing covers what's above your aim.",
    };

    /// <summary>
    /// Swaps the old prebuilt crosshair shapes (one object per style, scaled by percentages) for one
    /// empty root each in the HUD and the preview box; CrosshairController draws the parts into them
    /// at runtime. Returns the round sprite for the DOT style.
    /// </summary>
    static Sprite BuildCrosshairRoots(CrosshairController cc, Transform previewBox)
    {
        Transform canvas = cc.transform;
        Sprite dot = cc.dotSprite;
        var oldDot = canvas.Find("Crosshair_Dot");
        if (dot == null && oldDot != null && oldDot.childCount > 0) dot = oldDot.GetChild(0).GetComponent<Image>().sprite;

        var centre = new Vector2(0.5f, 0.5f);
        var hud = Child(canvas, "Crosshair");
        var oldCross = canvas.Find("Crosshair_Cross");
        if (oldCross != null) hud.SetSiblingIndex(oldCross.GetSiblingIndex());
        Place(hud, centre, centre, centre, Vector2.zero, Vector2.zero);
        foreach (var n in new[] { "Crosshair_Cross", "Crosshair_Dot", "Crosshair_Brackets" }) DestroyChild(canvas, n);

        var preview = Child(previewBox, "Preview_Crosshair_Live");
        Place(preview, centre, centre, centre, Vector2.zero, Vector2.zero);
        foreach (var n in new[] { "Preview_Crosshair_Cross", "Preview_Crosshair_Dot", "Preview_Crosshair_Brackets" }) DestroyChild(previewBox, n);

        var so = new SerializedObject(cc);
        so.FindProperty("hudRoot").objectReferenceValue = hud;
        so.FindProperty("previewRoot").objectReferenceValue = preview;
        so.FindProperty("dotSprite").objectReferenceValue = dot;
        so.ApplyModifiedPropertiesWithoutUndo();
        return dot;
    }

    /// <summary>One 64x64 tile per style, in CrosshairController's style order, each with a small drawn icon.</summary>
    static void BuildStyleTiles(SettingsMenuController smc, Transform row, Sprite dot)
    {
        var template = row.Find("Option_CROSS");
        DestroyChild(row, "Option_BRACKETS");
        var outlines = new Outline[TileKeys.Length];
        var icons = new RectTransform[TileKeys.Length];
        for (int i = 0; i < TileKeys.Length; i++)
        {
            var tile = row.Find("Option_" + TileKeys[i]);
            if (tile == null)
            {
                tile = Object.Instantiate(template.gameObject, row).transform;
                tile.name = "Option_" + TileKeys[i];
            }
            tile.SetSiblingIndex(i);
            var option = tile.GetComponent<CrosshairOptionButton>();
            option.styleIndex = i;
            EditorUtility.SetDirty(option);
            Size(tile.gameObject, 64f, 64f);

            var icon = (RectTransform)tile.Find("Icon");
            for (int c = icon.childCount - 1; c >= 0; c--) Object.DestroyImmediate(icon.GetChild(c).gameObject);
            if (i != CrosshairController.StyleDot)
            {
                IconBar(icon, new Vector2(-9f, 0f), new Vector2(10f, 2f), null);
                IconBar(icon, new Vector2(9f, 0f), new Vector2(10f, 2f), null);
                IconBar(icon, new Vector2(0f, -9f), new Vector2(2f, 10f), null);
                if (i != CrosshairController.StyleT) IconBar(icon, new Vector2(0f, 9f), new Vector2(2f, 10f), null);
            }
            if (i == CrosshairController.StyleCrossDot) IconBar(icon, Vector2.zero, new Vector2(2f, 2f), null);
            if (i == CrosshairController.StyleDot) IconBar(icon, Vector2.zero, new Vector2(10f, 10f), dot);

            Hint(tile.gameObject, null, "Crosshair shape. " + TileHints[i]);
            outlines[i] = tile.GetComponent<Outline>();
            icons[i] = icon;
        }
        Size(row.gameObject, 472f, 64f);

        var so = new SerializedObject(smc);
        var outlineProp = so.FindProperty("styleTileOutlines");
        var iconProp = so.FindProperty("styleTileIconRoots");
        outlineProp.arraySize = iconProp.arraySize = TileKeys.Length;
        for (int i = 0; i < TileKeys.Length; i++)
        {
            outlineProp.GetArrayElementAtIndex(i).objectReferenceValue = outlines[i];
            iconProp.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void IconBar(RectTransform icon, Vector2 position, Vector2 size, Sprite sprite)
    {
        var go = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(icon, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
    }

    static void StyleRow(RectTransform row, Font font, string hint)
    {
        var bg = GetOrAdd<Image>(row.gameObject);
        bg.color = new Color(1f, 1f, 1f, 0f);
        bg.raycastTarget = true;

        var layout = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        Configure(layout, TextAnchor.MiddleLeft, 16f, 0);
        layout.padding = new RectOffset(12, 12, 0, 0);

        var le = GetOrAdd<LayoutElement>(row.gameObject);
        le.preferredWidth = -1f;
        le.flexibleWidth = 1f;
        le.minHeight = le.preferredHeight = RowH;

        var label = row.Find("Label").GetComponent<Text>();
        StyleText(label, font, null, 16, LightText, TextAnchor.MiddleLeft);
        var labelLe = GetOrAdd<LayoutElement>(label.gameObject);
        labelLe.preferredWidth = -1f;
        labelLe.minWidth = 100f;
        labelLe.flexibleWidth = 1f;
        labelLe.preferredHeight = RowH;

        var divider = Child(row, "Divider");
        GetOrAdd<LayoutElement>(divider.gameObject).ignoreLayout = true;
        Place(divider, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 1f));
        Fill(divider, Rule);

        Hint(row.gameObject, bg, hint);
    }

    static void SectionHeader(Transform t, Font font, string text)
    {
        StyleText(t.GetComponent<Text>(), font, text, 12, MutedText, TextAnchor.LowerLeft);
        Size(t.gameObject, 472f, 22f);
    }

    // ---------------- SIDEBAR + FOOTER ----------------

    static void StyleTab(Button tab, Transform list, Font font)
    {
        tab.transform.SetParent(list, false);
        var le = GetOrAdd<LayoutElement>(tab.gameObject);
        le.minHeight = le.preferredHeight = TabH;
        le.preferredWidth = -1f;

        var label = tab.transform.Find("Label").GetComponent<Text>();
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(28f, 0f);
        StyleText(label, font, null, 15, LightText, TextAnchor.MiddleLeft);

        var indicator = Child(tab.transform, "Indicator");
        Place(indicator, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 0f));
        Fill(indicator, Accent);

        // ShowTab owns the tab's own background colour, so hover gets its own layer: a faint white
        // overlay that the button's colour tint fades from invisible (normal) to visible (hover).
        var hover = Child(tab.transform, "Hover");
        Place(hover, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Fill(hover, new Color(1f, 1f, 1f, 0.05f));
        hover.SetSiblingIndex(0);
        tab.targetGraphic = hover.GetComponent<Image>();
        var colors = tab.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        colors.fadeDuration = 0.08f;
        tab.colors = colors;
        tab.transition = Selectable.Transition.ColorTint;
    }

    static void FooterButton(Button button, Transform footer, Font font, string text, float rightX, Color bg, Color fg, string hint)
    {
        button.transform.SetParent(footer, false);
        var rt = (RectTransform)button.transform;
        Place(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(rightX, 0f), new Vector2(150f, 40f));
        StyleButton(button, font, text, bg, fg);
        Hint(button.gameObject, null, hint);
    }

    static void StyleButton(Button button, Font font, string text, Color bg, Color fg)
    {
        button.GetComponent<Image>().color = bg;
        var label = button.GetComponentInChildren<Text>(true);
        Stretch(label.rectTransform);
        StyleText(label, font, text, 14, fg, TextAnchor.MiddleCenter);
    }

    static void Hint(GameObject go, Graphic highlight, string text)
    {
        var h = GetOrAdd<SettingsHoverHint>(go);
        h.text = text;
        h.highlight = highlight;
    }

    // ---------------- HELPERS ----------------

    static RectTransform Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        return (RectTransform)t;
    }

    static void DestroyChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        Place(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    /// <summary>A 1px horizontal rule across the parent at height y (from the anchor), inset from both sides.</summary>
    static void Inset(RectTransform rt, Vector2 anchorY, float y, float inset)
    {
        Place(rt, new Vector2(0f, anchorY.y), new Vector2(1f, anchorY.y), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(-2f * inset, 1f));
    }

    static void Fill(RectTransform rt, Color color)
    {
        var img = GetOrAdd<Image>(rt.gameObject);
        img.color = color;
        img.raycastTarget = false;
    }

    static Text Label(Transform parent, string name, Font font, string text, int size, Color color, TextAnchor alignment)
    {
        var rt = Child(parent, name);
        var t = GetOrAdd<Text>(rt.gameObject);
        StyleText(t, font, text, size, color, alignment);
        t.raycastTarget = false;
        return t;
    }

    static void StyleText(Text t, Font font, string text, int size, Color color, TextAnchor alignment)
    {
        if (text != null) t.text = text;
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = alignment;
        t.fontStyle = FontStyle.Normal;
    }

    static void Size(GameObject go, float width, float height)
    {
        var le = GetOrAdd<LayoutElement>(go);
        le.preferredWidth = width;
        le.minHeight = le.preferredHeight = height;
        ((RectTransform)go.transform).sizeDelta = new Vector2(width, height);
    }

    static void Configure(HorizontalOrVerticalLayoutGroup g, TextAnchor align, float spacing, int padding)
    {
        g.childAlignment = align;
        g.childControlWidth = true;
        g.childControlHeight = true;
        g.childForceExpandWidth = false;
        g.childForceExpandHeight = false;
        g.spacing = spacing;
        g.padding = new RectOffset(padding, padding, padding, padding);
    }
}
