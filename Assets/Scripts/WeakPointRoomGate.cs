using UnityEngine;

/// <summary>
/// The Weak Point room's exit door stays locked until its Sentinel is dead. A headshot kill
/// unlocks it silently - that's the room's whole lesson, landed clean. A body-shot kill still
/// unlocks it (missing the eye shouldn't softlock the player) but swaps the objective text to a
/// short "try the eye next time" nudge instead of staying silent about it.
/// </summary>
public class WeakPointRoomGate : MonoBehaviour
{
    public Sentinel sentinel;
    public DoorController door;
    public RoomManager roomManager;
    public int roomIndex;
    public string headshotObjective = "Clean headshot. Door unlocked.";
    public string bodyShotObjective = "Door unlocked. Try the eye next time.";

    void Start()
    {
        if (sentinel != null) sentinel.OnKilled += HandleKilled;
    }

    void HandleKilled(bool headshot)
    {
        if (door != null) door.Unlock();
        if (roomManager != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
        {
            roomManager.rooms[roomIndex].objective = headshot ? headshotObjective : bodyShotObjective;
            roomManager.RefreshObjectiveText();
        }
    }
}
