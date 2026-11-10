using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-run debrief, filled from RunStats whenever its panel opens (win screen and Game Over). Shows the
/// numbers in two aligned columns, per-room times on a win, and one coaching tip picked from the weakest
/// stat - so the end screen tells the player what to do differently, not just that the run is over.
/// </summary>
public class RunDebriefUI : MonoBehaviour
{
    public Text statLabels;
    public Text statValues;
    [Tooltip("Optional - per-room times. Left empty on the Game Over panel.")]
    public Text splitLabels;
    public Text splitValues;
    public Text tipText;
    [Tooltip("Game Over: adds the room the run ended in.")]
    public bool showRoomReached;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        var stats = RunStats.Instance;
        if (stats == null) return;

        var labels = new StringBuilder();
        var values = new StringBuilder();
        var rm = RoomManager.Instance;

        if (showRoomReached && rm != null && rm.CurrentRoom >= 0)
        {
            Row(labels, values, "ENDED IN", rm.rooms[rm.CurrentRoom].title.ToUpper() + "  (" + (rm.CurrentRoom + 1) + "/" + rm.rooms.Length + ")");
        }
        Row(labels, values, "TIME", FormatTime(stats.TotalSeconds));
        Row(labels, values, "KILLS", stats.Kills.ToString());
        Row(labels, values, "ACCURACY", Percent(stats.Hits, stats.ShotsFired) + "  (" + stats.Hits + "/" + stats.ShotsFired + ")");
        Row(labels, values, "HEADSHOTS", Percent(stats.HeadshotHits, stats.SentinelHits) + " of drone hits");
        Row(labels, values, "DAMAGE TAKEN", Mathf.RoundToInt(stats.DamageTaken) + (stats.DamageAbsorbed > 0f ? "  (" + Mathf.RoundToInt(stats.DamageAbsorbed) + " by armor)" : ""));
        Row(labels, values, "UTILITY USED", UtilityBreakdown(stats));
        Row(labels, values, "OVERWATCH", stats.UltimateUses == 0 ? "not used" : "used x" + stats.UltimateUses);
        Row(labels, values, "ORBS", stats.OrbsCollected + "/" + TotalOrbs());
        if (stats.CheckpointRetries > 0) Row(labels, values, "CHECKPOINT RETRIES", stats.CheckpointRetries.ToString());
        if (stats.RoomSelects > 0) Row(labels, values, "ROOM SELECT", "used x" + stats.RoomSelects);

        if (statLabels != null) statLabels.text = labels.ToString().TrimEnd();
        if (statValues != null) statValues.text = values.ToString().TrimEnd();

        if (splitLabels != null && splitValues != null && rm != null)
        {
            var sl = new StringBuilder();
            var sv = new StringBuilder();
            for (int i = 0; i < rm.rooms.Length; i++)
            {
                Row(sl, sv, (i + 1) + "  " + rm.rooms[i].title.ToUpper(), FormatTime(stats.RoomSeconds(i)));
            }
            splitLabels.text = sl.ToString().TrimEnd();
            splitValues.text = sv.ToString().TrimEnd();
        }

        if (tipText != null) tipText.text = PickTip(stats);
    }

    static void Row(StringBuilder labels, StringBuilder values, string label, string value)
    {
        labels.AppendLine(label);
        values.AppendLine(value);
    }

    static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return (s / 60) + ":" + (s % 60).ToString("00");
    }

    static string Percent(int part, int whole)
    {
        return whole <= 0 ? "-" : Mathf.RoundToInt(100f * part / whole) + "%";
    }

    /// <summary>"Recon x1, Smoke x2, Flash x1, Dash x2" - abilities by name, not by key, so the line reads without remembering the bindings.</summary>
    string UtilityBreakdown(RunStats stats)
    {
        var ac = RoomManager.Instance != null ? RoomManager.Instance.abilities : null;
        if (ac == null) return stats.TotalAbilityUses.ToString();

        var sb = new StringBuilder();
        Ability[] all = { ac.reconDart, ac.smokeWall, ac.flash, ac.dash };
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            int n = stats.AbilityUses(all[i].abilityName);
            if (n == 0) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(ShortName(all[i].abilityName) + " x" + n);
        }
        return sb.Length > 0 ? sb.ToString() : "none";
    }

    /// <summary>First word only ("Recon Dart" -> "Recon"), so all four fit on the one line the column has.</summary>
    static string ShortName(string abilityName)
    {
        int space = abilityName.IndexOf(' ');
        return space > 0 ? abilityName.Substring(0, space) : abilityName;
    }

    static int TotalOrbs()
    {
        return Object.FindObjectsByType<UltOrbPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    /// <summary>The single most useful thing to work on, checked in order of how much it would help.</summary>
    string PickTip(RunStats stats)
    {
        int room = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : 0;

        if (stats.ShotsFired >= 15 && stats.Hits < stats.ShotsFired * 0.35f)
            return "TIP: Under 35% of your shots landed. Stop moving before you shoot, and tap or burst - holding fire makes the gun climb.";
        if (stats.SentinelHits >= 8 && stats.HeadshotHits < stats.SentinelHits * 0.3f)
            return "TIP: Most hits were body shots. The red eye takes 2.5x damage - aim at head height before you peek.";
        if (room >= 4 && stats.TotalAbilityUses < room - 2)
            return "TIP: You saved most of your utility. Every room refills it - smoke, flash or dart before you peek.";
        if (stats.UltimateUses == 0 && UltimateController.Instance != null && UltimateController.Instance.IsReady)
            return "TIP: You ended with Overwatch Protocol charged and unused. Press [X] to reveal every drone and take less damage.";
        if (room >= 6 && stats.OrbsCollected < TotalOrbs())
            return "TIP: You left Overwatch orbs behind - each one is worth a kill's worth of ultimate charge.";
        if (stats.DamageTaken >= 200f)
            return "TIP: You took a lot of damage. Crouch behind cover to break line of sight, and walk [Alt] so drones don't hear you coming.";
        return "Clean run. Now do it faster.";
    }
}
