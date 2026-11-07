using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tallies one playthrough for the end-of-run debrief (RunDebriefUI): time per room, shooting, damage,
/// utility and ultimate use. Lives in the scene, so a restart (scene reload) starts a fresh run, while a
/// checkpoint retry keeps counting - the debrief is for the whole run, failed attempts included.
/// </summary>
public class RunStats : MonoBehaviour
{
    public static RunStats Instance;

    public float TotalSeconds { get; private set; }
    public int ShotsFired { get; private set; }
    public int Hits { get; private set; }
    public int SentinelHits { get; private set; }
    public int HeadshotHits { get; private set; }
    public int Kills { get; private set; }
    public float DamageTaken { get; private set; }
    /// <summary>Part of DamageTaken that armor soaked up instead of health.</summary>
    public float DamageAbsorbed { get; private set; }
    public int UltimateUses { get; private set; }
    public int CheckpointRetries { get; private set; }
    /// <summary>Jumps through Room Select or Restart Room - shown on the debrief so a practised run's time reads as one.</summary>
    public int RoomSelects { get; private set; }
    public int OrbsCollected { get { return orbsCollected.Count; } }
    public bool Finished { get; private set; }

    float[] roomSeconds;
    readonly Dictionary<string, int> abilityUses = new Dictionary<string, int>();
    readonly HashSet<string> orbsCollected = new HashSet<string>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Scaled time, so the main menu, unlock cards and settings (all timeScale 0) don't count.
        if (Finished || MainMenuController.IsOpen || RoomManager.Instance == null) return;

        TotalSeconds += Time.deltaTime;
        int room = RoomManager.Instance.CurrentRoom;
        if (room < 0) return;
        if (roomSeconds == null) roomSeconds = new float[RoomManager.Instance.rooms.Length];
        if (room < roomSeconds.Length) roomSeconds[room] += Time.deltaTime;
    }

    public float RoomSeconds(int room)
    {
        return roomSeconds != null && room >= 0 && room < roomSeconds.Length ? roomSeconds[room] : 0f;
    }

    public int AbilityUses(string abilityName)
    {
        int n;
        return abilityUses.TryGetValue(abilityName, out n) ? n : 0;
    }

    public int TotalAbilityUses
    {
        get
        {
            int total = 0;
            foreach (var kv in abilityUses) total += kv.Value;
            return total;
        }
    }

    public void RecordShot() { ShotsFired++; }

    public void RecordHit(bool sentinel, bool headshot)
    {
        Hits++;
        if (sentinel) SentinelHits++;
        if (headshot) HeadshotHits++;
    }

    public void RecordKill() { Kills++; }
    public void RecordDamage(float amount, float absorbedByShield)
    {
        if (Finished) return;
        DamageTaken += amount;
        DamageAbsorbed += absorbedByShield;
    }
    public void RecordUltimate() { UltimateUses++; }
    public void RecordCheckpointRetry() { CheckpointRetries++; }
    public void RecordRoomSelect() { RoomSelects++; }

    public void RecordAbility(string abilityName)
    {
        int n;
        abilityUses.TryGetValue(abilityName, out n);
        abilityUses[abilityName] = n + 1;
    }

    /// <summary>Keyed by orb name - checkpoint retry respawns orbs, and re-collecting one shouldn't count twice.</summary>
    public void RecordOrb(string orbName) { orbsCollected.Add(orbName); }

    /// <summary>Stops the clock - called on a win. A death doesn't finish the run, since checkpoint retry continues it.</summary>
    public void Finish() { Finished = true; }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
