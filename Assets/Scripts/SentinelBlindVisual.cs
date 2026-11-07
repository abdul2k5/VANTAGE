using UnityEngine;

/// <summary>
/// Shows at a glance that a drone is flashed: its eye burns white-hot, its body sags and wobbles, and
/// sparks circle above it. The sparks flicker just before it recovers, so the player can read how much of
/// the window is left. Built from code the first time it's needed, so scene drones and the reinforcement
/// prefab both get it. Purely visual - no colliders, so it can't block shots.
/// </summary>
public class SentinelBlindVisual : MonoBehaviour
{
    const float SagHeight = 0.05f;
    const float WobbleDegrees = 13f;
    const float WobbleSpeed = 9f;
    const float SparkOrbitRadius = 0.42f;
    const float SparkOrbitSpeed = 300f;
    const float SparkHeightAboveBody = 0.28f;
    const float SparkSize = 0.13f;
    const float EyeGlowSize = 0.26f;
    const float RecoverWarningSeconds = 0.75f;
    static readonly Color GlowColor = new Color(1f, 0.97f, 0.85f, 1f);

    static Material glowMaterial;

    Sentinel sentinel;
    Transform body;
    Transform eye;
    Renderer eyeRenderer;
    Vector3 bodyLocalPosition;
    Quaternion bodyLocalRotation;

    GameObject effectRoot;
    Transform eyeGlow;
    Transform sparkOrbit;
    bool showing;

    void Awake()
    {
        sentinel = GetComponent<Sentinel>();

        var all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "DroneVisual") body = all[i];
            else if (all[i].name == "Eye") eye = all[i];
        }
        if (body != null)
        {
            bodyLocalPosition = body.localPosition;
            bodyLocalRotation = body.localRotation;
        }
        if (eye != null) eyeRenderer = eye.GetComponent<Renderer>();
    }

    void Update()
    {
        bool blinded = sentinel != null && sentinel.IsBlinded;
        if (blinded != showing) SetShowing(blinded);
        if (!showing) return;

        float wobble = Time.time * WobbleSpeed;
        if (body != null)
        {
            body.localPosition = bodyLocalPosition + Vector3.down * SagHeight;
            body.localRotation = bodyLocalRotation * Quaternion.Euler(Mathf.Sin(wobble * 0.7f) * WobbleDegrees, 0f, Mathf.Sin(wobble) * WobbleDegrees);
        }

        sparkOrbit.Rotate(Vector3.up, SparkOrbitSpeed * Time.deltaTime, Space.World);
        eyeGlow.localScale = Vector3.one * EyeGlowSize * (1f + 0.25f * Mathf.Sin(Time.time * 18f));

        bool recovering = sentinel.BlindRemaining <= RecoverWarningSeconds;
        sparkOrbit.gameObject.SetActive(!recovering || Mathf.Repeat(Time.time * 8f, 1f) < 0.5f);
    }

    void SetShowing(bool show)
    {
        showing = show;
        if (show && effectRoot == null) Build();
        if (effectRoot != null) effectRoot.SetActive(show);
        if (eyeRenderer != null) eyeRenderer.enabled = !show;

        if (!show && body != null)
        {
            body.localPosition = bodyLocalPosition;
            body.localRotation = bodyLocalRotation;
        }
    }

    void Build()
    {
        Vector3 eyePosition = eye != null ? eye.position : transform.position + Vector3.up * 1.38f;
        float top = eyePosition.y + 0.3f;
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++) top = Mathf.Max(top, renderers[i].bounds.max.y);

        // Parented to the drone, not the wobbling body, so the glow stays exactly on the headshot hitbox.
        effectRoot = new GameObject("BlindEffect");
        effectRoot.transform.SetParent(transform, false);

        eyeGlow = MakeGlowSphere("EyeGlow", effectRoot.transform, EyeGlowSize);
        eyeGlow.position = eyePosition;

        var lightGo = new GameObject("GlowLight");
        lightGo.transform.SetParent(eyeGlow, false);
        var glowLight = lightGo.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = GlowColor;
        glowLight.range = 2f;
        glowLight.intensity = 0.4f;
        glowLight.shadows = LightShadows.None;

        sparkOrbit = new GameObject("SparkOrbit").transform;
        sparkOrbit.SetParent(effectRoot.transform, false);
        sparkOrbit.position = new Vector3(transform.position.x, top + SparkHeightAboveBody, transform.position.z);

        const int sparkCount = 4;
        for (int i = 0; i < sparkCount; i++)
        {
            var spark = MakeGlowSphere("Spark", sparkOrbit, SparkSize);
            float angle = i * Mathf.PI * 2f / sparkCount;
            spark.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SparkOrbitRadius;
        }
    }

    static Transform MakeGlowSphere(string name, Transform parent, float size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * size;

        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = GetGlowMaterial();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    static Material GetGlowMaterial()
    {
        if (glowMaterial != null) return glowMaterial;

        glowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glowMaterial.color = GlowColor;
        glowMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        glowMaterial.EnableKeyword("_EMISSION");
        glowMaterial.SetColor("_EmissionColor", GlowColor * 4f);
        return glowMaterial;
    }
}
