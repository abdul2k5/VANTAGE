using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.GoToNextRoom();
        }
    }
}
