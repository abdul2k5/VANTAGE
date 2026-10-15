using UnityEngine;
using UnityEngine.EventSystems;

public class CrosshairColorButton : MonoBehaviour, IPointerClickHandler
{
    public int colorIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CrosshairController.Instance != null)
        {
            CrosshairController.Instance.SetColor(colorIndex);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
    }
}
