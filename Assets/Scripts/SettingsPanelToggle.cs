using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Opens/closes the settings panel and pauses the game while it's up. Escape is the primary
/// way in - the gear icon alone isn't reachable mid-game since the cursor is locked to the
/// center of the screen for mouse-look, so there's nothing to click on until the cursor is
/// already free.
/// </summary>
public class SettingsPanelToggle : MonoBehaviour, IPointerClickHandler
{
    public GameObject panel;

    void Update()
    {
        if (MainMenuController.IsOpen) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    void Toggle()
    {
        if (panel == null) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        bool opening = !panel.activeSelf;
        if (opening) panel.transform.SetAsLastSibling(); // render on top of any HUD added since this was authored
        panel.SetActive(opening);

        // While the settings panel is open the player needs the real OS pointer,
        // unlocked, to pick an option; mouse-look and gameplay are suspended for the same reason.
        Cursor.visible = opening;
        Cursor.lockState = opening ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = opening ? 0f : 1f;

        if (CrosshairController.Instance != null)
        {
            CrosshairController.Instance.SetCrosshairVisible(!opening);
        }
    }
}
