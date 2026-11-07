using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flash: hold E to ready it (the gun lowers and the flash glows in the player's hand, with a preview line of
/// the throw), release to throw. It bounces off one surface and pops (FlashProjectile), blinding drones within
/// range that can see the pop. Walls block it, so flashing round a corner means banking it off a wall rather
/// than blinding straight through one - the safe window still comes before the peek, but you have to earn it.
/// </summary>
public class FlashAbility : Ability
{
    public float radius = 10f;
    public float blindDuration = 3.5f;
    public Color flashColor = new Color(1f, 0.97f, 0.85f, 0.9f);

    [Header("Throw")]
    public float throwSpeed = 14f;
    [Tooltip("Surfaces it bounces off before the next contact pops it.")]
    public int bounces = 1;
    public float fuseSeconds = 1f;
    [Tooltip("After the bounce it pops this soon even in mid-air, so it goes off just round the corner it was banked off.")]
    public float popAfterBounceSeconds = 0.2f;
    public AudioClip bounceSound;

    [Header("Aiming")]
    [Tooltip("Shared with the Recon Dart's hint - only one can be aimed at a time.")]
    public Text aimHint;
    public float previewWidth = 0.03f;
    public float previewNearSkip = 1.5f;
    [Tooltip("Where the flash sits in the player's hand, relative to the camera.")]
    public Vector3 handPosition = new Vector3(-0.22f, -0.2f, 0.45f);

    Camera cam;
    bool aiming;
    GameObject handFlash;
    LineRenderer preview;
    GameObject popMarker;
    Material glowMaterial;
    readonly List<Vector3> points = new List<Vector3>();
    readonly List<Vector3> bouncePoints = new List<Vector3>();
    readonly List<Vector3> visiblePoints = new List<Vector3>();

    public bool IsAiming { get { return aiming; } }

    void Awake()
    {
        abilityName = "Flash";
        keyLabel = "E";
        cam = Camera.main;
    }

    static LayerMask FlightMask { get { return ~LayerMask.GetMask("Player", "Smoke", "Ignore Raycast", "UI"); } }

    Vector3 LaunchOrigin { get { return cam.transform.position + cam.transform.forward * 0.6f; } }
    Vector3 LaunchVelocity { get { return cam.transform.forward * throwSpeed; } }

    public void BeginAim()
    {
        if (!IsReady || cam == null) return;
        aiming = true;
        EnsureAimObjects();
        handFlash.SetActive(true);
        if (aimHint != null)
        {
            aimHint.gameObject.SetActive(true);
            aimHint.text = "FLASH READY   release [E] to throw";
        }
    }

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
        if (handFlash != null) handFlash.SetActive(false);
        if (preview != null) preview.enabled = false;
        if (popMarker != null) popMarker.SetActive(false);
    }

    protected override void Update()
    {
        base.Update();
        if (!aiming) return;

        if (MainMenuController.IsOpen || AbilityUnlockCardUI.IsShowingCard || Cursor.lockState != CursorLockMode.Locked || !IsReady)
        {
            CancelAim();
            return;
        }

        float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 12f);
        handFlash.transform.localScale = Vector3.one * 0.07f * pulse;
        DrawPreview();
    }

    void DrawPreview()
    {
        Vector3 pop = FlashProjectile.Simulate(LaunchOrigin, LaunchVelocity, bounces, fuseSeconds, popAfterBounceSeconds, FlightMask, points, bouncePoints);

        // Same as the Recon preview: the stretch nearest the eye would draw as a blob over the crosshair.
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

        popMarker.SetActive(true);
        popMarker.transform.position = pop;
    }

    void EnsureAimObjects()
    {
        if (glowMaterial == null)
        {
            glowMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            glowMaterial.SetFloat("_Surface", 1f);
            glowMaterial.SetFloat("_Blend", 0f);
            glowMaterial.renderQueue = 3000;
            glowMaterial.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0.8f);
        }
        if (handFlash == null)
        {
            handFlash = MakeGlowBall("FlashInHand", 0.07f);
            handFlash.transform.SetParent(cam.transform, false);
            handFlash.transform.localPosition = handPosition;
            var light = handFlash.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = flashColor;
            light.range = 1.2f;
            light.intensity = 1.5f;
            handFlash.SetActive(false);
        }
        if (preview == null)
        {
            var go = new GameObject("FlashAimPreview");
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
        }
        if (popMarker == null) popMarker = MakeGlowBall("FlashAimPop", 0.2f);
    }

    GameObject MakeGlowBall(string name, float size)
    {
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = name;
        DestroyImmediate(ball.GetComponent<Collider>()); // or the preview raycasts would hit it
        ball.transform.localScale = Vector3.one * size;
        var r = ball.GetComponent<MeshRenderer>();
        r.sharedMaterial = glowMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ball.SetActive(false);
        return ball;
    }

    protected override bool OnUse()
    {
        if (cam == null) return false;
        EnsureAimObjects();

        var go = MakeGlowBall("FlashProjectile", 0.12f);
        go.SetActive(true);
        go.transform.position = LaunchOrigin;
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.widthMultiplier = 0.06f;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.sharedMaterial = glowMaterial;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var proj = go.AddComponent<FlashProjectile>();
        proj.owner = this;
        proj.hitMask = FlightMask;
        proj.fuseSeconds = fuseSeconds;
        proj.popAfterBounceSeconds = popAfterBounceSeconds;
        proj.bounceSound = bounceSound;
        proj.Launch(LaunchVelocity, bounces);
        return true;
    }

    /// <summary>Called by FlashProjectile when it pops. Blinds drones in range with a clear line to the pop - the same
    /// Blocking layer the drones use for their own sight. Returns how many were blinded.</summary>
    public int Detonate(Vector3 point)
    {
        int blinded = 0;
        int sightMask = LayerMask.GetMask("Blocking");
        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Exclude);
        for (int i = 0; i < sentinels.Length; i++)
        {
            Vector3 target = ReconDartProjectile.SentinelCentre(sentinels[i]);
            if (Vector3.Distance(target, point) > radius) continue;
            if (Physics.Linecast(point, target, sightMask, QueryTriggerInteraction.Ignore)) continue;
            sentinels[i].Blind(blindDuration);
            blinded++;
        }

        StartCoroutine(FlashBurst(point));
        return blinded;
    }

    /// <summary>Checkpoint retry: drops the aim and removes a flash in flight or a burst still fading out.</summary>
    public void ClearActive()
    {
        CancelAim();
        StopAllCoroutines();
        var thrown = Object.FindObjectsByType<FlashProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < thrown.Length; i++) DestroyImmediate(thrown[i].gameObject);
        var burst = GameObject.Find("FlashBurst");
        while (burst != null) { DestroyImmediate(burst); burst = GameObject.Find("FlashBurst"); }
    }

    IEnumerator FlashBurst(Vector3 point)
    {
        var burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(burst.GetComponent<SphereCollider>());
        burst.name = "FlashBurst";
        burst.transform.position = point;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.color = flashColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", (Vector4)flashColor * 2f);
        mat.renderQueue = 3000;
        burst.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var light = burst.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = flashColor;
        light.range = radius * 1.5f;
        light.intensity = 8f;
        // Shadowed, so the light stops at walls the same way the blind does - an unshadowed light leaking round
        // a wall would look like the flash reached drones it didn't.
        light.shadows = LightShadows.Hard;

        float t = 0f;
        float grow = 0.35f;
        while (t < grow)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / grow);
            // Kept small: a big shell would poke through nearby walls towards the player.
            float scale = Mathf.Lerp(0.3f, 1.5f, k);
            burst.transform.localScale = new Vector3(scale, scale, scale);
            Color c = flashColor;
            c.a = Mathf.Lerp(flashColor.a, 0f, k);
            mat.color = c;
            light.intensity = Mathf.Lerp(8f, 0f, k);
            yield return null;
        }

        Destroy(burst);
    }
}
