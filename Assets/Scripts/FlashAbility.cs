using System.Collections;
using UnityEngine;

public class FlashAbility : Ability
{
    public float radius = 7f;
    public float blindDuration = 3.5f;
    public Color flashColor = new Color(1f, 0.97f, 0.85f, 0.9f);

    void Awake()
    {
        abilityName = "Flash";
        keyLabel = "E";
    }

    protected override bool OnUse()
    {
        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Exclude);
        for (int i = 0; i < sentinels.Length; i++)
        {
            float dist = Vector3.Distance(sentinels[i].transform.position, transform.position);
            if (dist <= radius)
            {
                sentinels[i].Blind(blindDuration);
            }
        }

        StartCoroutine(FlashBurst());
        return true;
    }

    IEnumerator FlashBurst()
    {
        var burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(burst.GetComponent<SphereCollider>());
        burst.name = "FlashBurst";
        burst.transform.position = transform.position + transform.forward * 0.5f;
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

        float t = 0f;
        float grow = 0.35f;
        while (t < grow)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / grow);
            float scale = Mathf.Lerp(0.3f, radius * 0.6f, k);
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
