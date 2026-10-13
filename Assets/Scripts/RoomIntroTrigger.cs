using UnityEngine;

public class RoomIntroTrigger : MonoBehaviour
{
    public GameObject panel;
    bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        if (panel != null) panel.SetActive(true);
    }
}
