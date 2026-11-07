using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Owns the settings panel: open/close (gear icon or Escape), the dim overlay, hiding the
/// gameplay HUD while paused, tab switching (General/Crosshair/Audio), and wiring every
/// control in those tabs back to the system it configures. Replaces the old SettingsPanelToggle,
/// which only handled open/close for a single flat (overlapping) layout.
/// </summary>
public class SettingsMenuController : MonoBehaviour, IPointerClickHandler
{
    public static SettingsMenuController Instance;

    [Header("Panel")]
    public GameObject panel;
    public Image dimOverlay;
    public GameObject[] gameplayHudRoots;

    [Header("Tabs")]
    public Button[] tabButtons;
    public GameObject[] tabPanels;
    /// <summary>The one accent colour: selected tab label and marker, toggles that are ON, selected crosshair tile.</summary>
    public Color tabActiveColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    /// <summary>OFF toggles and other neutral buttons.</summary>
    public Color tabInactiveColor = new Color(0.16f, 0.17f, 0.15f, 1f);
    public Color tabActiveTextColor = new Color(0.08f, 0.09f, 0.07f, 1f);
    public Color tabInactiveTextColor = new Color(0.85f, 0.85f, 0.82f, 1f);
    /// <summary>Sidebar tab backgrounds: the selected one is lifted slightly, the rest blend into the sidebar.</summary>
    public Color sidebarTabActiveBg = new Color(0.13f, 0.14f, 0.13f, 1f);
    public Color sidebarTabInactiveBg = new Color(0.13f, 0.14f, 0.13f, 0f);
    public Color sidebarTabInactiveText = new Color(0.6f, 0.6f, 0.57f, 1f);

    [Header("Header + footer")]
    public Text headerTitle;
    public Text headerSubtitle;
    public string[] tabTitles;
    public string[] tabSubtitles;
    public Text hintText;
    public string defaultHint = "Hover a setting to see what it does.";
    public float tabFadeSeconds = 0.16f;
    public float tabSlideDistance = 14f;

    [Header("General Tab")]
    public PlayerController player;
    public Slider sensitivitySlider;
    public Text sensitivityValueText;
    public Button resumeButton;
    public Button restartRoomButton;
    public Button quitToMenuButton;
    [Tooltip("PlayerPrefs 'CheckpointsEnabled', default ON. OFF = hardcore: any death restarts from Room 1, no checkpoint banner.")]
    public Button checkpointsToggleButton;
    public Image checkpointsToggleButtonImage;
    public Text checkpointsToggleButtonLabel;


    [Header("Crosshair Tab")]
    /// <summary>2px outline per style tile - enabled + accent-colored only on the selected tile. Never a solid fill, so the icon underneath always stays visible.</summary>
    public Outline[] styleTileOutlines;
    /// <summary>Icon bars inside each style tile (the small crosshair-shape preview) - recolored accent/light together with the outline.</summary>
    public RectTransform[] styleTileIconRoots;
    public GameObject[] colorChipSelectedRings;
    public Slider sizeSlider;
    public Text sizeValueText;
    public Slider gapSlider;
    public Text gapValueText;
    public Button outlineButton;
    public Image outlineButtonImage;
    public Text outlineButtonLabel;

    [Header("Audio Tab")]
    public Slider masterSlider;
    public Text masterValueText;
    public Slider musicSlider;
    public Text musicValueText;
    public Slider sfxSlider;
    public Text sfxValueText;

