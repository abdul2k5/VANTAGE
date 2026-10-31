using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Recon Dart: hold F to aim (a live preview line shows the arc, each ricochet and where it sticks), right-click
/// while aiming to cycle 0/1/2 bounces, release F to fire. The arrow sticks and pulses, revealing Sentinels it
/// can see (ReconDartProjectile). Bounces let it reach round a corner the player can't safely peek - "scout
/// before you commit" - and the preview makes those bank shots learnable instead of guesswork.
/// </summary>
public class ReconDartAbility : Ability
{
    public float throwSpeed = 22f;
    public int maxBounces = 2;
    public float revealRadius = 12f;
    public float revealDuration = 2.5f;
    public int pulseCount = 2;
    public float pulseInterval = 1.2f;
    public Color dartColor = new Color(0.25f, 0.75f, 0.95f, 1f);

    [Header("Aiming")]
    [Tooltip("\"BOUNCES: 0  [RMB]  release [F]\" - shown while aiming.")]
    public Text aimHint;
    public float previewWidth = 0.03f;
    [Tooltip("The arc isn't drawn closer than this to the camera - see DrawPreview.")]
    public float previewNearSkip = 1.5f;
    public float markerRadius = 0.3f;

    [Header("Audio")]
    public AudioClip bounceSound;
    public AudioClip stickSound;
    public AudioClip pulseSound;

    Camera cam;
    int bounces;
    bool aiming;
    LineRenderer preview;
    GameObject landMarker;
    readonly List<Vector3> points = new List<Vector3>();
    readonly List<Vector3> bouncePoints = new List<Vector3>();
    Material glowMaterial;
    Material markerMaterial;
    readonly List<Vector3> visiblePoints = new List<Vector3>();
    readonly List<GameObject> bounceDots = new List<GameObject>();

    public bool IsAiming { get { return aiming; } }
    public int Bounces { get { return bounces; } }

    void Awake()
    {
        abilityName = "Recon Dart";
        keyLabel = "F";
        cam = Camera.main;
    }

    void Start()
    {
        if (aimHint != null) aimHint.gameObject.SetActive(false);
    }

    static LayerMask FlightMask { get { return ~LayerMask.GetMask("Player", "Smoke", "Ignore Raycast", "UI"); } }

    Vector3 LaunchOrigin { get { return cam.transform.position + cam.transform.forward * 0.6f; } }
    Vector3 LaunchVelocity { get { return cam.transform.forward * throwSpeed; } }

    public void BeginAim()
    {
        if (!IsReady || cam == null) return;
        aiming = true;
        // Every aim starts as a straight shot; bounces are an opt-in for reaching round corners.
        bounces = 0;
        if (aimHint != null) aimHint.gameObject.SetActive(true);
    }

    public void CycleBounces()
    {
        if (!aiming) return;
        bounces = (bounces + 1) % (maxBounces + 1);
    }

    /// <summary>Release: fire if still aiming. Charges/cooldown come from Ability.TryUse as usual.</summary>
    public void ReleaseAim()
    {
        if (!aiming) return;
        TryUse();
        CancelAim();
    }

    public void CancelAim()
    {
        aiming = false;
        if (aimHint != null) aimHint.gameObject.SetActive(false);
        if (preview != null) preview.enabled = false;
        if (landMarker != null) landMarker.SetActive(false);
        for (int i = 0; i < bounceDots.Count; i++) bounceDots[i].SetActive(false);
    }

    protected override void Update()
    {
        base.Update();
        if (!aiming) return;

        // Anything that takes control away (menu, unlock card, settings, game over) drops the aim.
        if (MainMenuController.IsOpen || AbilityUnlockCardUI.IsShowingCard || Cursor.lockState != CursorLockMode.Locked || !IsReady)
        {
            CancelAim();
            return;
        }

        DrawPreview();
        if (aimHint != null) aimHint.text = "BOUNCES: " + bounces + "   [RMB] change   release [F] to fire";
    }

