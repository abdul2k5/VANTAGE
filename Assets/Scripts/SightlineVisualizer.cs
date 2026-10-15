using UnityEngine;

/// <summary>
/// Floor-projected detection cone (the 3D equivalent of the top-down version's vertical
/// triangle) - a flat decal lying on the ground so it's readable from a first-person view,
/// the same convention used by Hitman/MGS-style stealth cones.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SightlineVisualizer : MonoBehaviour
{
    public float range = 6f;
    public float halfAngleDegrees = 28f;
    public Color fillColor = new Color(0.886f, 0.647f, 0.235f, 0.35f);
    public Color outlineColor = new Color(1f, 0.78f, 0.35f, 0.9f);
    public Color blindedFill = new Color(0.45f, 0.48f, 0.42f, 0.18f);
    public Color blindedOutline = new Color(0.55f, 0.58f, 0.52f, 0.4f);

    void OnEnable()
    {
        BuildCone();
    }

    void BuildCone()
    {
        var mesh = new Mesh();

        // Baked-in lift off the floor (the Sentinel's own pivot sits at ground level for
        // NavMeshAgent conventions, so the decal needs its own offset, not the object's).
        float groundLift = 0.03f;
        Vector3 origin = new Vector3(0f, groundLift, 0f);
        float rad = halfAngleDegrees * Mathf.Deg2Rad;
        // Local +Z is "forward" to match transform.forward, flat on the XZ (floor) plane.
        Vector3 left = new Vector3(-Mathf.Sin(rad), groundLift, Mathf.Cos(rad)) * range;
        Vector3 right = new Vector3(Mathf.Sin(rad), groundLift, Mathf.Cos(rad)) * range;
        left.y = groundLift;
        right.y = groundLift;

        // Wound so the face normal points up (+Y) for an above/angled first-person view.
        mesh.vertices = new[] { origin, right, left };
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
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 3;
        line.SetPosition(0, origin);
        line.SetPosition(1, right);
        line.SetPosition(2, left);
        line.startWidth = 0.06f;
        line.endWidth = 0.06f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = outlineColor;
        line.endColor = outlineColor;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void SetBlinded(bool blinded)
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh != null)
        {
            Color c = blinded ? blindedFill : fillColor;
            mesh.colors = new[] { c, c, c };
        }

        var line = GetComponent<LineRenderer>();
        if (line != null)
        {
            Color lc = blinded ? blindedOutline : outlineColor;
            line.startColor = lc;
            line.endColor = lc;
        }
    }
}
