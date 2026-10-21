using System.Collections;
using UnityEngine;

/// <summary>
/// Lecturer feedback: dying anywhere used to be either a free respawn (rooms 1-7) or a full
/// restart (The Round), with no middle ground on a fairly long tutorial run. This adds one
/// midpoint checkpoint at the start of Room 5 (Smoke) - the split between the "basics" half
/// (gunplay/Sentinels/recon) and the "utility" half (smoke/flash/dash/the round). Captured once
/// per playthrough, gated by the GENERAL settings toggle (PlayerPrefs "CheckpointsEnabled").
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    public const int CheckpointRoomIndex = 4; // Room 5 "Smoke" - see RoomManager.rooms ordering

    [Header("Checkpoint banner")]
    public GameObject bannerPanel;
    public float bannerSeconds = 2.5f;
    public AudioClip checkpointSound;

    [Header("Checkpoint marker (flag) placed in Room 5")]
    public GameObject checkpointMarker;

    bool reached;
    float savedUltCharge;

    public bool CheckpointsEnabled { get { return PlayerPrefs.GetInt("CheckpointsEnabled", 1) == 1; } }
    public bool HasReachedCheckpoint { get { return reached && CheckpointsEnabled; } }

    void Awake()
    {
        Instance = this;
        if (checkpointMarker != null) checkpointMarker.SetActive(false);
    }

    /// <summary>Called by RoomManager every time a room is entered - only acts on Room 5, and only once.</summary>
    public void NotifyRoomEntered(int roomIndex)
    {
        if (roomIndex != CheckpointRoomIndex || reached || !CheckpointsEnabled) return;

        reached = true;
        savedUltCharge = UltimateController.Instance != null ? UltimateController.Instance.CurrentChargeAmount : 0f;

        if (checkpointMarker != null) checkpointMarker.SetActive(true);
        if (bannerPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(BannerRoutine());
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(checkpointSound);
    }

    IEnumerator BannerRoutine()
    {
        bannerPanel.SetActive(true);
        yield return new WaitForSeconds(bannerSeconds);
        bannerPanel.SetActive(false);
    }

    /// <summary>
    /// Drops the player back at the Room 5 entry with the exact state they had on first
    /// arriving there: abilities/cooldowns via the normal EnterRoom flow, full HP/ammo, and the
    /// ultimate charge snapshotted at that moment (kills since then don't count). Rooms 5-8
    /// Sentinels and the Breach Charge/round state reset to fresh, regardless of how far past
    /// the checkpoint the player got before dying.
    /// </summary>
    public void RetryFromCheckpoint()
    {
        if (!HasReachedCheckpoint || RoomManager.Instance == null) return;

        ResetRooms5To8();

        RoomManager.Instance.EnterRoom(CheckpointRoomIndex);

        var player = RoomManager.Instance.player;
        var health = player != null ? player.GetComponent<PlayerHealth>() : null;
        var weapon = player != null ? player.GetComponent<Weapon>() : null;

        if (health != null) health.FullRestore();
        if (weapon != null) weapon.Reload();
        if (UltimateController.Instance != null) UltimateController.Instance.SetCharge(savedUltCharge);
    }

    void ResetRooms5To8()
    {
        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sentinels.Length; i++)
        {
            if (sentinels[i].OwnerRoomIndex >= CheckpointRoomIndex) sentinels[i].ResetState();
        }

        if (RoundController.Instance != null) RoundController.Instance.ResetState();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
