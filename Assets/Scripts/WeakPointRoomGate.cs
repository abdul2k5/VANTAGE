using UnityEngine;

/// <summary>
/// The Weak Point room's exit door stays locked until every drone in the room is dead. All
/// headshot kills unlock it silently - that's the room's whole lesson, landed clean. If any kill
/// was a body shot it still unlocks (missing the eye shouldn't softlock the player) but swaps the
/// objective text to a short "try the eye next time" nudge instead of staying silent about it.
/// </summary>
public class WeakPointRoomGate : MonoBehaviour
{
    public Sentinel[] sentinels;
    public DoorController door;
    public RoomManager roomManager;
    public int roomIndex;
    public string headshotObjective = "Clean headshots. Door unlocked.";
    public string bodyShotObjective = "Door unlocked. Try the eye next time.";

    int remaining;
    bool allHeadshots = true;

    void Start()
    {
        foreach (var s in sentinels)
        {
            if (s == null) continue;
            remaining++;
            s.OnKilled += HandleKilled;
        }
    }

    void HandleKilled(bool headshot)
    {
        if (!headshot) allHeadshots = false;
        remaining--;
        if (remaining > 0) return;

        if (door != null) door.Unlock();
        if (roomManager != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
        {
            roomManager.rooms[roomIndex].objective = allHeadshots ? headshotObjective : bodyShotObjective;
            roomManager.RefreshObjectiveText();
        }
    }
}
