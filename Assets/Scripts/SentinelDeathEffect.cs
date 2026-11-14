using UnityEngine;

/// <summary>
/// What a Sentinel leaves behind when it dies: a burst of sparks, a short flash of light and a few
/// pieces of its shell thrown outwards. Before this a killed drone simply vanished, so a kill had a
/// sound and a kill-feed line but nothing to look at. Visual only - nothing here has a collider, so
/// the pieces can't block a bullet or a drone's sight.
/// </summary>
public class SentinelDeathEffect : MonoBehaviour
{
    const int SparkCount = 26;
    const int PieceCount = 6;
    const float Lifetime = 1.4f;
    const float FlashSeconds = 0.18f;
    const float FlashIntensity = 6f;
    const float Gravity = 9.8f;
    static readonly Color SparkColor = new Color(1f, 0.62f, 0.22f, 1f);
    static readonly Color FlashColor = new Color(1f, 0.55f, 0.25f, 1f);

    static Material sparkMaterial;

    Light flash;
    Transform[] pieces;
    Vector3[] velocities;
    Vector3[] spins;
    Vector3[] sizes;
    float age;

    /// <summary>Spawns the effect at the drone's body. pieceMaterials are the drone's own, used in turn, so the pieces match it.</summary>
    public static void Spawn(Vector3 position, Material[] pieceMaterials)
    {
        var go = new GameObject("SentinelDeathEffect");
        go.transform.position = position;
        go.AddComponent<SentinelDeathEffect>().Build(pieceMaterials);
        Destroy(go, Lifetime + 0.1f);
    }

    void Build(Material[] pieceMaterials)
    {
        BuildSparks();

        var lightGo = new GameObject("Flash");
        lightGo.transform.SetParent(transform, false);
        flash = lightGo.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = FlashColor;
        flash.range = 6f;
        flash.intensity = FlashIntensity;
        flash.shadows = LightShadows.None;

        pieces = new Transform[PieceCount];
        velocities = new Vector3[PieceCount];
        spins = new Vector3[PieceCount];
        sizes = new Vector3[PieceCount];
        for (int i = 0; i < PieceCount; i++)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = "Piece";
            Destroy(piece.GetComponent<Collider>());
            piece.transform.SetParent(transform, false);
            piece.transform.localPosition = Random.insideUnitSphere * 0.15f;
            piece.transform.localRotation = Random.rotation;
            piece.transform.localScale = new Vector3(Random.Range(0.08f, 0.2f), Random.Range(0.04f, 0.1f), Random.Range(0.08f, 0.2f));
            var r = piece.GetComponent<Renderer>();
            if (pieceMaterials != null && pieceMaterials.Length > 0) r.sharedMaterial = pieceMaterials[i % pieceMaterials.Length];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Vector3 outward = Random.onUnitSphere;
            outward.y = Mathf.Abs(outward.y) * 0.8f + 0.2f;
            pieces[i] = piece.transform;
            sizes[i] = piece.transform.localScale;
            velocities[i] = outward.normalized * Random.Range(2f, 4.5f);
            spins[i] = Random.insideUnitSphere * 540f;
        }
    }

    void BuildSparks()
    {
        var go = new GameObject("Sparks");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = SparkColor;
        main.gravityModifier = 0.8f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)SparkCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetSparkMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
    }

    /// <summary>A material asset in Resources, not one built from Shader.Find, so the shader variant is certain to be in a standalone build.</summary>
    static Material GetSparkMaterial()
    {
        if (sparkMaterial == null) sparkMaterial = Resources.Load<Material>("SentinelDeathSpark");
        return sparkMaterial;
    }

    void Update()
    {
        age += Time.deltaTime;

        if (flash != null) flash.intensity = Mathf.Lerp(FlashIntensity, 0f, age / FlashSeconds);

        // Shrink over the last half second, so the pieces go out instead of popping off.
        float shrink = Mathf.Clamp01((Lifetime - age) / 0.5f);
        for (int i = 0; i < pieces.Length; i++)
        {
            var p = pieces[i];
            velocities[i].y -= Gravity * Time.deltaTime;
            Vector3 pos = p.position + velocities[i] * Time.deltaTime;
            if (pos.y < 0.05f)
            {
                // One soft bounce off the floor, then it skids to a stop.
                pos.y = 0.05f;
                velocities[i] = new Vector3(velocities[i].x * 0.5f, -velocities[i].y * 0.3f, velocities[i].z * 0.5f);
                spins[i] *= 0.5f;
            }
            p.position = pos;
            p.Rotate(spins[i] * Time.deltaTime, Space.Self);
            p.localScale = sizes[i] * shrink;
        }
    }
}
