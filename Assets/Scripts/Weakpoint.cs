using System.Collections.Generic;
using UnityEngine;

/// <summary>Marks the drone's eye as the headshot zone. Its collider only gets the bullet to the drone: Weapon then asks IsHitBy whether the shot touches the eye you can actually see, so the box-shaped body hitbox can't swallow eye shots from the side, and air beside the eye doesn't count.</summary>
public class Weakpoint : MonoBehaviour
{
    public float damageMultiplier = 2.5f;

    MeshFilter eyeMesh;
    MeshFilter[] bodyMeshes;
    Renderer[] bodyRenderers;

    static readonly Dictionary<Mesh, Vector3[]> vertexCache = new Dictionary<Mesh, Vector3[]>();
    static readonly Dictionary<Mesh, int[]> triangleCache = new Dictionary<Mesh, int[]>();

    void Awake()
    {
        eyeMesh = GetComponent<MeshFilter>();
        // The eye sits on the drone's body model, so every other mesh under the same parent can hide it.
        var found = transform.parent != null ? transform.parent.GetComponentsInChildren<MeshFilter>(true) : new MeshFilter[0];
        var meshes = new List<MeshFilter>();
        for (int i = 0; i < found.Length; i++) if (found[i] != eyeMesh) meshes.Add(found[i]);
        bodyMeshes = meshes.ToArray();
        bodyRenderers = new Renderer[bodyMeshes.Length];
        for (int i = 0; i < bodyMeshes.Length; i++) bodyRenderers[i] = bodyMeshes[i].GetComponent<Renderer>();
    }

    /// <summary>True if the shot reaches the visible eye before any other visible part of the drone.</summary>
    public bool IsHitBy(Ray ray, float maxDistance)
    {
        float eyeDistance;
        if (eyeMesh == null || !RaycastMesh(eyeMesh, ray, maxDistance, out eyeDistance)) return false;

        float blockedAt;
        for (int i = 0; i < bodyMeshes.Length; i++)
        {
            if (bodyRenderers[i] != null && !bodyRenderers[i].enabled) continue;
            if (RaycastMesh(bodyMeshes[i], ray, eyeDistance, out blockedAt)) return false;
        }
        return true;
    }

    static bool RaycastMesh(MeshFilter filter, Ray ray, float maxDistance, out float distance)
    {
        distance = maxDistance;
        Mesh mesh = filter.sharedMesh;
        if (mesh == null || !mesh.isReadable || !filter.gameObject.activeInHierarchy) return false;

        // Moved into the mesh's own space with scale included, origin + t * direction has the same t as in world space, so distances compare directly.
        Transform t = filter.transform;
        Vector3 o = t.InverseTransformPoint(ray.origin);
        Vector3 d = t.InverseTransformVector(ray.direction);
        if (!mesh.bounds.IntersectRay(new Ray(o, d))) return false;

        Vector3[] vertices;
        if (!vertexCache.TryGetValue(mesh, out vertices)) { vertices = mesh.vertices; vertexCache[mesh] = vertices; }
        int[] triangles;
        if (!triangleCache.TryGetValue(mesh, out triangles)) { triangles = mesh.triangles; triangleCache[mesh] = triangles; }

        bool hit = false;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Moller-Trumbore, both faces.
            Vector3 v0 = vertices[triangles[i]];
            Vector3 e1 = vertices[triangles[i + 1]] - v0;
            Vector3 e2 = vertices[triangles[i + 2]] - v0;
            Vector3 p = Vector3.Cross(d, e2);
            float det = Vector3.Dot(e1, p);
            if (det > -1e-12f && det < 1e-12f) continue;
            float inverse = 1f / det;
            Vector3 s = o - v0;
            float u = Vector3.Dot(s, p) * inverse;
            if (u < 0f || u > 1f) continue;
            Vector3 q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(d, q) * inverse;
            if (v < 0f || u + v > 1f) continue;
            float along = Vector3.Dot(e2, q) * inverse;
            if (along > 0f && along < distance) { distance = along; hit = true; }
        }
        return hit;
    }
}
