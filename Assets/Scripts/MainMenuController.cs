using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gates player/weapon input while the start overlay is up. IsOpen defaults to false so
/// early milestones (no menu built yet) can test movement/shooting without it being blocked.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public GameObject menuPanel;

    public static bool IsOpen { get; private set; }
    /// <summary>Set by GameOverController's "RESTART FROM BEGINNING"/"RESTART" buttons before reloading the scene, so Awake() drops straight into Room 1 gameplay instead of showing the title screen again.</summary>
    public static bool SkipMainMenuOnNextLoad;


void Awake()
    {
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
    }

    public void OnPointerClickStart(PointerEventData eventData)
    {
        StartGame();
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

    void OnDestroy()
    {
        IsOpen = false;
    }
}
