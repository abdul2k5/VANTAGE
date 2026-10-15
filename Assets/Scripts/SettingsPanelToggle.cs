using UnityEngine;
using UnityEngine.EventSystems;

public class SettingsPanelToggle : MonoBehaviour, IPointerClickHandler
{
    public GameObject panel;

public void OnPointerClick(PointerEventData eventData)
    {
        if (panel == null) return;

        bool opening = !panel.activeSelf;
        panel.SetActive(opening);

        // While the settings panel is open the player needs the real OS pointer,
        // unlocked, to pick an option; mouse-look is suspended for the same reason.
        Cursor.visible = opening;
        Cursor.lockState = opening ? CursorLockMode.None : CursorLockMode.Locked;
        if (CrosshairController.Instance != null)
        {
            CrosshairController.Instance.SetCrosshairVisible(!opening);
        }
    }
}
