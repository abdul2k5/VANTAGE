using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Checkpoint pad + terminal at the Room 5 entry point")]
    public CheckpointStation station;

    bool reached;
    float savedUltCharge;

    public bool CheckpointsEnabled { get { return PlayerPrefs.GetInt("CheckpointsEnabled", 1) == 1; } }
    public bool HasReachedCheckpoint { get { return reached && CheckpointsEnabled; } }

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Called by RoomManager every time a room is entered - only acts on Room 5, and only once.</summary>
    public void NotifyRoomEntered(int roomIndex)
    {
        if (roomIndex != CheckpointRoomIndex || reached || !CheckpointsEnabled) return;

        reached = true;
        savedUltCharge = UltimateController.Instance != null ? UltimateController.Instance.CurrentChargeAmount : 0f;

        // Station stays in its dim/standby state for the whole run if this never fires - either
        // because checkpoints are off (gated above) or the player just hasn't reached Room 5 yet.
        if (station != null) station.Activate();
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

        StopAllCoroutines();
        StartCoroutine(RetryFromCheckpointRoutine());
    }

    /// <summary>
    /// Every other room change goes through RoomManager's fade-to-black (TransitionTo) so the
    /// camera teleport is never visible mid-frame. RetryFromCheckpoint used to call EnterRoom
    /// directly with no fade, which could show a jarring instant snap (wrong eye height/angle for
    /// a frame) while the camera and viewmodel were mid-teleport. Matching the same fade here
    /// fixes that and keeps checkpoint retry visually consistent with normal progression.
    /// </summary>
    IEnumerator RetryFromCheckpointRoutine()
    {
        var rm = RoomManager.Instance;

        yield return Fade(rm.fadeOverlay, 0f, 1f, rm.fadeSeconds);

        ResetRooms5To8();
        rm.EnterRoom(CheckpointRoomIndex);

        var player = rm.player;
        var health = player != null ? player.GetComponent<PlayerHealth>() : null;
        var weapon = player != null ? player.GetComponent<Weapon>() : null;

        if (health != null) health.FullRestore();
        if (weapon != null) weapon.Reload();
        if (UltimateController.Instance != null) UltimateController.Instance.SetCharge(savedUltCharge);

        yield return new WaitForSeconds(rm.holdSeconds);
        yield return Fade(rm.fadeOverlay, 1f, 0f, rm.fadeSeconds);
    }

    IEnumerator Fade(Image overlay, float from, float to, float seconds)
    {
        if (overlay == null) yield break;

        float t = 0f;
        Color c = overlay.color;
        while (t < seconds)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            overlay.color = c;
            yield return null;
        }
        c.a = to;
        overlay.color = c;
    }

    void ResetRooms5To8()
    {
        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sentinels.Length; i++)
        {
            if (sentinels[i].OwnerRoomIndex >= CheckpointRoomIndex) sentinels[i].ResetState();
        }

        // Inactive included - a collected orb has disabled itself.
        var orbs = Object.FindObjectsByType<UltOrbPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i].roomIndex >= CheckpointRoomIndex) orbs[i].ResetState();
        }

        var gates = Object.FindObjectsByType<RoomExitGate>(FindObjectsSortMode.None);
        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i].roomIndex >= CheckpointRoomIndex) gates[i].ResetState();
        }

        if (RoundController.Instance != null) RoundController.Instance.ResetState();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
