using UnityEngine;

/// <summary>
/// Exit door gate for Rooms 3-7. The door stays locked until the player either uses the room's
/// utility (requiredAbility, while actually standing in this room) or drops every Sentinel listed
/// in sentinels. Before this, those doors started open and the player could sprint straight past
/// the room's lesson - "utility before you peek" was only suggested by the objective text, never
/// required. Killing everything still counts so a player who wins the fight the hard way isn't
/// softlocked. Holding Angle has no ability yet, so its gate is sentinels-only.
/// </summary>
public class RoomExitGate : MonoBehaviour
{
    public DoorController door;
    public RoomManager roomManager;
    public int roomIndex;
    public Ability requiredAbility;
    public Sentinel[] sentinels;
    public string unlockedObjective = "Door unlocked - head to the exit.";

    string originalObjective;
    bool unlocked;

    void Start()
    {
        if (roomManager != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
            originalObjective = roomManager.rooms[roomIndex].objective;

        if (requiredAbility != null) requiredAbility.OnUsed += HandleAbilityUsed;
        if (sentinels != null)
        {
            for (int i = 0; i < sentinels.Length; i++)
            {
                if (sentinels[i] != null) sentinels[i].OnKilled += HandleKilled;
            }
        }
    }

    void HandleAbilityUsed()
    {
        if (roomManager == null || roomManager.CurrentRoom != roomIndex) return;
        Open();
    }

    void HandleKilled(bool headshot)
    {
        // Counted from live state, not a running tally, so Sentinel.ResetState() on checkpoint
        // retry (which re-activates them) needs no extra bookkeeping here.
        for (int i = 0; i < sentinels.Length; i++)
        {
            if (sentinels[i] != null && sentinels[i].gameObject.activeSelf) return;
        }
        Open();
    }

    void Open()
    {
        if (unlocked) return;
        unlocked = true;
        if (door != null) door.Unlock();
        if (roomManager != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
        {
            roomManager.rooms[roomIndex].objective = unlockedObjective;
            roomManager.RefreshObjectiveText();
        }
    }

    /// <summary>Checkpoint retry: re-lock the door and restore the room's original objective.</summary>
    public void ResetState()
    {
        unlocked = false;
        if (door != null) door.Relock();
        if (roomManager != null && originalObjective != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
            roomManager.rooms[roomIndex].objective = originalObjective;
    }

    void OnDestroy()
    {
        if (requiredAbility != null) requiredAbility.OnUsed -= HandleAbilityUsed;
    }
}
