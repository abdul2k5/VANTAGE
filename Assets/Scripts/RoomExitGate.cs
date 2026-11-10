using UnityEngine;

/// <summary>
/// Exit door gate for Rooms 3-7. The door stays locked until the player either uses the room's
/// utility (requiredAbility, while actually standing in this room) or drops every Sentinel listed
/// in sentinels. Before this, those doors started open and the player could sprint straight past
/// the room's lesson - "utility before you peek" was only suggested by the objective text, never
/// required. Killing everything still counts so a player who wins the fight the hard way isn't
/// softlocked. Holding Angle has no ability yet, so its gate is sentinels-only.
/// A room with two crossings (Dash) also lists followUpAbilities: the door then needs the required
/// ability and any one of those, in either order.
/// </summary>
public class RoomExitGate : MonoBehaviour
{
    public DoorController door;
    public RoomManager roomManager;
    public int roomIndex;
    public Ability requiredAbility;
    [Tooltip("Optional. If set, one of these must also be used in the room before the door opens.")]
    public Ability[] followUpAbilities;
    public Sentinel[] sentinels;
    public string unlockedObjective = "Door unlocked - head to the exit.";
    [Tooltip("Objective once the required ability is used but no follow-up yet. Only used with Follow Up Abilities.")]
    public string followUpObjective = "";
    [Tooltip("Objective once a follow-up is used but the required ability isn't yet.")]
    public string requiredObjective = "";

    string originalObjective;
    bool unlocked;
    bool requiredUsed;
    bool followUpUsed;

    void Start()
    {
        if (roomManager != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
            originalObjective = roomManager.rooms[roomIndex].objective;

        if (requiredAbility != null) requiredAbility.OnUsed += HandleAbilityUsed;
        if (followUpAbilities != null)
        {
            for (int i = 0; i < followUpAbilities.Length; i++)
            {
                if (followUpAbilities[i] != null) followUpAbilities[i].OnUsed += HandleFollowUpUsed;
            }
        }
        if (sentinels != null)
        {
            for (int i = 0; i < sentinels.Length; i++)
            {
                if (sentinels[i] != null) sentinels[i].OnKilled += HandleKilled;
            }
        }
    }

    bool NeedsFollowUp { get { return followUpAbilities != null && followUpAbilities.Length > 0; } }

    void HandleAbilityUsed()
    {
        if (roomManager == null || roomManager.CurrentRoom != roomIndex) return;
        requiredUsed = true;
        CheckAbilities();
    }

    void HandleFollowUpUsed()
    {
        if (roomManager == null || roomManager.CurrentRoom != roomIndex) return;
        followUpUsed = true;
        CheckAbilities();
    }

    void CheckAbilities()
    {
        if (unlocked) return;
        if (requiredUsed && (followUpUsed || !NeedsFollowUp)) { Open(); return; }
        // Half done: say what's still missing.
        string next = requiredUsed ? followUpObjective : requiredObjective;
        if (!string.IsNullOrEmpty(next)) SetObjective(next);
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
        SetObjective(unlockedObjective);
    }

    void SetObjective(string text)
    {
        if (roomManager == null || roomIndex < 0 || roomIndex >= roomManager.rooms.Length) return;
        roomManager.rooms[roomIndex].objective = text;
        roomManager.RefreshObjectiveText();
    }

    /// <summary>Checkpoint retry: re-lock the door and restore the room's original objective.</summary>
    public void ResetState()
    {
        unlocked = false;
        requiredUsed = false;
        followUpUsed = false;
        if (door != null) door.Relock();
        if (roomManager != null && originalObjective != null && roomIndex >= 0 && roomIndex < roomManager.rooms.Length)
            roomManager.rooms[roomIndex].objective = originalObjective;
    }

    void OnDestroy()
    {
        if (requiredAbility != null) requiredAbility.OnUsed -= HandleAbilityUsed;
        if (followUpAbilities != null)
        {
            for (int i = 0; i < followUpAbilities.Length; i++)
            {
                if (followUpAbilities[i] != null) followUpAbilities[i].OnUsed -= HandleFollowUpUsed;
            }
        }
    }
}
