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

        // While the settings panel is open the player needs the real OS pointer
        // to pick an option; the custom crosshair is for aiming, not for menus.
        Cursor.visible = opening;
        if (CrosshairController.Instance != null)
        {
            CrosshairController.Instance.SetCrosshairVisible(!opening);
        }
    }
}
