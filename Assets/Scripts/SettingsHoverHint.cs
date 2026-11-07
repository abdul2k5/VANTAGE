using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Put on a settings row, tile or button: hovering it shows a one-line explanation in the settings
/// footer, and optionally lights up the row behind it so it's clear which setting the text is about.
/// </summary>
public class SettingsHoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea] public string text;
    public Graphic highlight;
    public Color hoverColor = new Color(1f, 1f, 1f, 0.045f);
    public Color restColor = new Color(1f, 1f, 1f, 0f);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight != null) highlight.color = hoverColor;
        if (SettingsMenuController.Instance != null) SettingsMenuController.Instance.ShowHint(text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Clear();
    }

    void OnDisable()
    {
        // Switching tabs hides the row under the pointer without an exit event.
        Clear();
    }

    void Clear()
    {
        if (highlight != null) highlight.color = restColor;
        if (SettingsMenuController.Instance != null) SettingsMenuController.Instance.ClearHint(text);
    }
}