    void DrawPreview()
    {
        EnsurePreviewObjects();

        RaycastHit stick;
        bool sticks = ReconDartProjectile.Simulate(LaunchOrigin, LaunchVelocity, bounces, FlightMask, points, bouncePoints, out stick);

        // Skip the first stretch of the arc: seen from right behind it, the part nearest the eye draws as a
        // thick blob over the crosshair (playtest: "confused where the dart would land"). The gradient then
        // fades the line in from there, so the far end - where it actually goes - reads strongest.
        Vector3 eye = cam.transform.position;
        visiblePoints.Clear();
        for (int i = 0; i < points.Count; i++)
        {
            if (visiblePoints.Count == 0 && (points[i] - eye).sqrMagnitude < previewNearSkip * previewNearSkip) continue;
            visiblePoints.Add(points[i]);
        }

        preview.enabled = visiblePoints.Count > 1;
        preview.positionCount = visiblePoints.Count;
        for (int i = 0; i < visiblePoints.Count; i++) preview.SetPosition(i, visiblePoints[i]);

        for (int i = 0; i < bounceDots.Count; i++)
        {
            bool show = i < bouncePoints.Count;
            bounceDots[i].SetActive(show);
            if (show) bounceDots[i].transform.position = bouncePoints[i];
        }

        landMarker.SetActive(sticks);
        if (sticks)
        {
            landMarker.transform.position = stick.point + stick.normal * 0.02f;
            landMarker.transform.rotation = Quaternion.FromToRotation(Vector3.up, stick.normal);
            // Gentle pulse so the landing spot catches the eye even when it is small and far away.
            float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 8f);
            landMarker.transform.localScale = Vector3.one * pulse;
        }
    }

    void EnsurePreviewObjects()
    {
        if (glowMaterial == null)
        {
            glowMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            glowMaterial.SetFloat("_Surface", 1f);
            glowMaterial.SetFloat("_Blend", 0f);
            glowMaterial.renderQueue = 3000;
            glowMaterial.color = new Color(dartColor.r, dartColor.g, dartColor.b, 0.75f);
        }
        if (markerMaterial == null)
        {
            markerMaterial = new Material(glowMaterial);
            markerMaterial.color = new Color(0.75f, 0.95f, 1f, 1f);
        }
        if (preview == null)
        {
            var go = new GameObject("ReconAimPreview");
            preview = go.AddComponent<LineRenderer>();
            preview.useWorldSpace = true;
            preview.widthMultiplier = previewWidth;
            preview.widthCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1f), new Keyframe(1f, 1f));
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 1f) });
            preview.colorGradient = g;
            preview.sharedMaterial = glowMaterial;
            preview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            preview.receiveShadows = false;
            preview.numCornerVertices = 2;
        }
        if (landMarker == null)
        {
            // Hollow ring + centre dot flat on the surface: a target, unmistakable from any angle the
            // line itself might be seen at.
            landMarker = new GameObject("ReconAimMarker");
            var ring = new GameObject("Ring").AddComponent<LineRenderer>();
            ring.transform.SetParent(landMarker.transform, false);
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.widthMultiplier = 0.035f;
            ring.sharedMaterial = markerMaterial;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int segments = 32;
            ring.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * markerRadius);
            }

            var dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dot.name = "Dot";
            DestroyImmediate(dot.GetComponent<Collider>()); // or the preview raycasts would hit the marker
            dot.transform.SetParent(landMarker.transform, false);
            dot.transform.localScale = new Vector3(0.09f, 0.002f, 0.09f);
            var r = dot.GetComponent<MeshRenderer>();
            r.sharedMaterial = markerMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        while (bounceDots.Count < maxBounces)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "ReconAimBounce";
            DestroyImmediate(b.GetComponent<Collider>());
            b.transform.localScale = Vector3.one * 0.1f;
            var r = b.GetComponent<MeshRenderer>();
            r.sharedMaterial = markerMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.SetActive(false);
            bounceDots.Add(b);
        }
    }

    protected override bool OnUse()
    {
        if (cam == null) return false;

        var dart = BuildArrow();
        dart.transform.position = LaunchOrigin;

        var proj = dart.AddComponent<ReconDartProjectile>();
        proj.revealRadius = revealRadius;
        proj.revealDuration = revealDuration;
        proj.pulseCount = pulseCount;
        proj.pulseInterval = pulseInterval;
        proj.pulseColor = new Color(dartColor.r, dartColor.g, dartColor.b, 0.9f);
        proj.hitMask = FlightMask;
        proj.sightMask = LayerMask.GetMask("Blocking");
        proj.bounceSound = bounceSound;
        proj.stickSound = stickSound;
        proj.pulseSound = pulseSound;
        proj.Launch(LaunchVelocity, bounces);
        return true;
    }

    /// <summary>Arrow from primitives, pointing down local +Z: dark shaft, glowing head, two fins, a trail and a small light.</summary>
    GameObject BuildArrow()
    {
        var root = new GameObject("ReconDart");

        var shaftMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        shaftMat.color = new Color(0.12f, 0.14f, 0.16f, 1f);
        var glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glowMat.color = dartColor;
        glowMat.EnableKeyword("_EMISSION");
        glowMat.SetColor("_EmissionColor", (Vector4)dartColor * 2f);

        Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.08f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.025f, 0.2f, 0.025f), shaftMat);
        // Pointed head: a cube balanced on its corner (its long diagonal along +Z) inside a parent stretched
        // along Z, which skews it into a four-sided spike.
        var tip = new GameObject("Tip");
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = new Vector3(0f, 0f, 0.13f);
        tip.transform.localScale = new Vector3(0.06f, 0.06f, 0.14f);
        Part(tip, PrimitiveType.Cube, Vector3.zero, Quaternion.Euler(35.26f, 45f, 0f), Vector3.one * 0.58f, glowMat);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, -0.25f), Quaternion.identity, new Vector3(0.09f, 0.004f, 0.06f), glowMat);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, -0.25f), Quaternion.identity, new Vector3(0.004f, 0.09f, 0.06f), glowMat);

        var light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = dartColor;
        light.range = 2f;
        light.intensity = 1.2f;

        var trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.widthMultiplier = 0.04f;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        EnsurePreviewObjects();
        trail.sharedMaterial = glowMaterial;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return root;
    }

    static void Part(GameObject root, PrimitiveType type, Vector3 localPos, Quaternion localRot, Vector3 scale, Material mat)
    {
        var p = GameObject.CreatePrimitive(type);
        // Immediate, not Destroy: a deferred destroy leaves the collider alive for the arrow's first
        // flight step, and the arrow's own raycast would hit its own head.
        DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(root.transform, false);
        p.transform.localPosition = localPos;
        p.transform.localRotation = localRot;
        p.transform.localScale = scale;
        var r = p.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
