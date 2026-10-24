using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diegetic checkpoint feedback at the Room 5 entry point, replacing the old flag marker: a floor
/// pad whose segmented emissive ring sweeps on over fillSeconds, and a wall terminal whose screen
/// text/status light switch from STANDBY/red to CHECKPOINT LOGGED/amber. Both stay in their dim
/// "before activation" state until CheckpointManager calls Activate() - which only happens once,
/// the first time the player reaches Room 5 with checkpoints enabled in Settings. If checkpoints
/// are off, Activate() is simply never called and both stay in standby for the whole run.
/// </summary>
public class CheckpointStation : MonoBehaviour
{
    [Header("Pad ring segments, any order")]
    public Renderer[] ringSegments;
    public Color dimRingColor = new Color(0.22f, 0.17f, 0.10f);
    public Color activeRingColor = new Color(0.9f, 0.6f, 0.15f);
    [ColorUsage(true, true)] public Color dimRingEmission = new Color(0.04f, 0.025f, 0.01f);
    [ColorUsage(true, true)] public Color activeRingEmission = new Color(0.4f, 0.2f, 0.04f);
    public float fillSeconds = 0.5f;

    [Header("Terminal screen + status light")]
    public Text screenText;
    public Renderer statusLight;
    public Color standbyLightColor = new Color(0.8f, 0.1f, 0.1f);
    public Color loggedLightColor = new Color(0.9f, 0.6f, 0.15f);
    [ColorUsage(true, true)] public Color standbyLightEmission = new Color(0.35f, 0.03f, 0.03f);
    [ColorUsage(true, true)] public Color loggedLightEmission = new Color(0.4f, 0.2f, 0.04f);
    public string standbyText = "STANDBY";
    public string loggedText = "CHECKPOINT LOGGED ✓";

    bool activated;
    public bool IsActivated { get { return activated; } }

    void Awake()
    {
        ApplyInstant(false);
    }

    /// <summary>Plays the 0.5s ring sweep and flips the terminal. No-ops if already activated, so a checkpoint retry (which re-enters Room 5) never replays it.</summary>
    public void Activate()
    {
        if (activated) return;
        activated = true;
        StopAllCoroutines();
        StartCoroutine(ActivateRoutine());
    }

    IEnumerator ActivateRoutine()
    {
        if (screenText != null) { screenText.text = loggedText; screenText.color = loggedLightColor; }
        if (statusLight != null) ApplyLightColor(loggedLightColor, loggedLightEmission);

        int count = ringSegments != null ? ringSegments.Length : 0;
        float t = 0f;
        while (t < fillSeconds)
        {
            t += Time.deltaTime;
            int litCount = Mathf.CeilToInt(Mathf.Clamp01(t / fillSeconds) * count);
            for (int i = 0; i < count; i++)
            {
                bool lit = i < litCount;
                ApplyRingColor(ringSegments[i], lit ? activeRingColor : dimRingColor, lit ? activeRingEmission : dimRingEmission);
            }
            yield return null;
        }
        for (int i = 0; i < count; i++) ApplyRingColor(ringSegments[i], activeRingColor, activeRingEmission);
    }

    void ApplyInstant(bool isActive)
    {
        if (ringSegments != null)
        {
            for (int i = 0; i < ringSegments.Length; i++)
            {
                ApplyRingColor(ringSegments[i], isActive ? activeRingColor : dimRingColor, isActive ? activeRingEmission : dimRingEmission);
            }
        }
        if (screenText != null) { screenText.text = isActive ? loggedText : standbyText; screenText.color = isActive ? loggedLightColor : standbyLightColor; }
        if (statusLight != null) ApplyLightColor(isActive ? loggedLightColor : standbyLightColor, isActive ? loggedLightEmission : standbyLightEmission);
    }

    void ApplyRingColor(Renderer r, Color baseColor, Color emission)
    {
        if (r == null) return;
        r.material.SetColor("_BaseColor", baseColor);
        r.material.SetColor("_EmissionColor", emission);
    }

    void ApplyLightColor(Color baseColor, Color emission)
    {
        if (statusLight == null) return;
        statusLight.material.SetColor("_BaseColor", baseColor);
        statusLight.material.SetColor("_EmissionColor", emission);
    }
}
