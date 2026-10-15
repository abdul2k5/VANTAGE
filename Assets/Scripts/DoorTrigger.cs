using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.GoToNextRoom();
        }
    }
}
