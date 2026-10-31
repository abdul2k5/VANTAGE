using System.Collections;
using UnityEngine;

/// <summary>
/// A real sliding sci-fi door replacing the old invisible DoorTrigger-on-a-gray-cube + beacon
/// line. Two panels slide apart on Unlock(), a light strip above switches red (locked) to amber
/// (open), and a short airlock trigger behind the door (a plain DoorTrigger, same component the
/// old wall-mounted one used) fires the existing fade-and-teleport once the player actually walks
/// through, instead of right at the wall. A full restart reloads the scene back to authored
/// defaults; the only in-place re-lock is Relock(), used by checkpoint retry to re-arm the gated
/// doors in Rooms 5-7.
/// </summary>
public class DoorController : MonoBehaviour
{
    public bool startLocked;
    public Transform leftPanel;
    public Transform rightPanel;
    public float slideDistance = 1.2f;
    public float slideSeconds = 0.6f;
    public Renderer lightStrip;
    /// <summary>Panel colliders - solid (and on the Blocking layer, for both player collision and Sentinel line-of-sight) while locked, disabled once open.</summary>
    public Collider[] blockingColliders;
    public Color lockedColor = new Color(0.75f, 0.1f, 0.08f);
    public Color openColor = new Color(0.9f, 0.6f, 0.15f);
    [ColorUsage(true, true)] public Color lockedEmission = new Color(0.5f, 0.03f, 0.02f);
    [ColorUsage(true, true)] public Color openEmission = new Color(0.35f, 0.18f, 0.04f);
    public AudioClip openSound;
    /// <summary>Floor chevron rows gated to this door - dark while locked, sweep-lit on Unlock(), lit instantly for doors that start open.</summary>
    public FloorChevronGuide[] chevronGuides;


    bool isOpen;
    public bool IsOpen { get { return isOpen; } }

    Vector3 leftClosedLocalPos, rightClosedLocalPos;

    void Awake()
    {
        if (leftPanel != null) leftClosedLocalPos = leftPanel.localPosition;
        if (rightPanel != null) rightClosedLocalPos = rightPanel.localPosition;
    }

    void Start()
    {
        ApplyInstant(!startLocked);
    }

    /// <summary>No-ops if already open - safe to call more than once (e.g. a gate script that could theoretically fire twice).</summary>
    public void Unlock()
    {
        if (isOpen) return;
        StopAllCoroutines();
        StartCoroutine(OpenRoutine());
    }

    /// <summary>Snaps the door back to closed and locked, no animation - for checkpoint retry, so a door opened before dying doesn't stay open on the retry.</summary>
    public void Relock()
    {
        StopAllCoroutines();
        isOpen = false;
        SetBlockingColliders(true);
        ApplyLight(lockedColor, lockedEmission);
        if (leftPanel != null) leftPanel.localPosition = leftClosedLocalPos;
        if (rightPanel != null) rightPanel.localPosition = rightClosedLocalPos;
        if (chevronGuides != null)
        {
            for (int i = 0; i < chevronGuides.Length; i++)
            {
                if (chevronGuides[i] != null) chevronGuides[i].SetUnlitInstant();
            }
        }
    }

IEnumerator OpenRoutine()
    {
        isOpen = true;
        SetBlockingColliders(false);
        ApplyLight(openColor, openEmission);
        TriggerChevronSweep();
        if (openSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySfxAtPoint(openSound, transform.position);

        Vector3 leftTarget = leftClosedLocalPos + Vector3.left * slideDistance;
        Vector3 rightTarget = rightClosedLocalPos + Vector3.right * slideDistance;

        float t = 0f;
        while (t < slideSeconds)
        {
            t += Time.deltaTime;
            float frac = Mathf.Clamp01(t / slideSeconds);
            if (leftPanel != null) leftPanel.localPosition = Vector3.Lerp(leftClosedLocalPos, leftTarget, frac);
            if (rightPanel != null) rightPanel.localPosition = Vector3.Lerp(rightClosedLocalPos, rightTarget, frac);
            yield return null;
        }
        if (leftPanel != null) leftPanel.localPosition = leftTarget;
        if (rightPanel != null) rightPanel.localPosition = rightTarget;
    }

    /// <summary>Sets the door's state with no animation - used once at Start() for doors that are open from the very beginning (every room except Range/Weak Point), so they don't play a pointless slide on room load.</summary>
void ApplyInstant(bool open)
    {
        isOpen = open;
        SetBlockingColliders(!open);
        ApplyLight(open ? openColor : lockedColor, open ? openEmission : lockedEmission);
        if (open) SetChevronsLitInstant();
        if (open)
        {
            if (leftPanel != null) leftPanel.localPosition = leftClosedLocalPos + Vector3.left * slideDistance;
            if (rightPanel != null) rightPanel.localPosition = rightClosedLocalPos + Vector3.right * slideDistance;
        }
    }

    void SetBlockingColliders(bool solid)
    {
        if (blockingColliders == null) return;
        for (int i = 0; i < blockingColliders.Length; i++)
        {
            if (blockingColliders[i] != null) blockingColliders[i].enabled = solid;
        }
    }

    void ApplyLight(Color baseColor, Color emission)
    {
        if (lightStrip == null) return;
        lightStrip.material.SetColor("_BaseColor", baseColor);
        lightStrip.material.SetColor("_EmissionColor", emission);
    }


    void SetChevronsLitInstant()
    {
        if (chevronGuides == null) return;
        for (int i = 0; i < chevronGuides.Length; i++)
        {
            if (chevronGuides[i] != null) chevronGuides[i].SetLitInstant();
        }
    }

    void TriggerChevronSweep()
    {
        if (chevronGuides == null) return;
        for (int i = 0; i < chevronGuides.Length; i++)
        {
            if (chevronGuides[i] != null) chevronGuides[i].TriggerUnlockSweep();
        }
    }

}
