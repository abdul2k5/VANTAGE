using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class RoomDefinition
{
    public string title = "READ THE LINE";
    public string objective = "";
    public Transform entryPoint;
}

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public RoomDefinition[] rooms;
    public Transform player;
    public Image fadeOverlay;
    public Text objectiveLabel;
    public FollowCamera followCamera;
    public AbilityController abilities;
    public float fadeSeconds = 0.35f;
    public float holdSeconds = 0.45f;
    [Tooltip("Drones can't see the player while a room transition is faded, nor for this long after it clears. Rooms 5-8's crossfire drone can see the entry spot, so without this the player was shot during the black fade before they could see anything.")]
    public float entryGraceSeconds = 1f;

    int currentRoom = -1;
    int furthestRoom = -1;
    bool transitioning;
    float entryGraceUntil = -1f;

    // The player as they first walked into the furthest room. Going back there through Room Select puts them
    // back exactly so, the same way the room itself is reset, so a replay can't top up health or Overwatch.
    float arrivalHealth = -1f;
    float arrivalUltCharge;

    /// <summary>Read by Sentinel.CanSeePlayer - true while the player is arriving in a room.</summary>
    public bool EntryGraceActive { get { return Time.time < entryGraceUntil; } }
    /// <summary>Grace holds for as long as the screen is black - call before teleporting the player.</summary>
    public void HoldEntryGrace() { entryGraceUntil = float.PositiveInfinity; }
    /// <summary>Starts the post-fade countdown - call once the fade back in has finished.</summary>
    public void ReleaseEntryGrace() { entryGraceUntil = Time.time + entryGraceSeconds; }
    /// <summary>Firing ends the grace early, so it can't be used to shoot drones that can't see you back.</summary>
    public void CancelEntryGrace() { entryGraceUntil = -1f; }

    public int CurrentRoom { get { return currentRoom; } }
    /// <summary>The furthest room reached this run. Room Select offers every room up to and including it.</summary>
    public int FurthestRoom { get { return furthestRoom; } }
    /// <summary>True while the player is back in a room they've already completed.</summary>
    public bool IsReplaying { get { return currentRoom >= 0 && currentRoom < furthestRoom; } }
    /// <summary>True from the fade-out until the new room has faded back in.</summary>
    public bool IsTransitioning { get { return transitioning; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnterRoom(0, false);
    }

    public void GoToNextRoom()
    {
        if (transitioning) return;
        int next = currentRoom + 1;
        if (next >= rooms.Length) return;
        StartCoroutine(TransitionTo(next));
    }

    /// <summary>Room Select (and Restart Room): fades to the start of any room reached so far, reset to how it was first found.</summary>
    public void JumpToRoom(int index)
    {
        if (transitioning || index < 0 || index > furthestRoom) return;
        if (RunStats.Instance != null) RunStats.Instance.RecordRoomSelect();
        // Before the fade, not after it - the room being left still has live drones until the screen is black.
        HoldEntryGrace();
        StartCoroutine(TransitionTo(index));
    }

    /// <summary>Puts one room back the way it was authored: drones, pickups, the exit door and its objective, and the bomb site in The Round.</summary>
    public void ResetRoom(int index)
    {
        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sentinels.Length; i++)
        {
            if (sentinels[i].OwnerRoomIndex == index) sentinels[i].ResetState();
        }

        // Inactive included - a collected orb or medkit has disabled itself.
        var orbs = Object.FindObjectsByType<UltOrbPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i].roomIndex == index) orbs[i].ResetState();
        }

        var medkits = Object.FindObjectsByType<MedkitPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < medkits.Length; i++)
        {
            if (medkits[i].roomIndex == index) medkits[i].ResetState();
        }

        var exitGates = Object.FindObjectsByType<RoomExitGate>(FindObjectsSortMode.None);
        for (int i = 0; i < exitGates.Length; i++)
        {
            if (exitGates[i].roomIndex == index) exitGates[i].ResetState();
        }

        var rangeGates = Object.FindObjectsByType<RangeRoomGate>(FindObjectsSortMode.None);
        for (int i = 0; i < rangeGates.Length; i++)
        {
            if (rangeGates[i].roomIndex == index) rangeGates[i].ResetState();
        }

        var weakPointGates = Object.FindObjectsByType<WeakPointRoomGate>(FindObjectsSortMode.None);
        for (int i = 0; i < weakPointGates.Length; i++)
        {
            if (weakPointGates[i].roomIndex == index) weakPointGates[i].ResetState();
        }

        if (index == rooms.Length - 1)
        {
            if (RoundController.Instance != null) RoundController.Instance.ResetState();
            // Separate from the round's state - left planted, the site ignores the plant key on the way back.
            var site = Object.FindFirstObjectByType<BreachChargeSite>(FindObjectsInactive.Include);
            if (site != null) site.ResetState();
        }

        // Utility still out (a smoke standing, a dart revealing) belongs to the last visit.
        if (abilities != null) abilities.ClearDeployed();
    }

    /// <summary>Checkpoint retry: rooms after the checkpoint count as not reached again, and the player's restored state becomes the new arrival snapshot.</summary>
    public void RewindProgressTo(int index)
    {
        furthestRoom = index;
        CaptureArrivalState();
    }

    void CaptureArrivalState()
    {
        var health = player != null ? player.GetComponent<PlayerHealth>() : null;
        arrivalHealth = health != null ? health.CurrentHealth : -1f;
        arrivalUltCharge = UltimateController.Instance != null ? UltimateController.Instance.CurrentChargeAmount : 0f;
    }

    /// <summary>Going back into a room already visited: the room starts fresh, and so does the player - full health for a replay,
    /// or exactly as they first arrived when returning to the furthest room.</summary>
    void PrepareRevisit(int index)
    {
        ResetRoom(index);

        var health = player != null ? player.GetComponent<PlayerHealth>() : null;
        var weapon = player != null ? player.GetComponent<Weapon>() : null;
        // Cancel first - SetCharge is ignored while the ultimate is running.
        if (UltimateController.Instance != null) UltimateController.Instance.CancelUltimate();

        if (health != null)
        {
            health.FullRestore();
            if (index == furthestRoom && arrivalHealth > 0f) health.SetHealth(arrivalHealth);
        }
        if (index == furthestRoom && UltimateController.Instance != null) UltimateController.Instance.SetCharge(arrivalUltCharge);
        if (weapon != null) weapon.Reload();
    }

