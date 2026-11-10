using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The retry and win regression tests, run in Play mode in real time (no frame stepping, which has
/// produced false passes before). One scripted run: play through to The Round, plant, die, retry from
/// the checkpoint, plant again, lose to a disarm (or die), retry again, plant a third time, hold the
/// site to a win, then PLAY AGAIN. Every check is on the end state - room, health, panels, drones -
/// and the run fails if frames stop advancing, so a stalled Editor can't pass.
///
/// Enter Play mode, then Vantage > Run Retry And Win Tests. Read the result from the Console or
/// VantageRunTests.Report.
/// </summary>
public static class VantageRunTests
{
    public static string Report = "";
    public static bool Running;

    static IEnumerator routine;
    static double waitUntilTime;
    static Func<bool> waitCondition;
    static double conditionDeadline;
    static bool conditionTimedOut;
    static int passed, failed;
    static StringBuilder log;

    class WaitSeconds { public double seconds; }
    class WaitFor { public Func<bool> condition; public double timeout; }

    [MenuItem("Vantage/Run Retry And Win Tests")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) { Debug.LogError("[VantageRunTests] Enter Play mode first."); return; }
        if (Running) return;
        Running = true;
        passed = failed = 0;
        log = new StringBuilder();
        Report = "RUNNING";
        waitCondition = null;
        waitUntilTime = 0;
        routine = Scenario();
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { Finish("ABORTED: left Play mode"); return; }

        // Cards pause the game; the player would press Space.
        if (AbilityUnlockCardUI.IsShowingCard && AbilityUnlockCardUI.Instance != null) AbilityUnlockCardUI.Instance.Dismiss();

        if (waitCondition != null)
        {
            bool met = false;
            try { met = waitCondition(); } catch (Exception) { }
            if (!met && EditorApplication.timeSinceStartup < conditionDeadline) return;
            conditionTimedOut = !met;
            waitCondition = null;
        }
        if (EditorApplication.timeSinceStartup < waitUntilTime) return;

        bool more;
        try { more = routine.MoveNext(); }
        catch (Exception e) { Check(false, "exception: " + e); Finish(null); return; }
        if (!more) { Finish(null); return; }

        if (routine.Current is WaitSeconds w) waitUntilTime = EditorApplication.timeSinceStartup + w.seconds;
        else if (routine.Current is WaitFor f) { waitCondition = f.condition; conditionDeadline = EditorApplication.timeSinceStartup + f.timeout; }
    }

    static void Finish(string note)
    {
        EditorApplication.update -= Tick;
        Running = false;
        if (note != null) { log.AppendLine(note); failed++; }
        log.AppendLine("RESULT: " + passed + " passed, " + failed + " failed");
        Report = log.ToString();
        if (failed == 0) Debug.Log("[VantageRunTests] " + Report);
        else Debug.LogError("[VantageRunTests] " + Report);
    }

    static void Check(bool ok, string what)
    {
        if (ok) passed++; else failed++;
        log.AppendLine((ok ? "PASS  " : "FAIL  ") + what);
    }

    static object Wait(double seconds) { return new WaitSeconds { seconds = seconds }; }
    static object Until(Func<bool> condition, double timeout) { return new WaitFor { condition = condition, timeout = timeout }; }

    static RoomManager Rooms { get { return RoomManager.Instance; } }
    static PlayerHealth Health { get { return Rooms.player.GetComponent<PlayerHealth>(); } }
    static GameObject GameOverPanel { get { return GameOverController.Instance.panel; } }

    static IEnumerator Scenario()
    {
        Application.runInBackground = true;

        // Always from a fresh run, whatever the Play session was doing before (the same reload RESTART uses).
        GameOverController.Instance.RestartFromBeginning();
        yield return Wait(1.0);
        int startFrame = Time.frameCount;
        yield return Wait(1.5);
        Check(Time.frameCount > startFrame + 10, "frames are advancing (" + (Time.frameCount - startFrame) + " in 1.5s)");
        Check(Rooms.CurrentRoom == 0 && Time.timeScale == 1f, "run starts in Room 1 with time running");

        string roundObjective = Rooms.rooms[Rooms.rooms.Length - 1].objective;

        // --- Attempt 1: reach The Round, plant, die mid-round ---
        foreach (object step in WalkToRoom(7)) yield return step;
        Check(Rooms.CurrentRoom == 7, "walked the doors to Room 8 (in room " + (Rooms.CurrentRoom + 1) + ")");
        Check(CheckpointManager.Instance.HasReachedCheckpoint, "checkpoint captured on the way through Room 5");

        foreach (object step in PlantCharge()) yield return step;
        Check(RoundController.Instance.IsRoundActive, "plant starts the round");
        yield return Wait(3.0);
        Health.TakeDamage(9999f);
        yield return Wait(0.5);
        Check(GameOverPanel.activeSelf && Time.timeScale == 0f, "death opens Game Over and pauses");
        Check(!RoundController.Instance.IsRoundActive, "death stops the round");
        Check(GameOverController.Instance.retryCheckpointButton.gameObject.activeSelf, "Game Over offers RETRY FROM CHECKPOINT");

        foreach (object step in RetryAndVerify("retry 1 (after death)", roundObjective)) yield return step;

        // --- Attempt 2: plant again (the old softlock), lose the round ---
        foreach (object step in WalkToRoom(7)) yield return step;
        foreach (object step in PlantCharge()) yield return step;
        Check(RoundController.Instance.IsRoundActive, "the site can be planted again after a retry");
        // Stand at the charge and do nothing: the round is lost to a disarm or to the drones' guns.
        yield return Until(() => GameOverPanel.activeSelf, 45.0);
        Check(GameOverPanel.activeSelf && !RoundController.Instance.IsRoundActive, "a passive player loses the round" + (conditionTimedOut ? " (TIMED OUT)" : ""));

        foreach (object step in RetryAndVerify("retry 2 (after losing the round)", roundObjective)) yield return step;

        // --- Attempt 3: plant and hold to the win ---
        foreach (object step in WalkToRoom(7)) yield return step;
        foreach (object step in PlantCharge()) yield return step;
        GameObject winPanel = RoundController.Instance.winPanel;
        double deadline = EditorApplication.timeSinceStartup + 60.0;
        int dronesKilled = 0;
        while (!winPanel.activeSelf && !GameOverPanel.activeSelf && EditorApplication.timeSinceStartup < deadline)
        {
            // Holding the site: every reinforcement is shot as soon as it has landed.
            foreach (Sentinel s in UnityEngine.Object.FindObjectsByType<Sentinel>(FindObjectsSortMode.None))
            {
                if (s.enabled && s.name == "Sentinel_Reinforcement") { s.TakeDamage(9999f, true); dronesKilled++; }
            }
            yield return Wait(0.2);
        }
        Check(winPanel.activeSelf, "holding the site wins the round (" + dronesKilled + " reinforcements killed)");
        Check(!GameOverPanel.activeSelf, "Game Over is not open behind the win screen");
        Check(Time.timeScale == 0f, "the win screen pauses the game");
        Check(RunStats.Instance.Finished, "the run clock has stopped");
        Check(RunStats.Instance.CheckpointRetries == 2, "debrief counts 2 checkpoint retries (" + RunStats.Instance.CheckpointRetries + ")");
        Check(LiveDroneCount(7) == 0, "no live drones left in the room after the blast");

        // --- PLAY AGAIN ---
        RoundController.Instance.playAgainButton.onClick.Invoke();
        yield return Wait(2.0);
        Check(RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == 0 && !MainMenuController.IsOpen, "PLAY AGAIN reloads straight into Room 1");
        Check(Time.timeScale == 1f || AbilityUnlockCardUI.IsShowingCard, "time is running after PLAY AGAIN");
        Check(RunStats.Instance != null && !RunStats.Instance.Finished && RunStats.Instance.TotalSeconds < 5f, "run stats are fresh");
        Check(!CheckpointManager.Instance.HasReachedCheckpoint, "checkpoint is cleared by the reload");
    }

    /// <summary>Opens each door in turn, the way a player progresses, until the target room.</summary>
    static IEnumerable WalkToRoom(int target)
    {
        while (Rooms.CurrentRoom < target)
        {
            int before = Rooms.CurrentRoom;
            Rooms.GoToNextRoom();
            yield return Until(() => Rooms.CurrentRoom == before + 1 && !Rooms.IsTransitioning, 6.0);
            if (Rooms.CurrentRoom != before + 1) { Check(false, "door from Room " + (before + 1) + " did not lead on"); yield break; }
            yield return Wait(0.3);
        }
    }

    /// <summary>Clears the two site drones, stands on the site and plants.</summary>
    static IEnumerable PlantCharge()
    {
        foreach (Sentinel s in UnityEngine.Object.FindObjectsByType<Sentinel>(FindObjectsSortMode.None))
        {
            if (s.OwnerRoomIndex == 7 && s.name != "Sentinel_Reinforcement") s.TakeDamage(9999f, true);
        }

        var site = UnityEngine.Object.FindFirstObjectByType<BreachChargeSite>();
        Transform player = Rooms.player;
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.position = new Vector3(site.transform.position.x, player.position.y, site.transform.position.z);
        cc.enabled = true;
        yield return Wait(0.5);

        Check(!site.IsPlanted, "site is unplanted before planting");
        if (site.device != null) site.device.SetDeployProgress(player.position, 1f);
        typeof(BreachChargeSite).GetMethod("Plant", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(site, null);
        yield return Wait(0.3);
    }

    static IEnumerable RetryAndVerify(string label, string roundObjective)
    {
        int frameBefore = Time.frameCount;
        GameOverController.Instance.retryCheckpointButton.onClick.Invoke();
        yield return Wait(2.5);

        Check(Time.frameCount > frameBefore + 10, label + ": frames advanced during the retry");
        Check(Rooms.CurrentRoom == CheckpointManager.CheckpointRoomIndex, label + ": back in Room 5 (in room " + (Rooms.CurrentRoom + 1) + ")");
        Check(!GameOverPanel.activeSelf && Time.timeScale == 1f, label + ": Game Over closed and time running");
        Check(Health.CurrentHealth == Health.MaxHealth && Health.CurrentShield == Health.MaxShield && !Health.AttemptOver, label + ": full health and armour");
        Check(Rooms.FurthestRoom == CheckpointManager.CheckpointRoomIndex, label + ": progress rewound to Room 5");

        var site = UnityEngine.Object.FindFirstObjectByType<BreachChargeSite>(FindObjectsInactive.Include);
        Check(!site.IsPlanted && !RoundController.Instance.IsRoundActive, label + ": site unplanted and round idle");
        Check(GameObject.Find("Sentinel_Reinforcement") == null && GameObject.Find("ReinforcementWarning") == null, label + ": reinforcements and warning lights removed");
        Check(Rooms.rooms[7].objective == roundObjective, label + ": The Round's objective text restored");
        Check(RoundController.Instance.timerPanel == null || !RoundController.Instance.timerPanel.activeSelf, label + ": round timer hidden");

        int inactive = 0;
        foreach (Sentinel s in UnityEngine.Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (s.OwnerRoomIndex >= CheckpointManager.CheckpointRoomIndex && !s.gameObject.activeSelf) inactive++;
        }
        Check(inactive == 0, label + ": every drone in Rooms 5-8 is back (" + inactive + " still down)");

        // The retry bounce: drones that could see the player used to reopen Game Over just after the retry.
        yield return Wait(1.5);
        Check(!GameOverPanel.activeSelf, label + ": Game Over did not reopen");
    }

    static int LiveDroneCount(int room)
    {
        int n = 0;
        foreach (Sentinel s in UnityEngine.Object.FindObjectsByType<Sentinel>(FindObjectsSortMode.None))
        {
            if (s.name == "Sentinel_Reinforcement" || s.OwnerRoomIndex == room) n++;
        }
        return n;
    }
}
