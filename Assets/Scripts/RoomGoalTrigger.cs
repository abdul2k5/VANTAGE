using UnityEngine;

public class RoomGoalTrigger : MonoBehaviour
{
    public string roomLabel = "Room";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"{roomLabel} complete");
        }
    }
}