public void EnterRoom(int index, bool movePlayer = true)
    {
        if (index < 0 || index >= rooms.Length) return;

        currentRoom = index;
        var room = rooms[index];

        if (movePlayer && player != null && room.entryPoint != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = room.entryPoint.position;
            player.rotation = room.entryPoint.rotation;
            if (cc != null) cc.enabled = true;

            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.SetRespawnPoint(room.entryPoint.position);
        }

        // Armor is a per-room budget, like ability charges.
        var playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
        if (playerHealth != null) playerHealth.RefillShield();

        // The camera must not smooth-slide across the gap between rooms.
        if (followCamera != null) followCamera.SnapToTarget();

        if (abilities != null)
        {
            abilities.UnlockForRoom(currentRoom);
            abilities.ResetAllCooldowns();
        }

        UpdateObjectiveLine();
        UpdateMusic();
        if (currentRoom > furthestRoom)
        {
            furthestRoom = currentRoom;
            CaptureArrivalState();
        }
        if (CheckpointManager.Instance != null) CheckpointManager.Instance.NotifyRoomEntered(currentRoom);

    }

    /// <summary>Every room shares the same gameplay track except the last (The Round), which swaps to round-specific music. Skipped while the main menu is still up - that keeps menu music playing through the automatic EnterRoom(0) call in Start().</summary>
    void UpdateMusic()
    {
        if (AudioManager.Instance == null || MainMenuController.IsOpen) return;
        if (currentRoom == rooms.Length - 1) AudioManager.Instance.PlayRoundMusic();
        else AudioManager.Instance.PlayGameplayMusic();
    }

    /// <summary>Re-applies the current room's objective line - used by room-gate scripts (e.g. RangeRoomGate) that mutate a RoomDefinition's objective string after a door unlocks, so the HUD updates without a full room re-entry.</summary>
    public void RefreshObjectiveText()
    {
        UpdateObjectiveLine();
    }

    /// <summary>The one line of text on screen during play: where you are and what to do now. Teaching is the room card's job.</summary>
    void UpdateObjectiveLine()
    {
        if (objectiveLabel == null) return;

        var room = rooms[currentRoom];
        string where = "<color=#E2A53C><b>ROOM " + (currentRoom + 1) + "/" + rooms.Length + "  " + room.title.ToUpper() + "</b></color>";
        objectiveLabel.text = string.IsNullOrEmpty(room.objective) ? where : where + "    " + room.objective;
    }

    IEnumerator TransitionTo(int index)
    {
        transitioning = true;

        yield return Fade(0f, 1f);

        HoldEntryGrace();
        if (index <= furthestRoom) PrepareRevisit(index);
        EnterRoom(index);
        yield return new WaitForSeconds(holdSeconds);

        yield return Fade(1f, 0f);
        ReleaseEntryGrace();

        transitioning = false;
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeOverlay == null) yield break;

        float t = 0f;
        Color c = fadeOverlay.color;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeSeconds));
            fadeOverlay.color = c;
            yield return null;
        }
        c.a = to;
        fadeOverlay.color = c;
    }
}
