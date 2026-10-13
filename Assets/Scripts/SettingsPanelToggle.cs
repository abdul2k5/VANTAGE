using UnityEngine;
using UnityEngine.EventSystems;

public class SettingsPanelToggle : MonoBehaviour, IPointerClickHandler
{
    public GameObject panel;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}
