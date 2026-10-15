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
            float dist = Vector2.Distance(sentinels[i].transform.position, transform.position);
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
        var burst = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(burst.GetComponent<MeshCollider>());
        burst.name = "FlashBurst";
        burst.transform.position = new Vector3(transform.position.x, transform.position.y, -0.5f);
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = flashColor;
        var mr = burst.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.sortingOrder = 30;

        float t = 0f;
        float grow = 0.35f;
        while (t < grow)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / grow);
            float scale = Mathf.Lerp(0.5f, radius * 2f, k);
            burst.transform.localScale = new Vector3(scale, scale, 1f);
            Color c = flashColor;
            c.a = Mathf.Lerp(flashColor.a, 0f, k);
            mat.color = c;
            yield return null;
        }

        Destroy(burst);
    }
}
