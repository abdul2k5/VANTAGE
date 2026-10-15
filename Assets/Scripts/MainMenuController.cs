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

    void Awake()
    {
        IsOpen = menuPanel != null && menuPanel.activeSelf;
        // Render on top of whatever HUD elements exist, regardless of authoring order -
        // new UI keeps getting added later in the hierarchy as the project grows.
        if (IsOpen && menuPanel != null) menuPanel.transform.SetAsLastSibling();
        Cursor.visible = IsOpen;
        Cursor.lockState = IsOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = IsOpen ? 0f : 1f;
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUIClick();
            AudioManager.Instance.PlayGameplayMusic();
        }
    }

    void OnDestroy()
    {
        IsOpen = false;
    }
}
