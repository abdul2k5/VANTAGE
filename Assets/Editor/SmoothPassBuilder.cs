using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Phase 19.19: takes the hard edges off the things the player looks at closely - the breach charge, the
/// range targets, the drones' eyes, cover, crates, door frames and every round pipe, lamp and post. Only
/// the shapes drawn are changed: colliders, positions and sizes stay as they were, so nothing plays
/// differently. Safe to run again (Vantage > Smooth Pass), and it has to be after either of the
/// Environment rebuilds, which put plain cubes back.
/// </summary>
public static class SmoothPassBuilder
{
    const string SteelPath = "Assets/Art/Materials/Mat_Gun_Steel.mat";
    const string ReinforcementPrefab = "Assets/Prefabs/Sentinel_Reinforcement.prefab";
    const string MedkitPrefab = "Assets/Prefabs/Medkit.prefab";

    [MenuItem("Vantage/Smooth Pass")]
    public static void Apply()
    {
        var steel = AssetDatabase.LoadAssetAtPath<Material>(SteelPath);
        if (steel == null) { Debug.LogError("SmoothPassBuilder: run Vantage > Rebuild Guns first - " + SteelPath + " is missing."); return; }

        int charge = BuildBreachCharge(steel);
        int targets = BuildTargets(steel);

        int eyes = 0;
        foreach (var sentinel in Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (AddEyeRim(sentinel.transform, steel)) eyes++;
        EditPrefab(ReinforcementPrefab, root => AddEyeRim(root.transform, steel));
        EditPrefab(MedkitPrefab, root => { foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) RoundProp(mf); });

        int props = 0, tubes = 0;
        var gun = Object.FindFirstObjectByType<GunViewmodelAnimator>(FindObjectsInactive.Include);
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // The guns are built smooth by GunSetBuilder, and a prefab's copies take their shape from the prefab.
            if (gun != null && mf.transform.IsChildOf(gun.transform)) continue;
            if (PrefabUtility.IsPartOfPrefabInstance(mf.gameObject)) continue;
            if (RoundProp(mf)) { props++; continue; }
            if (mf.sharedMesh != null && mf.sharedMesh.name == "Cylinder")
            {
                mf.sharedMesh = SmoothMeshes.Cylinder(false);
                EditorUtility.SetDirty(mf);
                tubes++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("SmoothPassBuilder: breach charge parts " + charge + ", targets " + targets + ", drone eyes " + eyes + ", props " + props + ", round parts " + tubes + ".");
    }

    // ---- Breach charge: a round canister in place of a box on a plate. The core and the six lights are the
    // same objects as before, so BreachChargeDevice still lights them; only what they sit on is new.
    static int BuildBreachCharge(Material steel)
    {
        var device = Object.FindFirstObjectByType<BreachChargeDevice>(FindObjectsInactive.Include);
        if (device == null || device.canister == null) { Debug.LogWarning("SmoothPassBuilder: no breach charge in the scene."); return 0; }
        var can = device.canister;

        // Colours carried over from the parts being replaced.
        var oldBase = can.Find("Base");
        var oldBody = can.Find("Body");
        var baseMat = oldBase != null ? oldBase.GetComponent<Renderer>().sharedMaterial : steel;
        var bodyMat = oldBody != null ? oldBody.GetComponent<Renderer>().sharedMaterial : steel;
        foreach (var name in new[] { "Base", "Body", "Band_Low", "Band_High", "LightBand", "Collar" })
        {
            var old = can.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        // Heights and radii in the canister's own units (the device is scaled 1.7 in the scene).
        const float bodyRadius = 0.19f, bodyTop = 0.44f, lightHeight = 0.22f;

        var foot = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0.265f, 0f) };
        SmoothMeshes.Arc(foot, new Vector2(0.265f, 0.02f), 0.02f, -90f, 0f, 4);
        SmoothMeshes.Arc(foot, new Vector2(0.255f, 0.03f), 0.03f, 0f, 90f, 5);
        foot.Add(new Vector2(0f, 0.06f));
        Turned(can, "Base", SmoothMeshes.Lathe("Charge_Base", foot.ToArray()), baseMat);

        // Straight sides, then a rounded shoulder up to a flat top the core sits in.
        var body = new List<Vector2> { new Vector2(0f, 0.05f), new Vector2(bodyRadius - 0.015f, 0.05f) };
        SmoothMeshes.Arc(body, new Vector2(bodyRadius - 0.015f, 0.065f), 0.015f, -90f, 0f, 4);
        SmoothMeshes.Arc(body, new Vector2(bodyRadius - 0.08f, bodyTop - 0.08f), 0.08f, 0f, 90f, 8);
        body.Add(new Vector2(0f, bodyTop));
        Turned(can, "Body", SmoothMeshes.Lathe("Charge_Body", body.ToArray()), bodyMat);

        Turned(can, "Band_Low", SmoothMeshes.Lathe("Charge_Band_Low", Hoop(bodyRadius, 0.115f, 0.012f, 0.012f)), steel);
        Turned(can, "Band_High", SmoothMeshes.Lathe("Charge_Band_High", Hoop(bodyRadius, 0.325f, 0.012f, 0.012f)), steel);
        // A wider, flatter band at the lights' height for them to sit on.
        Turned(can, "LightBand", SmoothMeshes.Lathe("Charge_LightBand", Hoop(bodyRadius, lightHeight, 0.008f, 0.04f)), steel);
        Turned(can, "Collar", SmoothMeshes.Lathe("Charge_Collar", new[]
        {
            new Vector2(0.07f, bodyTop - 0.005f), new Vector2(0.105f, bodyTop - 0.005f), new Vector2(0.105f, bodyTop + 0.02f),
            new Vector2(0.095f, bodyTop + 0.032f), new Vector2(0.07f, bodyTop + 0.032f),
        }), steel);

        if (device.ringLightRenderers != null)
        {
            int count = device.ringLightRenderers.Length;
            for (int i = 0; i < count; i++)
            {
                if (device.ringLightRenderers[i] == null) continue;
                float angle = i * Mathf.PI * 2f / count;
                var t = device.ringLightRenderers[i].transform;
                // Half sunk into the band, so each reads as a lamp set in the casing rather than a ball floating beside it.
                t.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (bodyRadius + 0.006f) + Vector3.up * lightHeight;
                t.localScale = Vector3.one * 0.05f;
            }
        }
        if (device.coreRenderer != null) device.coreRenderer.transform.localPosition = new Vector3(0f, bodyTop + 0.03f, 0f);
        EditorUtility.SetDirty(device);
        return 6;
    }

