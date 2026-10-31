using System.Collections;
using UnityEngine;

/// <summary>
/// A short row of floor chevrons that stay dark while the room's exit door is locked and light up
/// with a quick first-to-last sweep the moment it unlocks, settling to a soft amber glow - replaces
/// the old dashed "road marking" floor guide strips with a marking that actually carries information.
/// Bars are grouped into chevrons of <see cref="barsPerChevron"/> consecutive entries (normally 2,
/// the two angled strokes of a single "&gt;"), ordered first-to-last in the direction of travel
/// (toward the door) so the sweep visually ripples the way the player should walk.
/// </summary>
public class FloorChevronGuide : MonoBehaviour
{
    public Renderer[] bars;
    public int barsPerChevron = 2;
    /// <summary>For wayfinding chevrons not gated by a door (e.g. pointing around blind cover near a room entry) - lit from Awake, ignores Unlock/TriggerUnlockSweep.</summary>
    public bool alwaysLit;

    public Color unlitBase = new Color(0.05f, 0.05f, 0.05f);
    [ColorUsage(true, true)] public Color unlitEmission = Color.black;
    public Color litBase = new Color(0.3f, 0.22f, 0.1f);
    [ColorUsage(true, true)] public Color litEmission = new Color(0.12f, 0.06f, 0.015f);
    [ColorUsage(true, true)] public Color sweepPeakEmission = new Color(0.26f, 0.13f, 0.03f);
    public float sweepTotalSeconds = 0.4f;
    public float peakHoldSeconds = 0.12f;

    bool lit;

    void Awake()
    {
        ApplyInstant(alwaysLit);
    }

    /// <summary>For doors that start unlocked - shows the chevrons lit immediately, no sweep.</summary>
    public void SetLitInstant()
    {
        StopAllCoroutines();
        ApplyInstant(true);
    }

    /// <summary>Back to dark - used when a door re-locks on checkpoint retry. Always-lit wayfinding rows stay lit.</summary>
    public void SetUnlitInstant()
    {
        if (alwaysLit) return;
        StopAllCoroutines();
        ApplyInstant(false);
    }

    /// <summary>No-op if already lit - safe to call more than once, mirrors DoorController.Unlock().</summary>
    public void TriggerUnlockSweep()
    {
        if (lit) return;
        StopAllCoroutines();
        StartCoroutine(SweepRoutine());
    }

    void ApplyInstant(bool isLit)
    {
        lit = isLit;
        Color baseColor = isLit ? litBase : unlitBase;
        Color emission = isLit ? litEmission : unlitEmission;
        for (int i = 0; i < bars.Length; i++) SetColor(bars[i], baseColor, emission);
    }

    IEnumerator SweepRoutine()
    {
        lit = true;
        int chevronCount = barsPerChevron > 0 ? bars.Length / barsPerChevron : 0;
        if (chevronCount == 0) yield break;
        float perChevron = sweepTotalSeconds / chevronCount;
        for (int c = 0; c < chevronCount; c++)
        {
            StartCoroutine(FlashGroup(c * barsPerChevron, barsPerChevron));
            yield return new WaitForSeconds(perChevron);
        }
    }

    IEnumerator FlashGroup(int startIndex, int count)
    {
        for (int i = 0; i < count; i++) SetColor(bars[startIndex + i], litBase, sweepPeakEmission);
        yield return new WaitForSeconds(peakHoldSeconds);
        for (int i = 0; i < count; i++) SetColor(bars[startIndex + i], litBase, litEmission);
    }

    void SetColor(Renderer r, Color baseColor, Color emission)
    {
        if (r == null) return;
        var mat = r.material;
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_EmissionColor", emission);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }
}
