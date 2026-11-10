using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The objective icon beside the settings gear. Clicking it re-opens the current room's card, the
/// same as pressing [Tab] (handled in AbilityUnlockCardUI, which owns the card).
/// </summary>
public class ObjectiveRecallButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (AbilityUnlockCardUI.Instance != null) AbilityUnlockCardUI.Instance.ReopenRoomCard();
    }
}