    /// <summary>The outline of a raised hoop round a tube: how far it stands out and how tall it is, with its corners rounded off.</summary>
    static Vector2[] Hoop(float tubeRadius, float height, float stand, float halfTall)
    {
        float soft = Mathf.Min(stand, halfTall) * 0.6f;
        var outline = new List<Vector2> { new Vector2(tubeRadius - 0.004f, height - halfTall) };
        SmoothMeshes.Arc(outline, new Vector2(tubeRadius + stand - soft, height - halfTall + soft), soft, -90f, 0f, 4);
        SmoothMeshes.Arc(outline, new Vector2(tubeRadius + stand - soft, height + halfTall - soft), soft, 0f, 90f, 4);
        outline.Add(new Vector2(tubeRadius - 0.004f, height + halfTall));
        return outline.ToArray();
    }

    static Transform Turned(Transform parent, string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go.transform;
    }

    // ---- Range targets: a round plate with a soft rim and a dark ring, in place of the eight-sided one.
    static int BuildTargets(Material steel)
    {
        int built = 0;
        foreach (var target in Object.FindObjectsByType<RangeTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            const string suffix = "_Bullseye";
            if (!target.name.EndsWith(suffix) || target.transform.parent == null) continue;
            var props = target.transform.parent;
            string prefix = target.name.Substring(0, target.name.Length - suffix.Length);
            var plate = props.Find(prefix);
            if (plate == null || plate.GetComponent<MeshFilter>() == null) continue;

            // Where the old plate is and how big, from its mesh (its renderer is switched off after the first run).
            var local = plate.GetComponent<MeshFilter>().sharedMesh.bounds;
            var bounds = new Bounds(plate.TransformPoint(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                bounds.Encapsulate(plate.TransformPoint(local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            float radius = Mathf.Max(bounds.size.x, bounds.size.y) * 0.5f;
            float thick = Mathf.Max(bounds.size.z, 0.03f);

            foreach (var name in new[] { prefix + "_Disc", prefix + "_Ring" })
            {
                var old = props.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            float soft = Mathf.Min(0.012f, thick * 0.45f);
            var outline = new List<Vector2> { new Vector2(0f, -thick * 0.5f) };
            SmoothMeshes.Arc(outline, new Vector2(radius - soft, -thick * 0.5f + soft), soft, -90f, 0f, 4);
            SmoothMeshes.Arc(outline, new Vector2(radius - soft, thick * 0.5f - soft), soft, 0f, 90f, 4);
            outline.Add(new Vector2(0f, thick * 0.5f));
            var disc = Turned(props, prefix + "_Disc", SmoothMeshes.Lathe("Target_Disc", outline.ToArray()), plate.GetComponent<Renderer>().sharedMaterial);
            // The lathe turns round Y; laid over so the plate faces along the range.
            disc.SetPositionAndRotation(bounds.center, Quaternion.Euler(90f, 0f, 0f));

            var ring = Turned(props, prefix + "_Ring", GunSetBuilder.RingMesh(), steel);
            ring.position = bounds.center;
            ring.localScale = new Vector3(radius * 1.3f, radius * 1.3f, thick + 0.008f);

            plate.GetComponent<Renderer>().enabled = false;
            // The plate is what turns green on a hit.
            target.face = disc.GetComponent<Renderer>();
            EditorUtility.SetDirty(target);
            built++;
        }
        return built;
    }

    // ---- Drones: a steel rim round the eye, so it reads as a lens in a housing. No collider: the eye is the
    // weak point and must stay as easy to hit as it was.
    static bool AddEyeRim(Transform sentinel, Material steel)
    {
        var body = sentinel.Find("Body");
        var eye = body != null ? body.Find("Eye") : null;
        if (eye == null) return false;
        var old = body.Find("EyeRim");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var rim = Turned(body, "EyeRim", GunSetBuilder.RingMesh(), steel);
        // Set back from the front of the eye, which still stands proud of it.
        rim.localPosition = eye.localPosition - Vector3.forward * (eye.localScale.z * 0.05f);
        rim.localScale = new Vector3(eye.localScale.x * 1.17f, eye.localScale.y * 1.17f, eye.localScale.z * 0.4f);
        return true;
    }

    // ---- Props: the same boxes with their edges rounded. The radius is by what the thing is.
    static float EdgeRadiusFor(Transform t)
    {
        string name = t.name, parent = t.parent != null ? t.parent.name : "";
        if (name.Contains("_Cover") || name.Contains("_CrouchCover")) return 0.06f;
        if (name.StartsWith("CrateStack") || name.StartsWith("FlankCrate")) return 0.05f;
        if (name.StartsWith("FiringLine_")) return 0.04f;
        if (name.StartsWith("Counter_") && name.EndsWith("_Top")) return 0.02f;
        if (name.StartsWith("Pillar_") && name.EndsWith("_Base")) return 0.03f;
        if (parent.StartsWith("DoorFrame_"))
        {
            if (name == "LeftJamb" || name == "RightJamb" || name == "Lintel") return 0.03f;
            if (name == "LeftPanel" || name == "RightPanel") return 0.02f;
        }
        if (name == "TerminalBody") return 0.03f;
        if (name == "Case" && parent == "Visual") return 0.04f;
        return 0f;
    }

    static bool RoundProp(MeshFilter mf)
    {
        if (mf.sharedMesh == null || (mf.sharedMesh.name != "Cube" && !mf.sharedMesh.name.StartsWith("Box_"))) return false;
        float radius = EdgeRadiusFor(mf.transform);
        if (radius <= 0f) return false;
        mf.sharedMesh = SmoothMeshes.RoundedBox(mf.transform.lossyScale, radius);
        EditorUtility.SetDirty(mf);
        return true;
    }

    static void EditPrefab(string path, System.Action<GameObject> edit)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
        var root = PrefabUtility.LoadPrefabContents(path);
        edit(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
