using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A thrown flash in flight. It ricochets off the first surface it hits, then pops on the next contact, a
/// short moment after the bounce, or when its fuse runs out, whichever comes first. FlashAbility.Detonate
/// then blinds only the drones that can see the pop point. Flight reuses ReconDartProjectile.StepOnce, and
/// Advance is shared with the aim preview, so the flash pops where the preview says it will.
/// </summary>
public class FlashProjectile : MonoBehaviour
{
    const float SimStep = 0.02f;
    /// <summary>Pushed off the surface it popped against, so that surface doesn't block its own line of sight.</summary>
    const float PopStandoff = 0.25f;

    public FlashAbility owner;
    public LayerMask hitMask;
    public float fuseSeconds = 1f;
    public float popAfterBounceSeconds = 0.2f;
    public AudioClip bounceSound;

    Vector3 velocity;
    int bouncesLeft;
    float flightTime;
    float bounceTime = -1f;

    public void Launch(Vector3 initialVelocity, int bounces)
    {
        velocity = initialVelocity;
        bouncesLeft = bounces;
    }

    /// <summary>One flight step. Returns true when the flash pops, with popPoint set.</summary>
    public static bool Advance(ref Vector3 pos, ref Vector3 vel, ref int bounces, ref float time, ref float bouncedAt, float dt,
        float fuse, float popAfterBounce, LayerMask mask, out Vector3 popPoint, out bool bounced)
    {
        popPoint = pos;
        RaycastHit hit;
        time += dt;
        if (ReconDartProjectile.StepOnce(ref pos, ref vel, ref bounces, dt, mask, out hit, out bounced))
        {
            popPoint = hit.point + hit.normal * PopStandoff;
            return true;
        }
        if (bounced) bouncedAt = time;

        bool fuseOut = time >= fuse;
        bool settled = bouncedAt >= 0f && time - bouncedAt >= popAfterBounce;
        if (fuseOut || settled)
        {
            popPoint = pos;
            return true;
        }
        return false;
    }

    /// <summary>Fills points with the predicted path and returns where it pops.</summary>
    public static Vector3 Simulate(Vector3 origin, Vector3 vel, int bounces, float fuse, float popAfterBounce, LayerMask mask, List<Vector3> points, List<Vector3> bouncePoints)
    {
        points.Clear();
        bouncePoints.Clear();
        Vector3 pos = origin;
        float time = 0f, bouncedAt = -1f;
        points.Add(pos);
        for (int i = 0; i < 500; i++)
        {
            Vector3 pop;
            bool bounced;
            if (Advance(ref pos, ref vel, ref bounces, ref time, ref bouncedAt, SimStep, fuse, popAfterBounce, mask, out pop, out bounced))
            {
                points.Add(pop);
                return pop;
            }
            points.Add(pos);
            if (bounced) bouncePoints.Add(pos);
        }
        return pos;
    }

    void Update()
    {
        // Fixed sub-steps, matching the preview, so a slow frame can't tunnel it through a thin wall.
        float remaining = Time.deltaTime;
        while (remaining > 0f)
        {
            float dt = Mathf.Min(SimStep, remaining);
            remaining -= dt;

            Vector3 pos = transform.position;
            Vector3 pop;
            bool bounced;
            bool popped = Advance(ref pos, ref velocity, ref bouncesLeft, ref flightTime, ref bounceTime, dt, fuseSeconds, popAfterBounceSeconds, hitMask, out pop, out bounced);
            if (bounced && bounceSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySfxAtPoint(bounceSound, pos);
            if (popped)
            {
                if (owner != null) owner.Detonate(pop);
                Destroy(gameObject);
                return;
            }
            transform.position = pos;
        }
    }
}
