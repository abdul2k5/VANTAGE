using System.Collections;
using UnityEngine;

public class SmokeWallAbility : Ability
{
    public float distanceAhead = 2.5f;
    public float wallWidth = 3.2f;
    public float wallHeight = 2.2f;
    public float wallThickness = 0.4f;
    public float duration = 6f;
    public Color smokeColor = new Color(0.286f, 0.702f, 0.651f, 0.6f);

    void Awake()
    {
        abilityName = "Smoke Wall";
        keyLabel = "Q";
    }

    protected override bool OnUse()
    {
        Vector3 forward = transform.forward;
        Vector3 spawnPos = transform.position + forward * distanceAhead;
        spawnPos.y = wallHeight * 0.5f;

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "SmokeWall";
        wall.transform.position = spawnPos;
        // The wall spans across the facing direction, so it blocks the lane you're looking down.
        wall.transform.rotation = Quaternion.LookRotation(transform.right, Vector3.up);
        wall.transform.localScale = new Vector3(wallWidth, wallHeight, wallThickness);
        wall.layer = LayerMask.NameToLayer("Blocking");

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f); // transparent
        mat.SetFloat("_Blend", 0f);
        mat.color = smokeColor;
        mat.renderQueue = 3000;
        wall.GetComponent<MeshRenderer>().sharedMaterial = mat;

        StartCoroutine(ExpireAfter(wall, wall.GetComponent<MeshRenderer>()));
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
