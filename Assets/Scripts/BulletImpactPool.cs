using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and pools bullet-hole decals (quads oriented to the hit surface, offset to avoid
/// z-fighting) plus a small dust/spark puff and impact sound, on every shot - player or
/// Sentinel - that hits world geometry. Sentinel hits get a spark only, never a decal (they
/// already have the 2D hit marker + hitSound/headshotSound). Range-target hits are exempt from
/// the normal pool/fade lifecycle so the player can read their shot grouping for as long as
/// they're in Room 1.
/// </summary>
public class BulletImpactPool : MonoBehaviour
{
    public static BulletImpactPool Instance;

    [Header("Decal pool")]
    public int maxDecals = 60;
    public float decalFadeAfterSeconds = 15f;
    public float decalFadeDuration = 1f;
    public float decalMinSize = 0.08f;
    public float decalMaxSize = 0.16f;
    public float decalSurfaceOffset = 0.015f;

    [Header("Audio")]
    public AudioClip impactSound;
    public float impactVolume = 0.5f;

    [Header("Spark (Sentinel hits - no decal)")]
    public Color sparkColor = new Color(1f, 0.8f, 0.3f, 1f);
    public Color dustColor = new Color(0.4f, 0.37f, 0.33f, 1f);

    readonly Queue<GameObject> pooledDecals = new Queue<GameObject>();
    static Texture2D decalTexture;
    static Material decalMaterialTemplate;

    void Awake()
    {
        Instance = this;
        if (decalTexture == null) decalTexture = BuildDecalTexture();
    }

    static Texture2D BuildDecalTexture()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist01 = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float jitter = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.25f;
                float alpha = Mathf.Clamp01(1f - dist01 + jitter - 0.15f);
                alpha = Mathf.Pow(alpha, 1.6f);
                Color c = Color.Lerp(new Color(0.04f, 0.035f, 0.03f, 1f), new Color(0.16f, 0.13f, 0.1f, 1f), dist01);
                c.a = alpha;
                pixels[y * size + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>Normal wall/floor/cover/range-target bullet hole. isRangeTarget decals are never pooled/faded.</summary>
public void SpawnDecal(Vector3 point, Vector3 normal, Transform hitTransform, bool isRangeTarget)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = isRangeTarget ? "RangeBulletHole" : "BulletHole";
        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Parent first (local identity), then set world position/rotation via the world setters -
        // those are always correct under a non-uniformly scaled parent (position via inverse point,
        // rotation via quaternion inverse - neither depends on parent scale). Scale is the one thing
        // that genuinely interacts with a non-uniform parent scale once the quad is rotated to face
        // the surface, so it's compensated explicitly below instead of trusting
        // SetParent(worldPositionStays:true), which shears a rotated child under a non-uniform parent.
        quad.transform.SetParent(hitTransform, false);
        quad.transform.position = point + normal * decalSurfaceOffset;
        quad.transform.rotation = Quaternion.LookRotation(-normal, Vector3.up) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        float worldSize = Random.Range(decalMinSize, decalMaxSize);
        Vector3 parentScale = hitTransform.lossyScale;
        Vector3 worldRight = quad.transform.rotation * Vector3.right;
        Vector3 worldUp = quad.transform.rotation * Vector3.up;
        float rightFactor = Mathf.Abs(worldRight.x) * parentScale.x + Mathf.Abs(worldRight.y) * parentScale.y + Mathf.Abs(worldRight.z) * parentScale.z;
        float upFactor = Mathf.Abs(worldUp.x) * parentScale.x + Mathf.Abs(worldUp.y) * parentScale.y + Mathf.Abs(worldUp.z) * parentScale.z;
        quad.transform.localScale = new Vector3(
            rightFactor > 0.0001f ? worldSize / rightFactor : worldSize,
            upFactor > 0.0001f ? worldSize / upFactor : worldSize,
            1f);

        if (decalMaterialTemplate == null)
        {
            decalMaterialTemplate = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            decalMaterialTemplate.SetFloat("_Surface", 1f);
            decalMaterialTemplate.SetFloat("_Blend", 0f);
            decalMaterialTemplate.renderQueue = 3000;
            decalMaterialTemplate.mainTexture = decalTexture;
        }
        var mat = new Material(decalMaterialTemplate);
        quad.GetComponent<MeshRenderer>().material = mat;

        SpawnPuff(point, normal, dustColor);
        PlayImpactSound(point);

        if (isRangeTarget) return; // persists for the whole Room 1 visit, never pooled/faded

        pooledDecals.Enqueue(quad);
        if (pooledDecals.Count > maxDecals)
        {
            var oldest = pooledDecals.Dequeue();
            if (oldest != null) Destroy(oldest);
        }
        StartCoroutine(FadeThenRecycle(quad, mat));
    }

    /// <summary>Sentinel hits - a spark only, no decal (the sentinel already has a 2D hit marker + sound).</summary>
    public void SpawnSentinelSpark(Vector3 point, Vector3 normal)
    {
        SpawnPuff(point, normal, sparkColor);
    }

    void SpawnPuff(Vector3 point, Vector3 normal, Color color)
    {
        GameObject puffGo = new GameObject("ImpactPuff");
        puffGo.transform.position = point;
        var ps = puffGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.35f;
        main.startSpeed = 1.5f;
        main.startSize = 0.06f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 6) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.rotation = Quaternion.LookRotation(normal).eulerAngles;

        var renderer = puffGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        ps.Play();
        Destroy(puffGo, 1f);
    }

    void PlayImpactSound(Vector3 point)
    {
        if (impactSound == null || AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySfxAtPoint(impactSound, point, impactVolume);
    }

    IEnumerator FadeThenRecycle(GameObject quad, Material mat)
    {
        yield return new WaitForSeconds(decalFadeAfterSeconds);
        if (quad == null) yield break;

        float t = 0f;
        Color start = mat.color;
        while (t < decalFadeDuration)
        {
            if (quad == null) yield break;
            t += Time.deltaTime;
            Color c = start;
            c.a = Mathf.Lerp(start.a, 0f, t / decalFadeDuration);
            mat.color = c;
            yield return null;
        }

        if (quad != null) Destroy(quad);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