    int currentTab;
    Coroutine tabFade;
    string shownHint;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Close);
        if (restartRoomButton != null) restartRoomButton.onClick.AddListener(RestartRoom);
        if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(QuitToMenu);
        if (checkpointsToggleButton != null) checkpointsToggleButton.onClick.AddListener(OnCheckpointsToggleClicked);


        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => OnTabClicked(idx));
            }
        }

        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (sizeSlider != null) sizeSlider.onValueChanged.AddListener(OnSizeChanged);
        if (gapSlider != null) gapSlider.onValueChanged.AddListener(OnGapChanged);
        if (outlineButton != null) outlineButton.onClick.AddListener(OnOutlineToggleClicked);

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    void Update()
    {
        if (MainMenuController.IsOpen || AbilityUnlockCardUI.IsShowingCard) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Toggle();

        if (panel != null && panel.activeSelf) RefreshHighlights();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    public void Toggle()
    {
        if (panel == null) return;
        if (panel.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        if (dimOverlay != null)
        {
            dimOverlay.gameObject.SetActive(true);
            dimOverlay.transform.SetAsLastSibling();
        }
        panel.transform.SetAsLastSibling(); // render on top of dim overlay + any HUD added since this was authored
        panel.SetActive(true);

        SetHudVisible(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (CrosshairController.Instance != null) CrosshairController.Instance.SetCrosshairVisible(false);

        RefreshAllControlsFromState();
        ShowHint(null);
        ShowTab(currentTab);
    }

    public void Close()
    {
        if (panel == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        panel.SetActive(false);
        if (dimOverlay != null) dimOverlay.gameObject.SetActive(false);

        SetHudVisible(true);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (CrosshairController.Instance != null) CrosshairController.Instance.SetCrosshairVisible(true);
    }

    void SetHudVisible(bool visible)
    {
        if (gameplayHudRoots == null) return;
        foreach (var go in gameplayHudRoots)
        {
            if (go != null) go.SetActive(visible);
        }
    }

    void OnTabClicked(int index)
    {
        if (index == currentTab) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        ShowTab(index);
    }

    public void ShowTab(int index)
    {
        if (tabPanels == null || index < 0 || index >= tabPanels.Length) return;
        currentTab = index;

        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null) tabPanels[i].SetActive(i == index);
        }

        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                bool isActive = i == index;
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = isActive ? sidebarTabActiveBg : sidebarTabInactiveBg;

                var label = tabButtons[i].GetComponentInChildren<Text>();
                if (label != null) label.color = isActive ? tabActiveColor : sidebarTabInactiveText;

                var marker = tabButtons[i].transform.Find("Indicator");
                if (marker != null) marker.gameObject.SetActive(isActive);
            }
        }

        if (headerTitle != null && tabTitles != null && index < tabTitles.Length) headerTitle.text = tabTitles[index];
        if (headerSubtitle != null && tabSubtitles != null && index < tabSubtitles.Length) headerSubtitle.text = tabSubtitles[index];

        if (tabPanels[index] != null && isActiveAndEnabled && Application.isPlaying)
        {
            if (tabFade != null) StopCoroutine(tabFade);
            tabFade = StartCoroutine(FadeInTab(tabPanels[index]));
        }
    }

    /// <summary>Fade and slide the new tab in. Runs on unscaled time - the game is paused while the menu is open.</summary>
    System.Collections.IEnumerator FadeInTab(GameObject tab)
    {
        var group = tab.GetComponent<CanvasGroup>();
        if (group == null) group = tab.AddComponent<CanvasGroup>();
        var rt = (RectTransform)tab.transform;

        float t = 0f;
        while (t < tabFadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tabFadeSeconds);
            float eased = 1f - (1f - k) * (1f - k);
            group.alpha = eased;
            rt.anchoredPosition = new Vector2((1f - eased) * tabSlideDistance, 0f);
            yield return null;
        }
        group.alpha = 1f;
        rt.anchoredPosition = Vector2.zero;
        tabFade = null;
    }

    /// <summary>Footer text for whatever the pointer is over. Null or empty puts back the default.</summary>
    public void ShowHint(string text)
    {
        shownHint = text;
        if (hintText != null) hintText.text = string.IsNullOrEmpty(text) ? defaultHint : text;
    }

    /// <summary>Only clears if this hint is still the one showing - moving straight from one row to the next fires the new enter before the old exit.</summary>
    public void ClearHint(string text)
    {
        if (shownHint == text) ShowHint(null);
    }

    /// <summary>Same as picking the current room in Room Select: the room resets and the player is put back as they arrived.
    /// It used to only move the player to the entrance, leaving dead drones dead and the round running.</summary>
    void RestartRoom()
    {
        Close(); // first - the jump's fade runs on scaled time
        if (RoomManager.Instance != null) RoomManager.Instance.JumpToRoom(RoomManager.Instance.CurrentRoom);
    }

    void QuitToMenu()
    {
        // A full scene reload is the cleanest reset here - room index, health, Sentinel
        // state, ability cooldowns, ultimate charge, and any active round are all back to
        // their authored defaults, same as a fresh launch, with the main menu panel active
        // again exactly as it's saved in the scene.
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void OnSensitivityChanged(float v)
    {
        if (player != null) player.SetSensitivity(v);
        if (sensitivityValueText != null) sensitivityValueText.text = v.ToString("0.0");
    }

    void OnSizeChanged(float v)
    {
        if (CrosshairController.Instance != null) CrosshairController.Instance.SetSize(v);
        if (sizeValueText != null) sizeValueText.text = FormatPercent(v);
    }

    void OnGapChanged(float v)
    {
        if (CrosshairController.Instance != null) CrosshairController.Instance.SetGap(v);
        if (gapValueText != null) gapValueText.text = FormatPercent(v);
    }

    void OnOutlineToggleClicked()
    {
        if (CrosshairController.Instance == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        CrosshairController.Instance.SetOutline(!CrosshairController.Instance.CurrentOutline);
        UpdateOutlineLabel();
    }

    void OnMasterChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(v);
        if (masterValueText != null) masterValueText.text = FormatPercent(v);
    }

    void OnMusicChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
        if (musicValueText != null) musicValueText.text = FormatPercent(v);
    }

    void OnSfxChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v);
        if (sfxValueText != null) sfxValueText.text = FormatPercent(v);
    }

    static string FormatPercent(float v01)
    {
        return Mathf.RoundToInt(v01 * 100f) + "%";
    }

    void UpdateOutlineLabel()
    {
        if (CrosshairController.Instance == null) return;
        bool on = CrosshairController.Instance.CurrentOutline;
        if (outlineButtonLabel != null)
        {
            outlineButtonLabel.text = on ? "ON" : "OFF";
            outlineButtonLabel.color = on ? tabActiveTextColor : tabInactiveTextColor;
        }
        if (outlineButtonImage != null) outlineButtonImage.color = on ? tabActiveColor : tabInactiveColor;
    }

