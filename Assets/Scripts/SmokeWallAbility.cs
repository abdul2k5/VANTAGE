using System.Collections;
using UnityEngine;

/// <summary>
/// Q - drops a wall of smoke a few metres ahead, across the lane you're facing. The wall itself is
/// an invisible box on the Smoke layer - Sentinel sight (CanSeePlayer) treats it as solid, but
/// bullets in both directions pass through it. What you see is a cluster of soft drifting puffs
/// over that box, thick enough that you can't see through it either: smoke is a trade, not a
/// one-way window.
/// </summary>
public class SmokeWallAbility : Ability
{
    public float distanceAhead = 2.5f;
    public float wallWidth = 3.2f;
    public float wallHeight = 2.2f;
    public float wallThickness = 0.4f;
    public float duration = 6f;
    /// <summary>Puff tint; alpha is per-puff opacity - overlapping puffs stack to near-solid in the middle.</summary>
    public Color smokeColor = new Color(0.286f, 0.702f, 0.651f, 0.6f);

    [Header("Smoke visuals")]
    /// <summary>Mat_Smoke - URP Particles/Unlit, transparent, soft puff texture. Referenced here (not Shader.Find'd) so the shader is guaranteed to be in builds.</summary>
    public Material smokeMaterial;
    public int puffCount = 45;
    public float puffSizeMin = 1.3f;
    public float puffSizeMax = 2.1f;
    /// <summary>How deep (along your view) the puffs spawn. Deeper than the 0.4m blocker on purpose: a
    /// paper-thin layer of puffs looks see-through up close, more layers stacked in depth reads as dense.</summary>
    public float puffDepth = 1.0f;
    public float bloomInSeconds = 0.35f;

    void Awake()
    {
        abilityName = "Smoke Wall";
        keyLabel = "Q";
    }

    protected override bool OnUse()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 spawnPos = transform.position + forward * distanceAhead;
        spawnPos.y = wallHeight * 0.5f;

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "SmokeWall";
        wall.transform.position = spawnPos;
        // The wall spans across the facing direction, so it blocks the lane you're looking down:
        // the cube's local Z (thickness) points along your view, local X (width) runs left-right.
        // It used to look along transform.right instead, which turned the 3.2m width to run
        // away from the player - a long thin slab down the lane that barely blocked anything.
        wall.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        wall.transform.localScale = new Vector3(wallWidth, wallHeight, wallThickness);
        wall.layer = LayerMask.NameToLayer("Smoke");

        // The box is gameplay only - the smoke you see is the particle cluster below.
        wall.GetComponent<MeshRenderer>().enabled = false;
        var puffs = CreateSmokeVisual(wall.transform);

        StartCoroutine(ExpireAfter(wall, puffs));
        return true;
    }

    GameObject CreateSmokeVisual(Transform wall)
    {
        // Not parented to the wall: the wall is non-uniformly scaled, which would distort the puffs.
        var go = new GameObject("SmokeWall_Puffs");
        go.transform.SetPositionAndRotation(wall.position, wall.rotation);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(duration * 0.95f, duration);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(puffSizeMin, puffSizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = smokeColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = puffCount;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)puffCount) });

        // Puffs fill the same box the collider occupies (slightly inset so edges stay soft rather
        // than poking out past the real blocker).
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(wallWidth * 0.85f, wallHeight * 0.7f, puffDepth);

        // Slow churn so it reads as smoke, not a static billboard stack.
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.0f, 0.06f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

        // Bloom in quickly, hold, then thin out over the last quarter - same expiry timing the old
        // solid box used, so the "smoke is about to drop" cue is unchanged.
        float bloomFrac = Mathf.Clamp01(bloomInSeconds / duration);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, bloomFrac), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = gradient;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(bloomFrac, 1f), new Keyframe(1f, 1.15f)));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = smokeMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        ps.Play();
        return go;
    }

    IEnumerator ExpireAfter(GameObject wall, GameObject puffs)
    {
        // The visual fades itself out over the last quarter (colorOverLifetime); the blocker stays
        // solid until the smoke is fully gone so what you see and what blocks always agree.
        yield return new WaitForSeconds(duration);
        if (wall != null) Destroy(wall);
        if (puffs != null) Destroy(puffs);
    }
}
