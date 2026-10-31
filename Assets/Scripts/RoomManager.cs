using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class RoomDefinition
{
    public string title = "READ THE LINE";
    public string objective = "";
    public Transform entryPoint;
    public GameObject introPanel;
}

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public RoomDefinition[] rooms;
    public Transform player;
    public Image fadeOverlay;
    public Text roomTitleLabel;
    public Text objectiveLabel;
    public FollowCamera followCamera;
    public AbilityController abilities;
    public float fadeSeconds = 0.35f;
    public float holdSeconds = 0.45f;
    [Tooltip("Drones can't see the player while a room transition is faded, nor for this long after it clears. Rooms 5-8's crossfire drone can see the entry spot, so without this the player was shot during the black fade before they could see anything.")]
    public float entryGraceSeconds = 1f;

    int currentRoom = -1;
    bool transitioning;
    float entryGraceUntil = -1f;

    /// <summary>Read by Sentinel.CanSeePlayer - true while the player is arriving in a room.</summary>
    public bool EntryGraceActive { get { return Time.time < entryGraceUntil; } }
    /// <summary>Grace holds for as long as the screen is black - call before teleporting the player.</summary>
    public void HoldEntryGrace() { entryGraceUntil = float.PositiveInfinity; }
    /// <summary>Starts the post-fade countdown - call once the fade back in has finished.</summary>
    public void ReleaseEntryGrace() { entryGraceUntil = Time.time + entryGraceSeconds; }
    /// <summary>Firing ends the grace early, so it can't be used to shoot drones that can't see you back.</summary>
    public void CancelEntryGrace() { entryGraceUntil = -1f; }

    public int CurrentRoom { get { return currentRoom; } }

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

        // The camera must not smooth-slide across the gap between rooms.
        if (followCamera != null) followCamera.SnapToTarget();

        if (abilities != null)
        {
            abilities.UnlockForRoom(currentRoom);
            abilities.ResetAllCooldowns();
        }

        ShowOnlyCurrentPanel();
        UpdateTitle();
        UpdateMusic();
        if (CheckpointManager.Instance != null) CheckpointManager.Instance.NotifyRoomEntered(currentRoom);

    }

    /// <summary>Every room shares the same gameplay track except the last (The Round), which swaps to round-specific music. Skipped while the main menu is still up - that keeps menu music playing through the automatic EnterRoom(0) call in Start().</summary>
    void UpdateMusic()
    {
        if (AudioManager.Instance == null || MainMenuController.IsOpen) return;
        if (currentRoom == rooms.Length - 1) AudioManager.Instance.PlayRoundMusic();
        else AudioManager.Instance.PlayGameplayMusic();
    }

    void ShowOnlyCurrentPanel()
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].introPanel == null) continue;
            rooms[i].introPanel.SetActive(i == currentRoom);
        }
    }

    /// <summary>Re-applies the current room's title/objective text - used by room-gate scripts (e.g. RangeRoomGate) that mutate a RoomDefinition's objective string after a door unlocks, so the HUD updates without a full room re-entry.</summary>
    public void RefreshObjectiveText()
    {
        UpdateTitle();
    }

    void UpdateTitle()
    {
        var room = rooms[currentRoom];
        if (roomTitleLabel != null)
        {
            roomTitleLabel.text = "ROOM " + (currentRoom + 1) + " / " + rooms.Length + "   " + room.title.ToUpper();
        }
        if (objectiveLabel != null)
        {
            objectiveLabel.text = string.IsNullOrEmpty(room.objective) ? "" : "OBJECTIVE: " + room.objective;
        }
    }

    IEnumerator TransitionTo(int index)
    {
        transitioning = true;

        yield return Fade(0f, 1f);

        HoldEntryGrace();
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
