using UnityEngine;

/// <summary>Throws a dart that reveals Sentinels through walls where it lands - "scout before you commit."</summary>
public class ReconDartAbility : Ability
{
    public float throwSpeed = 16f;
    public float revealRadius = 8f;
    public float revealDuration = 3f;
    public float fuseSeconds = 1.5f;
    public Color dartColor = new Color(0.25f, 0.75f, 0.95f, 1f);

    Camera cam;

    void Awake()
    {
        abilityName = "Recon Dart";
        keyLabel = "F";
        cam = Camera.main;
    }

    protected override bool OnUse()
    {
        Vector3 origin = cam != null ? cam.transform.position + cam.transform.forward * 0.6f : transform.position;
        Vector3 dir = cam != null ? cam.transform.forward : transform.forward;

        var dart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dart.name = "ReconDart";
        dart.transform.position = origin;
        dart.transform.localScale = Vector3.one * 0.18f;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = dartColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", (Vector4)dartColor);
        dart.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var col = dart.GetComponent<SphereCollider>();
        col.isTrigger = false;

        var rb = dart.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.linearVelocity = dir * throwSpeed;

        var proj = dart.AddComponent<ReconDartProjectile>();
        proj.revealRadius = revealRadius;
        proj.revealDuration = revealDuration;
        proj.fuseSeconds = fuseSeconds;
        proj.pulseColor = dartColor;

        return true;
    }
}
