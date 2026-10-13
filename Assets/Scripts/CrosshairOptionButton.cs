using UnityEngine;
using UnityEngine.EventSystems;

public class CrosshairOptionButton : MonoBehaviour, IPointerClickHandler
{
    public int styleIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CrosshairController.Instance != null)
        {
            CrosshairController.Instance.SetStyle(styleIndex);
        }
    }
}
