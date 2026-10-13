using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SightlineVisualizer : MonoBehaviour
{
    public float range = 6f;
    public float halfAngleDegrees = 28f;
    public Color fillColor = new Color(0.886f, 0.647f, 0.235f, 0.25f);
    public Color outlineColor = new Color(1f, 0.78f, 0.35f, 0.9f);

    void OnEnable()
    {
        BuildCone();
    }

void BuildCone()
    {
        var mesh = new Mesh();

        Vector3 origin = Vector3.zero;
        float rad = halfAngleDegrees * Mathf.Deg2Rad;
        Vector3 left = new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0f) * range;
        Vector3 right = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f) * range;

        mesh.vertices = new[] { origin, left, right };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.colors = new[] { fillColor, fillColor, fillColor };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        var mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        var line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 3;
        line.SetPosition(0, origin);
        line.SetPosition(1, left);
        line.SetPosition(2, right);
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = outlineColor;
        line.endColor = outlineColor;
        line.sortingOrder = 1;
    }
}
