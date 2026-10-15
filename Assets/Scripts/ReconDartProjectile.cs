using System.Collections;
using UnityEngine;

/// <summary>A thrown dart that, on landing, pulses and reveals nearby Sentinels through walls for a few seconds.</summary>
public class ReconDartProjectile : MonoBehaviour
{
    public float revealRadius = 8f;
    public float revealDuration = 3f;
    public float fuseSeconds = 1.5f;
    public Color pulseColor = new Color(0.25f, 0.75f, 0.95f, 0.9f);

    bool landed;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        StartCoroutine(FuseTimeout());
    }

    IEnumerator FuseTimeout()
    {
        yield return new WaitForSeconds(fuseSeconds);
        if (!landed) Land();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!landed) Land();
    }

    void Land()
    {
        landed = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in sentinels)
        {
            if (Vector3.Distance(s.transform.position, transform.position) <= revealRadius)
            {
                s.Reveal(revealDuration);
            }
        }

        StartCoroutine(PulseThenDestroy());
    }

    IEnumerator PulseThenDestroy()
    {
        var pulseGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulseGo.name = "ReconPulse";
        pulseGo.transform.position = transform.position;
        Destroy(pulseGo.GetComponent<SphereCollider>());
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.color = pulseColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", (Vector4)pulseColor * 1.5f);
        mat.renderQueue = 3000;
        pulseGo.GetComponent<MeshRenderer>().sharedMaterial = mat;

        float t = 0f;
        float grow = 0.4f;
        while (t < grow)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / grow);
            float scale = Mathf.Lerp(0.2f, revealRadius * 0.7f, k);
            pulseGo.transform.localScale = new Vector3(scale, scale, scale);
            Color c = pulseColor;
            c.a = Mathf.Lerp(pulseColor.a, 0f, k);
            mat.color = c;
            yield return null;
        }
        Destroy(pulseGo);

        // Hide the dart body itself but let the pulse finish; destroy after the reveal window closes.
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;
        yield return new WaitForSeconds(revealDuration);
        Destroy(gameObject);
    }
}
