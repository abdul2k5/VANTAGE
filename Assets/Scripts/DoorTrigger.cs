using UnityEngine;

/// <summary>
/// The airlock trigger in each doorway: walking through moves the player on to the next room.
/// It sits on the door line and reaches 0.3 m in front of the closed panels, so walking into a
/// locked door used to set it off too (found at the expo). It now only fires once its door is open.
/// </summary>
public class DoorTrigger : MonoBehaviour
{
    [Tooltip("The door this trigger belongs to. Left empty, the nearest door is used - each airlock sits in its own door's frame.")]
    public DoorController door;

    void Awake()
    {
        if (door == null) door = NearestDoor();
    }

    // Stay, not Enter: a player already pressed against the door when it unlocks is let through without backing off first.
    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (door != null && !door.IsOpen) return;
        if (RoomManager.Instance != null && !RoomManager.Instance.IsTransitioning)
        {
            RoomManager.Instance.GoToNextRoom();
        }
    }

    DoorController NearestDoor()
    {
        DoorController best = null;
        float bestDist = 3f;
        var doors = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < doors.Length; i++)
        {
            float d = Vector3.Distance(doors[i].transform.position, transform.position);
            if (d < bestDist) { bestDist = d; best = doors[i]; }
        }
        return best;
    }
}
