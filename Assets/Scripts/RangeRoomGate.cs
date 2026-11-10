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
    [Tooltip("The objective line while targets remain. {0} = hit so far, {1} = total.")]
    public string progressObjective = "Targets hit {0}/{1}";

    int hitCount;

    void Start()
    {
        if (targets == null) return;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null) targets[i].OnHit += HandleHit;
        }
        ShowObjective(Progress());
    }

    void HandleHit()
    {
        hitCount++;
        if (hitCount < targets.Length)
        {
            ShowObjective(Progress());
            return;
        }

        if (door != null) door.Unlock();
        ShowObjective(clearedObjective);
    }

    string Progress()
    {
        return string.Format(progressObjective, hitCount, targets != null ? targets.Length : 0);
    }

    void ShowObjective(string text)
    {
        if (roomManager == null || roomIndex < 0 || roomIndex >= roomManager.rooms.Length) return;
        roomManager.rooms[roomIndex].objective = text;
        // Only redraw the line if this is the room on screen - this also runs from Start, possibly before the first room is entered.
        if (roomManager.CurrentRoom == roomIndex) roomManager.RefreshObjectiveText();
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
        ShowObjective(Progress());
    }
}
