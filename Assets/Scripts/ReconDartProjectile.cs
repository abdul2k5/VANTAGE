using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Recon Dart in flight: a ballistic arrow that ricochets off surfaces a chosen number of times, then
/// sticks into the next one and fires scan pulses. Each pulse reveals Sentinels within range that the dart
/// has line of sight to - walls block the scan, so where the dart lands is the skill, not just throwing it.
/// Flight uses its own stepping (StepOnce) rather than a Rigidbody, so ReconDartAbility's aim preview can
/// run the exact same maths and the arrow lands where the preview line says it will.
/// </summary>
public class ReconDartProjectile : MonoBehaviour
{
    public const float Gravity = 9f;
    public const float BounceDamping = 0.8f;
    const float SimStep = 0.02f;
    const float MaxFlightSeconds = 4f;

    public float revealRadius = 12f;
    public float revealDuration = 2.5f;
    public int pulseCount = 2;
    public float pulseInterval = 1.2f;
    public Color pulseColor = new Color(0.25f, 0.75f, 0.95f, 0.9f);
    public LayerMask hitMask;
    /// <summary>What blocks the scan - same layer the Sentinels use for their own sight.</summary>
    public LayerMask sightMask;
    public AudioClip bounceSound;
    public AudioClip stickSound;
    public AudioClip pulseSound;

    Vector3 velocity;
    int bouncesLeft;
    bool stuck;
    float flightTime;
    Vector3 stickNormal = Vector3.up;

    public void Launch(Vector3 initialVelocity, int bounces)
    {
        velocity = initialVelocity;
        bouncesLeft = bounces;
        if (velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(velocity);
    }

    /// <summary>
    /// One flight step, shared by the real dart and the aim preview. Returns true when the dart sticks
    /// (stickHit is then the surface). A bounce reflects the velocity off the surface, losing some speed.
    /// </summary>
    public static bool StepOnce(ref Vector3 pos, ref Vector3 vel, ref int bounces, float dt, LayerMask mask, out RaycastHit stickHit, out bool bounced)
    {
        bounced = false;
        stickHit = default(RaycastHit);

        Vector3 nextVel = vel + Vector3.down * Gravity * dt;
        Vector3 step = nextVel * dt;
        float dist = step.magnitude;
        if (dist < 0.0001f) { vel = nextVel; return false; }

        RaycastHit hit;
        if (Physics.Raycast(pos, step / dist, out hit, dist + 0.05f, mask, QueryTriggerInteraction.Ignore))
        {
            if (bounces > 0)
            {
                bounces--;
                vel = Vector3.Reflect(nextVel, hit.normal) * BounceDamping;
                pos = hit.point + hit.normal * 0.05f;
                bounced = true;
                return false;
            }
            stickHit = hit;
            return true;
        }

        vel = nextVel;
        pos += step;
        return false;
    }

    /// <summary>Fills points with the predicted flight path; returns true if it ends stuck in a surface.</summary>
    public static bool Simulate(Vector3 origin, Vector3 vel, int bounces, LayerMask mask, List<Vector3> points, List<Vector3> bouncePoints, out RaycastHit stickHit)
    {
        points.Clear();
        bouncePoints.Clear();
        Vector3 pos = origin;
        points.Add(pos);
        stickHit = default(RaycastHit);
        for (float t = 0f; t < MaxFlightSeconds; t += SimStep)
        {
            bool bounced;
            if (StepOnce(ref pos, ref vel, ref bounces, SimStep, mask, out stickHit, out bounced))
            {
                points.Add(stickHit.point);
                return true;
            }
            points.Add(pos);
            if (bounced) bouncePoints.Add(pos);
        }
        return false;
    }

    void Update()
    {
        if (stuck) return;

        flightTime += Time.deltaTime;
        if (flightTime > MaxFlightSeconds)
        {
            Destroy(gameObject);
            return;
        }

        // Fixed sub-steps so a slow frame can't tunnel the arrow through a thin wall, and so the real
        // flight matches the preview's step size.
        float remaining = Time.deltaTime;
        while (remaining > 0f && !stuck)
        {
            float dt = Mathf.Min(SimStep, remaining);
            remaining -= dt;

            Vector3 pos = transform.position;
            RaycastHit hit;
            bool bounced;
            if (StepOnce(ref pos, ref velocity, ref bouncesLeft, dt, hitMask, out hit, out bounced))
            {
                Stick(hit);
                return;
            }
            transform.position = pos;
            if (bounced) PlayAt(bounceSound);
        }

        if (velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
    }

    void Stick(RaycastHit hit)
    {
        stuck = true;
        stickNormal = hit.normal;
        Vector3 dir = velocity.sqrMagnitude > 0.001f ? velocity.normalized : -hit.normal;
        transform.rotation = Quaternion.LookRotation(dir);
        // Bury the tip a little so it reads as stuck in, not resting on.
        transform.position = hit.point - dir * 0.12f;

        // Stuck in a drone: ride along with it.
        if (hit.collider.GetComponentInParent<Sentinel>() != null) transform.SetParent(hit.collider.transform, true);

        var trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.emitting = false;

        PlayAt(stickSound);
        StartCoroutine(PulseRoutine());
    }

    IEnumerator PulseRoutine()
    {
        for (int i = 0; i < pulseCount; i++)
        {
            if (i > 0) yield return new WaitForSeconds(pulseInterval);
            Pulse();
        }
        yield return new WaitForSeconds(0.6f);
        Destroy(gameObject);
    }

    void Pulse()
    {
        PlayAt(pulseSound);
        Vector3 eye = ScanOrigin;

        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sentinels.Length; i++)
        {
            Vector3 target = SentinelCentre(sentinels[i]);
            if (Vector3.Distance(eye, target) > revealRadius) continue;
            if (Physics.Linecast(eye, target, sightMask, QueryTriggerInteraction.Ignore)) continue;
            sentinels[i].Reveal(revealDuration);
        }

        StartCoroutine(PulseVisual());
    }

    /// <summary>Scan origin, nudged off the surface so the dart's own wall/floor doesn't block it.</summary>
    public Vector3 ScanOrigin { get { return transform.position + stickNormal * 0.25f; } }

    public static Vector3 SentinelCentre(Sentinel s)
    {
        var col = s.GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : s.transform.position + Vector3.up;
    }

    IEnumerator PulseVisual()
    {
        var pulseGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulseGo.name = "ReconPulse";
        pulseGo.transform.position = ScanOrigin;
        Destroy(pulseGo.GetComponent<SphereCollider>());
        float grow = 0.6f;
        // Timed destroy owned by the engine, not this coroutine: the dart destroys itself right after its last
        // pulse, which stops this coroutine and used to leave a 24m shell stuck round the player.
        Destroy(pulseGo, grow + 0.05f);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        // Setting _Surface alone doesn't make URP blend - the blend state and keyword have to be set too, or the
        // shell renders opaque (and with bloom, whites out the screen).
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", 0f); // two-sided, so the shell still shows once it has expanded past the player
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetColor("_BaseColor", pulseColor);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        pulseGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
        pulseGo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        float t = 0f;
        while (t < grow && pulseGo != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / grow);
            float scale = Mathf.Lerp(0.2f, revealRadius * 2f, k);
            pulseGo.transform.localScale = new Vector3(scale, scale, scale);
            Color c = pulseColor;
            c.a = Mathf.Lerp(0.35f, 0f, k);
            mat.SetColor("_BaseColor", c);
            yield return null;
        }
        if (pulseGo != null) Destroy(pulseGo);
    }

    void PlayAt(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySfxAtPoint(clip, transform.position);
    }
}