void OnCheckpointsToggleClicked()
    {
        bool on = PlayerPrefs.GetInt("CheckpointsEnabled", 1) == 1;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        PlayerPrefs.SetInt("CheckpointsEnabled", on ? 0 : 1);
        UpdateCheckpointsToggleLabel();
    }

    void UpdateCheckpointsToggleLabel()
    {
        bool on = PlayerPrefs.GetInt("CheckpointsEnabled", 1) == 1;
        if (checkpointsToggleButtonLabel != null)
        {
            checkpointsToggleButtonLabel.text = on ? "ON" : "OFF";
            checkpointsToggleButtonLabel.color = on ? tabActiveTextColor : tabInactiveTextColor;
        }
        if (checkpointsToggleButtonImage != null) checkpointsToggleButtonImage.color = on ? tabActiveColor : tabInactiveColor;
    }


    void RefreshAllControlsFromState()
    {
        UpdateCheckpointsToggleLabel();

        if (player != null && sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(player.mouseSensitivity);
            if (sensitivityValueText != null) sensitivityValueText.text = player.mouseSensitivity.ToString("0.0");
        }

        if (CrosshairController.Instance != null)
        {
            if (sizeSlider != null) sizeSlider.SetValueWithoutNotify(CrosshairController.Instance.CurrentSize);
            if (sizeValueText != null) sizeValueText.text = FormatPercent(CrosshairController.Instance.CurrentSize);
            if (gapSlider != null) gapSlider.SetValueWithoutNotify(CrosshairController.Instance.CurrentGap);
            if (gapValueText != null) gapValueText.text = FormatPercent(CrosshairController.Instance.CurrentGap);
            UpdateOutlineLabel();
        }

        if (AudioManager.Instance != null)
        {
            if (masterSlider != null) masterSlider.SetValueWithoutNotify(AudioManager.Instance.masterVolume);
            if (masterValueText != null) masterValueText.text = FormatPercent(AudioManager.Instance.masterVolume);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.Instance.musicVolume);
            if (musicValueText != null) musicValueText.text = FormatPercent(AudioManager.Instance.musicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
            if (sfxValueText != null) sfxValueText.text = FormatPercent(AudioManager.Instance.sfxVolume);
        }
    }

    void RefreshHighlights()
    {
        if (CrosshairController.Instance == null) return;

        int style = CrosshairController.Instance.CurrentStyle;
        if (styleTileOutlines != null)
        {
            for (int i = 0; i < styleTileOutlines.Length; i++)
            {
                bool selected = i == style;
                if (styleTileOutlines[i] != null)
                {
                    styleTileOutlines[i].enabled = selected;
                    styleTileOutlines[i].effectColor = tabActiveColor;
                }
                if (styleTileIconRoots != null && i < styleTileIconRoots.Length && styleTileIconRoots[i] != null)
                {
                    Color iconColor = selected ? tabActiveColor : tabInactiveTextColor;
                    var images = styleTileIconRoots[i].GetComponentsInChildren<Image>(true);
                    for (int j = 0; j < images.Length; j++) images[j].color = iconColor;
                }
            }
        }

        int color = CrosshairController.Instance.CurrentColor;
        if (colorChipSelectedRings != null)
        {
            for (int i = 0; i < colorChipSelectedRings.Length; i++)
            {
                if (colorChipSelectedRings[i] != null) colorChipSelectedRings[i].SetActive(i == color);
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
