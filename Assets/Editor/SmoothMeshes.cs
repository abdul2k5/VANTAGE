using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 19.19: the smooth shapes Unity has no built-in for - a box with rounded edges, a properly round
/// cylinder (Unity's has 20 flat sides), and anything turned on a lathe. Every mesh made here is kept in
/// one asset file under its own name, so the scene and the prefabs can point at it and a re-run refills
/// the same mesh instead of piling up copies.
/// </summary>
public static class SmoothMeshes
{
    const string AssetPath = "Assets/Art/Models/SmoothParts.asset";
    const int Segments = 48;
    static Dictionary<string, Mesh> cache;

    static Mesh Slot(string key)
    {
        if (cache != null)
        {
            // A deleted asset leaves dead entries behind.
            foreach (var m in cache.Values) if (m == null) { cache = null; break; }
        }
        if (cache == null)
        {
            cache = new Dictionary<string, Mesh>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetPath))
            {
                var m = o as Mesh;
                if (m != null) cache[m.name] = m;
            }
            if (cache.Count == 0)
            {
                // The file needs one main object for the rest to hang off.
                var holder = new Mesh { name = "SmoothParts" };
                AssetDatabase.CreateAsset(holder, AssetPath);
                cache[holder.name] = holder;
            }
        }
        Mesh mesh;
        if (!cache.TryGetValue(key, out mesh))
        {
            mesh = new Mesh { name = key };
            AssetDatabase.AddObjectToAsset(mesh, AssetPath);
            cache[key] = mesh;
        }
        return mesh;
    }

    static Mesh Fill(string key, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris)
    {
        var mesh = Slot(key);
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static string Tenths(float metres) { return Mathf.RoundToInt(metres * 10000f).ToString(); }

    /// <summary>
    /// A box with every edge and corner rounded to the given radius. The mesh is 1 x 1 x 1, like Unity's
    /// cube, and is meant for an object scaled to <paramref name="size"/>: it is squashed in advance so the
    /// rounding comes out even on all sides once that scale is applied. A collider on the object is untouched.
    /// </summary>
    public static Mesh RoundedBox(Vector3 size, float radius)
    {
        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        radius = Mathf.Min(radius, 0.49f * Mathf.Min(size.x, Mathf.Min(size.y, size.z)));
        string key = "Box_" + Tenths(size.x) + "_" + Tenths(size.y) + "_" + Tenths(size.z) + "_r" + Tenths(radius);

        const int steps = 3;
        Vector3 half = size * 0.5f;
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int axis = 0; axis < 3; axis++)
        {
            int u = (axis + 1) % 3, v = (axis + 2) % 3;
            float[] cu = EdgeCoords(half[u], radius, steps), cv = EdgeCoords(half[v], radius, steps);
            int n = cu.Length;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                int start = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        // A point on the square-cornered box, pulled in to the box that is one radius smaller, then pushed back out by the radius.
                        var p = Vector3.zero;
                        p[axis] = sign * half[axis]; p[u] = cu[i]; p[v] = cv[j];
                        var inner = new Vector3(
                            Mathf.Clamp(p.x, -(half.x - radius), half.x - radius),
                            Mathf.Clamp(p.y, -(half.y - radius), half.y - radius),
                            Mathf.Clamp(p.z, -(half.z - radius), half.z - radius));
                        var normal = (p - inner).normalized;
                        var pos = inner + normal * radius;
                        verts.Add(new Vector3(pos.x / size.x, pos.y / size.y, pos.z / size.z));
                        normals.Add(Vector3.Scale(normal, size).normalized);
                        uvs.Add(new Vector2(cu[i] / size[u] + 0.5f, cv[j] / size[v] + 0.5f));
                    }
                }
                for (int i = 0; i < n - 1; i++)
                {
                    for (int j = 0; j < n - 1; j++)
                    {
                        int a = start + i * n + j, b = a + n, c = a + 1, d = b + 1;
                        if (sign > 0) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(d); tris.Add(c); }
                        else { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); }
                    }
                }
            }
        }
        return Fill(key, verts, normals, uvs, tris);
    }

    /// <summary>Where the grid lines of one face fall along one side: packed into the two rounded ends, at even angles round the corner.</summary>
    static float[] EdgeCoords(float half, float radius, int steps)
    {
        var coords = new float[steps * 2 + 2];
        for (int s = 0; s <= steps; s++)
        {
            float reach = radius * Mathf.Tan(s * 45f / steps * Mathf.Deg2Rad);
            coords[steps - s] = -(half - radius) - reach;
            coords[steps + 1 + s] = (half - radius) + reach;
        }
        return coords;
    }

    /// <summary>
    /// A stand-in for Unity's cylinder with the same size (1 across, 2 tall, along Y) but 48 sides and
    /// smooth shading. <paramref name="softEdges"/> takes the sharp rim off each end.
    /// </summary>
    public static Mesh Cylinder(bool softEdges)
    {
        if (!softEdges)
            return Lathe("Cylinder_Plain", new[] { new Vector2(0f, -1f), new Vector2(0.5f, -1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f) });
        return Lathe("Cylinder_Soft", new[]
        {
            new Vector2(0f, -1f), new Vector2(0.40f, -1f), new Vector2(0.47f, -1f), new Vector2(0.49f, -0.992f), new Vector2(0.5f, -0.97f),
            new Vector2(0.5f, -0.9f), new Vector2(0.5f, 0.9f),
            new Vector2(0.5f, 0.97f), new Vector2(0.49f, 0.992f), new Vector2(0.47f, 1f), new Vector2(0.40f, 1f), new Vector2(0f, 1f),
        }, 50f);
    }

    /// <summary>
    /// A shape turned round the Y axis. <paramref name="profile"/> is its outline from bottom to top, each
    /// point a (distance from the axis, height). Corners gentler than <paramref name="creaseDegrees"/> are
    /// shaded as a curve, sharper ones stay as an edge.
    /// </summary>
    public static Mesh Lathe(string key, Vector2[] profile, float creaseDegrees = 35f)
    {
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        int edges = profile.Length - 1;
        var edgeNormals = new Vector2[edges];
        for (int i = 0; i < edges; i++)
        {
            var along = profile[i + 1] - profile[i];
            edgeNormals[i] = new Vector2(along.y, -along.x).normalized;
        }

        for (int i = 0; i < edges; i++)
        {
            Vector2 low = edgeNormals[i], high = edgeNormals[i];
            if (i > 0 && Vector2.Angle(edgeNormals[i - 1], edgeNormals[i]) < creaseDegrees) low = (edgeNormals[i - 1] + edgeNormals[i]).normalized;
            if (i < edges - 1 && Vector2.Angle(edgeNormals[i], edgeNormals[i + 1]) < creaseDegrees) high = (edgeNormals[i] + edgeNormals[i + 1]).normalized;

            int start = verts.Count;
            for (int j = 0; j <= Segments; j++)
            {
                float angle = j * Mathf.PI * 2f / Segments;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                verts.Add(new Vector3(profile[i].x * cos, profile[i].y, profile[i].x * sin));
                verts.Add(new Vector3(profile[i + 1].x * cos, profile[i + 1].y, profile[i + 1].x * sin));
                normals.Add(new Vector3(low.x * cos, low.y, low.x * sin));
                normals.Add(new Vector3(high.x * cos, high.y, high.x * sin));
                uvs.Add(new Vector2((float)j / Segments, (float)i / edges));
                uvs.Add(new Vector2((float)j / Segments, (float)(i + 1) / edges));
            }
            for (int j = 0; j < Segments; j++)
            {
                int l0 = start + j * 2, u0 = l0 + 1, l1 = l0 + 2, u1 = l0 + 3;
                tris.Add(l0); tris.Add(u0); tris.Add(l1);
                tris.Add(l1); tris.Add(u0); tris.Add(u1);
            }
        }
        return Fill(key, verts, normals, uvs, tris);
    }

    /// <summary>A quarter-round from one point of an outline to the next, for <see cref="Lathe"/>: the points between, bulging outwards.</summary>
    public static void Arc(List<Vector2> profile, Vector2 centre, float radius, float fromDegrees, float toDegrees, int steps = 6)
    {
        for (int s = 0; s <= steps; s++)
        {
            float angle = Mathf.Lerp(fromDegrees, toDegrees, (float)s / steps) * Mathf.Deg2Rad;
            profile.Add(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }
}
