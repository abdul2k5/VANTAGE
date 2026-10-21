using UnityEngine;

/// <summary>
/// The physical Breach Charge prop: a canister with a glowing core and a ring of lights,
/// original design (not a reskin of any existing game's spike/bomb). One instance lives in
/// the scene, hidden until BreachChargeSite starts a plant; BreachChargeSite drives the
/// deploy animation via SetDeployProgress (kept in lockstep with the real plant progress bar
/// rather than running its own timer, so cancelling the plant can't leave it out of sync),
/// then RoundController drives the post-plant pulse via SetCountdownProgress.
/// </summary>
public class BreachChargeDevice : MonoBehaviour
{
    public Transform canister;
    public Renderer coreRenderer;
    public Renderer[] ringLightRenderers;
    public Light redLight;
    public Renderer beamRenderer;

    public Color coreIdleColor = new Color(0.25f, 0.85f, 1f, 1f);
    public Color coreRedColor = new Color(1f, 0.15f, 0.1f, 1f);
    public float idlePulseSpeed = 2.5f;
    public float dangerPulseSpeed = 9f;
    public float redZoneSeconds = 5f;

    float countdownPulsePhase;
    bool planted;

    void Awake()
    {
        if (canister != null) canister.gameObject.SetActive(false);
    }

    /// <summary>0 = nothing visible yet, 1 = fully deployed (rises out of the floor, lights power on one by one, core fades in).</summary>
    public void SetDeployProgress(Vector3 position, float frac01)
    {
        if (canister == null) return;

        frac01 = Mathf.Clamp01(frac01);
        transform.position = position;
        canister.gameObject.SetActive(frac01 > 0f);

        float rise = Mathf.SmoothStep(0f, 1f, frac01);
        canister.localScale = new Vector3(1f, rise, 1f);
        canister.localPosition = new Vector3(0f, (rise - 1f) * 0.25f, 0f);

        SetCoreEmission(coreIdleColor, 0.4f + rise * 1.2f);

        if (ringLightRenderers != null)
        {
            int litCount = Mathf.RoundToInt(frac01 * ringLightRenderers.Length);
            for (int i = 0; i < ringLightRenderers.Length; i++)
            {
                SetEmission(ringLightRenderers[i], i < litCount ? coreIdleColor : Color.black, i < litCount ? 1f : 0f);
            }
        }
    }

    public void Hide()
    {
        if (canister != null) canister.gameObject.SetActive(false);
        if (redLight != null) redLight.enabled = false;
        if (beamRenderer != null) beamRenderer.gameObject.SetActive(false);
    }

    public void ConfirmPlanted()
    {
        planted = true;
        if (canister != null)
        {
            canister.gameObject.SetActive(true);
            canister.localScale = Vector3.one;
            canister.localPosition = Vector3.zero;
        }
        if (redLight != null)
        {
            redLight.enabled = true;
            redLight.color = coreIdleColor;
        }
        if (beamRenderer != null) beamRenderer.gameObject.SetActive(true);
        if (ringLightRenderers != null)
        {
            foreach (var r in ringLightRenderers) SetEmission(r, coreIdleColor, 1f);
        }
    }

    /// <summary>Pulses faster as detonation nears, switching to red for the final redZoneSeconds.</summary>
    public void SetCountdownProgress(float remainingSeconds, float totalSeconds)
    {
        if (!planted) return;

        bool danger = remainingSeconds <= redZoneSeconds;
        float speed = danger ? dangerPulseSpeed : idlePulseSpeed;
        countdownPulsePhase += Time.deltaTime * speed;
        float pulse01 = 0.5f + Mathf.Sin(countdownPulsePhase) * 0.5f;

        Color color = danger ? coreRedColor : coreIdleColor;
        float intensity = Mathf.Lerp(0.6f, 2.2f, pulse01);

        SetCoreEmission(color, intensity);
        if (redLight != null)
        {
            redLight.color = color;
            redLight.intensity = Mathf.Lerp(1.5f, 6f, pulse01);
        }
        if (ringLightRenderers != null)
        {
            foreach (var r in ringLightRenderers) SetEmission(r, color, intensity * 0.6f);
        }
        if (beamRenderer != null)
        {
            SetEmission(beamRenderer, color, intensity);
        }
    }

    public void Detonate()
    {
        Hide();
        planted = false;
    }

    void SetCoreEmission(Color color, float intensity)
    {
        SetEmission(coreRenderer, color, intensity);
    }

    void SetEmission(Renderer r, Color color, float intensity)
    {
        if (r == null) return;
        var mat = r.material;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * intensity);
    }


/// <summary>Used by CheckpointManager on a checkpoint retry - same end state as a fresh scene load (hidden, unplanted, pulse phase zeroed).</summary>
    public void ResetState()
    {
        Hide();
        planted = false;
        countdownPulsePhase = 0f;
    }
}
