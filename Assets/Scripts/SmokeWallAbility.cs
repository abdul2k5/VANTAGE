using System.Collections;
using UnityEngine;

public class SmokeWallAbility : Ability
{
    public float distanceAhead = 2.2f;
    public float wallWidth = 3.2f;
    public float wallThickness = 0.5f;
    public float duration = 6f;
    public Color smokeColor = new Color(0.286f, 0.702f, 0.651f, 0.75f);

    void Awake()
    {
        abilityName = "Smoke Wall";
        keyLabel = "Q";
    }

    protected override bool OnUse()
    {
        Vector3 forward = transform.right;
        Vector3 spawnPos = transform.position + forward * distanceAhead;
        spawnPos.z = 0f;

        var wall = new GameObject("SmokeWall");
        wall.transform.position = spawnPos;
        // The wall spans across the facing direction, so it blocks the lane you're looking down.
        wall.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg + 90f);
        wall.layer = LayerMask.NameToLayer("Blocking");

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(wall.transform, false);
        quad.transform.localScale = new Vector3(wallWidth, wallThickness, 1f);
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = smokeColor;
        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
        quad.GetComponent<MeshRenderer>().sortingOrder = 8;

        var col = wall.AddComponent<BoxCollider2D>();
        col.offset = Vector2.zero;
        col.size = new Vector2(wallWidth, wallThickness);

        StartCoroutine(ExpireAfter(wall, quad.GetComponent<MeshRenderer>()));
        return true;
    }

    IEnumerator ExpireAfter(GameObject wall, MeshRenderer renderer)
    {
        yield return new WaitForSeconds(duration * 0.75f);

        // Fade the last quarter so its expiry is readable, not a sudden vanish.
        float fadeTime = duration * 0.25f;
        float t = 0f;
        var mat = renderer.material;
        Color c = mat.color;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(smokeColor.a, 0f, Mathf.Clamp01(t / fadeTime));
            mat.color = c;
            yield return null;
        }

        if (wall != null) Destroy(wall);
    }
}
