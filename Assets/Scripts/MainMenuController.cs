using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Gates player/weapon input while the start overlay is up. IsOpen defaults to false so
/// early milestones (no menu built yet) can test movement/shooting without it being blocked.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public GameObject menuPanel;
    [Tooltip("Choose Your Weapon - shown by the Start button, on top of the menu. Built by Vantage > Rebuild Guns.")]
    public GameObject loadoutPanel;
    [Tooltip("The CREDITS page over the menu. Its body text is filled from GameCredits, the same text as the win screen's credits.")]
    public GameObject creditsPanel;
    public UnityEngine.UI.Text creditsBody;
    public Weapon weapon;

    public static bool IsOpen { get; private set; }
    /// <summary>Set by GameOverController's "RESTART FROM BEGINNING"/"RESTART" buttons before reloading the scene, so Awake() drops straight into Room 1 gameplay instead of showing the title screen again.</summary>
    public static bool SkipMainMenuOnNextLoad;


void Awake()
    {
        if (loadoutPanel != null) loadoutPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (creditsBody != null) creditsBody.text = GameCredits.Build(creditsBody.fontSize);
        if (SkipMainMenuOnNextLoad)
        {
            SkipMainMenuOnNextLoad = false;
            IsOpen = false;
            if (menuPanel != null) menuPanel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            return;
        }

        IsOpen = menuPanel != null && menuPanel.activeSelf;
        // Render on top of whatever HUD elements exist, regardless of authoring order -
        // new UI keeps getting added later in the hierarchy as the project grows.
        if (IsOpen && menuPanel != null) menuPanel.transform.SetAsLastSibling();
        Cursor.visible = IsOpen;
        Cursor.lockState = IsOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = IsOpen ? 0f : 1f;
        AudioListener.pause = IsOpen;
        // AudioManager survives the scene reload that brings the player back here, still playing
        // whatever the run ended on. Null on the very first load, where its own Awake starts the menu track.
        if (IsOpen && AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
    }

    public void OnPointerClickStart(PointerEventData eventData)
    {
        ShowLoadout();
    }

    /// <summary>The Start button: the run begins once a gun has been picked. With no loadout screen built, it starts straight away.</summary>
    public void ShowLoadout()
    {
        if (loadoutPanel == null || weapon == null || weapon.GunCount < 2) { StartGame(); return; }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        loadoutPanel.SetActive(true);
        loadoutPanel.transform.SetAsLastSibling();
    }

    /// <summary>A card on the Choose Your Weapon screen: that gun is carried for the whole run.</summary>
    public void ChooseGun(int index)
    {
        if (weapon != null) weapon.Equip(index);
        if (loadoutPanel != null) loadoutPanel.SetActive(false);
        StartGame();
    }

    /// <summary>The SETTINGS button: the pause menu's settings panel, opened over the title screen.</summary>
    public void OpenSettings()
    {
        if (SettingsMenuController.Instance != null) SettingsMenuController.Instance.OpenFromMainMenu();
    }

    /// <summary>The CREDITS button.</summary>
    public void OpenCredits()
    {
        if (creditsPanel == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        creditsPanel.SetActive(true);
        creditsPanel.transform.SetAsLastSibling();
    }

    /// <summary>The credits page's BACK button, and Escape.</summary>
    public void CloseCredits()
    {
        if (creditsPanel == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        creditsPanel.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        // Escape steps back from whatever is open over the title screen.
        if (kb.escapeKey.wasPressedThisFrame && !SliderValueField.IsEditing)
        {
            var settings = SettingsMenuController.Instance;
            if (settings != null && settings.IsOpenFromMainMenu) { settings.Close(); return; }
            if (creditsPanel != null && creditsPanel.activeSelf) { CloseCredits(); return; }
        }

        if (loadoutPanel == null || !loadoutPanel.activeInHierarchy) return;
        if (kb.digit1Key.wasPressedThisFrame) ChooseGun(0);
        else if (kb.digit2Key.wasPressedThisFrame) ChooseGun(1);
        else if (kb.digit3Key.wasPressedThisFrame) ChooseGun(2);
        else if (kb.escapeKey.wasPressedThisFrame) loadoutPanel.SetActive(false);
    }

    public void StartGame()
    {
        IsOpen = false;
        if (menuPanel != null) menuPanel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
            // RoomManager owns gameplay/round music from here - EnterRoom(0) already ran in its
            // own Start() but skipped the switch while the menu was still open; trigger it now.
            AudioManager.Instance.PlayGameplayMusic();
        }
    }

    /// <summary>The EXIT GAME button under START. Nothing is lost by quitting here: best times are saved as they are set.
    /// In the editor it stops Play mode instead, since Application.Quit does nothing there.</summary>
    public void QuitGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        IsOpen = false;
    }
}
