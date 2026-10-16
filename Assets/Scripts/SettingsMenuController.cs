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
    public Color tabActiveColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    public Color tabInactiveColor = new Color(0.16f, 0.17f, 0.15f, 1f);

    [Header("General Tab")]
    public PlayerController player;
    public Slider sensitivitySlider;
    public Text sensitivityValueText;
    public Button resumeButton;
    public Button restartRoomButton;
    public Button quitToMenuButton;

    [Header("Crosshair Tab")]
    public Image[] styleButtonBorders;
    public GameObject[] colorChipSelectedRings;
    public Slider sizeSlider;
    public Slider gapSlider;
    public Button outlineButton;
    public Text outlineButtonLabel;

    [Header("Audio Tab")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    int currentTab;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Close);
        if (restartRoomButton != null) restartRoomButton.onClick.AddListener(RestartRoom);
        if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(QuitToMenu);

        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => ShowTab(idx));
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
        if (MainMenuController.IsOpen) return;

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

        if (CrosshairController.Instance != null) CrosshairController.Instance.SetCrosshairVisible(false);

        RefreshAllControlsFromState();
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
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = i == index ? tabActiveColor : tabInactiveColor;
            }
        }
    }

    void RestartRoom()
    {
        if (RoomManager.Instance != null) RoomManager.Instance.EnterRoom(RoomManager.Instance.CurrentRoom);
        Close();
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
    }

    void OnGapChanged(float v)
    {
        if (CrosshairController.Instance != null) CrosshairController.Instance.SetGap(v);
    }

    void OnOutlineToggleClicked()
    {
        if (CrosshairController.Instance == null) return;
        CrosshairController.Instance.SetOutline(!CrosshairController.Instance.CurrentOutline);
        UpdateOutlineLabel();
    }

    void OnMasterChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(v);
    }

    void OnMusicChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
    }

    void OnSfxChanged(float v)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v);
    }

    void UpdateOutlineLabel()
    {
        if (outlineButtonLabel == null || CrosshairController.Instance == null) return;
        outlineButtonLabel.text = "OUTLINE: " + (CrosshairController.Instance.CurrentOutline ? "ON" : "OFF");
    }

    void RefreshAllControlsFromState()
    {
        if (player != null && sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(player.mouseSensitivity);
            if (sensitivityValueText != null) sensitivityValueText.text = player.mouseSensitivity.ToString("0.0");
        }

        if (CrosshairController.Instance != null)
        {
            if (sizeSlider != null) sizeSlider.SetValueWithoutNotify(CrosshairController.Instance.CurrentSize);
            if (gapSlider != null) gapSlider.SetValueWithoutNotify(CrosshairController.Instance.CurrentGap);
            UpdateOutlineLabel();
        }

        if (AudioManager.Instance != null)
        {
            if (masterSlider != null) masterSlider.SetValueWithoutNotify(AudioManager.Instance.masterVolume);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.Instance.musicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
        }
    }

    void RefreshHighlights()
    {
        if (CrosshairController.Instance == null) return;

        int style = CrosshairController.Instance.CurrentStyle;
        if (styleButtonBorders != null)
        {
            for (int i = 0; i < styleButtonBorders.Length; i++)
            {
                if (styleButtonBorders[i] != null) styleButtonBorders[i].color = i == style ? tabActiveColor : tabInactiveColor;
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
