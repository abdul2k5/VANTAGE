using UnityEngine;

/// <summary>
/// The Range room's exit door stays locked until all 3 practice targets have been hit - the
/// room's entire lesson (learn to aim and fire) has to actually happen before you can leave, not
/// just be suggested by the objective text.
/// </summary>
public class RangeRoomGate : MonoBehaviour
{
    public RangeTarget[] targets;
    public DoorController door;
    public RoomManager roomManager;
    public int roomIndex;
    public string clearedObjective = "All 3 targets down. Door unlocked - head to the exit.";

    int hitCount;
    string originalObjective;

    void Start()
    {
        if (roomManager != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
            originalObjective = roomManager.rooms[roomIndex].objective;
        if (targets == null) return;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null) targets[i].OnHit += HandleHit;
        }
    }

    void HandleHit()
    {
        hitCount++;
        if (hitCount < targets.Length) return;

        if (door != null) door.Unlock();
        if (roomManager != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
        {
            roomManager.rooms[roomIndex].objective = clearedObjective;
            roomManager.RefreshObjectiveText();
        }
    }

    /// <summary>Room Select: targets count again, door locked, original objective back.</summary>
    public void ResetState()
    {
        hitCount = 0;
        if (targets != null)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null) targets[i].ResetHit();
            }
        }
        if (door != null) door.Relock();
        if (roomManager != null && originalObjective != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
            roomManager.rooms[roomIndex].objective = originalObjective;
    }
}
