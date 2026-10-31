using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Environment Pass 1 ("night-shift facility"): rebuilds all set dressing for every room under one
/// "EnvDressing" root - steel roof beams, hanging work lamps, wall caps/trims/pilasters, pipes, vents,
/// coloured accent strips and floor lines - plus the night lighting, fog and post-processing.
///
/// Everything here is visual only: no colliders. That is deliberate - this pass must not change a single
/// drone sightline, bullet, dart bounce or walkable spot, so the tuned difficulty stays exactly as it is.
/// Re-running the menu item deletes and rebuilds the dressing, so tuning is: edit a number, run it again.
/// </summary>
public static class EnvironmentDresser
{
    const string RootName = "EnvDressing";
    const string MatFolder = "Assets/Art/Materials/Env";
    const float WallHeight = 3f;
    const float HalfInnerWidth = 5.8f;   // walls at centre +/-6, 0.4 thick
    const float SouthInner = -8.8f;      // south wall at z=-9
    const float DoorWallInner = 6.0f;    // exit door wall at z=6.2
    const float NoDoorNorthInner = 8.8f; // The Round has no exit door

    struct RoomInfo
    {
        public string name;
        public float cx;
        public float northInner;
        public bool hasDoor;
        public Color accent;
    }

    // Accent per room, in RoomManager order: Range, Weak Point, Holding Angle, Recon, Smoke, Flash, Dash, The Round.
    static readonly Color[] Accents =
    {
        new Color(1.00f, 0.62f, 0.20f), // Range - amber
        new Color(1.00f, 0.22f, 0.20f), // Weak Point - red (the eye)
        new Color(1.00f, 0.50f, 0.15f), // Holding Angle - orange (its cover colour)
        new Color(0.30f, 0.70f, 1.00f), // Recon - dart blue
        new Color(0.20f, 0.85f, 0.80f), // Smoke - teal (its cover colour)
        new Color(1.00f, 0.95f, 0.70f), // Flash - white-gold
        new Color(0.70f, 0.40f, 1.00f), // Dash - violet
        new Color(0.20f, 0.85f, 0.80f), // The Round - site teal
    };

    [MenuItem("Vantage/Environment/Rebuild All (lighting + dressing)")]
    public static void RebuildAll()
    {
        ApplyNightLighting();
        RebuildDressing();
    }

    [MenuItem("Vantage/Environment/Rebuild Dressing")]
    public static void RebuildDressing()
    {
        var rm = Object.FindFirstObjectByType<RoomManager>();
        if (rm == null) { Debug.LogError("EnvironmentDresser: no RoomManager in scene."); return; }

        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject(RootName);

        var doors = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rm.rooms.Length; i++)
        {
            if (rm.rooms[i].entryPoint == null) continue;
            var info = new RoomInfo();
            info.name = rm.rooms[i].title;
            info.cx = rm.rooms[i].entryPoint.position.x;
            info.hasDoor = false;
            foreach (var d in doors) if (Mathf.Abs(d.transform.position.x - info.cx) < 7f) info.hasDoor = true;
            info.northInner = info.hasDoor ? DoorWallInner : NoDoorNorthInner;
            info.accent = Accents[Mathf.Min(i, Accents.Length - 1)];

            var roomRoot = new GameObject("Room" + (i + 1) + "_" + info.name.Replace(" ", ""));
            roomRoot.transform.SetParent(root.transform, false);
            DressRoom(roomRoot.transform, info);
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("EnvironmentDresser: rebuilt dressing for " + root.transform.childCount + " rooms.");
    }

    // ---------------------------------------------------------------- lighting / post

    [MenuItem("Vantage/Environment/Apply Night Lighting")]
    public static void ApplyNightLighting()
    {
        // Sky: dark navy, no visible sun disc - the rooms are open-roofed, so this is what's overhead.
        var sky = GetOrCreateMaterial("Mat_Sky_Night", "Skybox/Procedural");
        sky.SetFloat("_SunDisk", 0f);
        sky.SetFloat("_AtmosphereThickness", 0.45f);
        sky.SetColor("_SkyTint", new Color(0.16f, 0.2f, 0.32f));
        sky.SetColor("_GroundColor", new Color(0.05f, 0.05f, 0.06f));
        sky.SetFloat("_Exposure", 0.5f);
        EditorUtility.SetDirty(sky);
        RenderSettings.skybox = sky;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.32f, 0.36f, 0.46f);
        RenderSettings.ambientEquatorColor = new Color(0.24f, 0.25f, 0.29f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.13f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.07f, 0.08f, 0.11f);
        RenderSettings.fogDensity = 0.028f;

        // Moonlight: cool, low, steep enough that walls throw shadow across the floor.
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.color = new Color(0.55f, 0.63f, 0.9f);
            l.intensity = 0.35f;
            l.shadows = LightShadows.Soft;
            l.transform.rotation = Quaternion.Euler(58f, -35f, 0f);
            EditorUtility.SetDirty(l);
        }

        ConfigurePipeline();
        BuildPostProfile();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    /// <summary>Forward+ (no 4-lights-per-object cap, so every lamp reaches the floor) + additional light shadows.</summary>
    static void ConfigurePipeline()
    {
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null) return;
        // Read-only in the API - set through the serialized fields.
        var so = new SerializedObject(asset);
        so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
        so.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);

        var list = so.FindProperty("m_RendererDataList");
        for (int i = 0; i < list.arraySize; i++)
        {
            var data = list.GetArrayElementAtIndex(i).objectReferenceValue as UniversalRendererData;
            if (data == null) continue;
            data.renderingMode = RenderingMode.ForwardPlus;
            EditorUtility.SetDirty(data);
        }
    }

    /// <summary>
    /// Phase 6a configured this profile, but its effects were never saved into the asset - the file held four
    /// null references, so since the next editor restart the game ran with no post-processing at all. Here the
    /// effects are added as real sub-assets (AddObjectToAsset) so they persist.
    /// </summary>
    static void BuildPostProfile()
    {
        var volume = Object.FindFirstObjectByType<Volume>();
        if (volume == null || volume.sharedProfile == null) return;
        var profile = volume.sharedProfile;
        string path = AssetDatabase.GetAssetPath(profile);

        // Drop the dangling references and any previous sub-assets, then rebuild.
        profile.components.RemoveAll(c => c == null);
        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (sub is VolumeComponent) { profile.components.Remove((VolumeComponent)sub); Object.DestroyImmediate(sub, true); }
        }
        profile.components.Clear();

        var tone = Add<Tonemapping>(profile, path);
        tone.mode.Override(TonemappingMode.ACES);

        var color = Add<ColorAdjustments>(profile, path);
        color.postExposure.Override(0.55f);
        color.contrast.Override(14f);
        color.saturation.Override(8f);

        var wb = Add<WhiteBalance>(profile, path);
        wb.temperature.Override(-6f);

        var bloom = Add<Bloom>(profile, path);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.7f);
        bloom.scatter.Override(0.65f);

        var vig = Add<Vignette>(profile, path);
        vig.intensity.Override(0.3f);
        vig.smoothness.Override(0.45f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    static T Add<T>(VolumeProfile profile, string path) where T : VolumeComponent
    {
        var c = profile.Add<T>(true);
        c.name = typeof(T).Name;
        c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(c, path);
        return c;
    }

    // ---------------------------------------------------------------- per-room dressing

    static void DressRoom(Transform parent, RoomInfo r)
    {
        var steel = GetLit("Mat_Env_Steel", new Color(0.16f, 0.17f, 0.19f), 0.6f, 0.45f);
        var trim = GetLit("Mat_Env_Trim", new Color(0.09f, 0.1f, 0.11f), 0.2f, 0.3f);
        var pipe = GetLit("Mat_Env_Pipe", new Color(0.32f, 0.34f, 0.33f), 0.7f, 0.55f);
        var pipeAccent = GetLit("Mat_Env_PipeYellow", new Color(0.75f, 0.58f, 0.12f), 0.3f, 0.4f);
        var vent = GetLit("Mat_Env_Vent", new Color(0.07f, 0.075f, 0.08f), 0.4f, 0.3f);
        var shade = GetLit("Mat_Env_LampShade", new Color(0.12f, 0.13f, 0.12f), 0.5f, 0.4f);
        var bulb = GetEmissive("Mat_Env_Bulb", new Color(1f, 0.82f, 0.55f), 3.2f);
        var floorLine = GetLit("Mat_Env_FloorLine", new Color(0.78f, 0.62f, 0.14f), 0f, 0.15f);
        var accent = GetEmissive("Mat_Env_Accent_" + Sanitize(r.name), r.accent, 2.2f);

        float zS = SouthInner, zN = r.northInner, cx = r.cx;
        float length = zN - zS;
        float zMid = (zS + zN) * 0.5f;

        // Cross beams on the wall tops, one every 4m, each over a pair of pilasters.
        var beamZs = new List<float>();
        // Spaced to reach the back of the room too - that's where the drones hold, and they must stay readable.
        for (float z = -6.5f; z < zN - 1.5f; z += 3.5f) beamZs.Add(z);

        var structure = Group(parent, "Structure");
        foreach (float z in beamZs)
        {
            IBeam(structure, "Beam", new Vector3(cx, WallHeight + 0.2f, z), 12.4f, steel);
            Box(structure, "Pilaster_W", new Vector3(cx - HalfInnerWidth + 0.1f, WallHeight * 0.5f, z), new Vector3(0.2f, WallHeight, 0.45f), steel);
            Box(structure, "Pilaster_E", new Vector3(cx + HalfInnerWidth - 0.1f, WallHeight * 0.5f, z), new Vector3(0.2f, WallHeight, 0.45f), steel);
        }
        // Two lengthwise purlins resting on the beams.
        Box(structure, "Purlin_W", new Vector3(cx - 3f, WallHeight + 0.47f, zMid), new Vector3(0.12f, 0.14f, length + 0.4f), steel);
        Box(structure, "Purlin_E", new Vector3(cx + 3f, WallHeight + 0.47f, zMid), new Vector3(0.12f, 0.14f, length + 0.4f), steel);

        // Wall caps - a dark rim on every wall top gives the room a hard silhouette against the sky.
        var caps = Group(parent, "WallCaps");
        Box(caps, "Cap_S", new Vector3(cx, WallHeight + 0.04f, -9f), new Vector3(12.6f, 0.08f, 0.55f), trim);
        Box(caps, "Cap_N", new Vector3(cx, WallHeight + 0.04f, 9f), new Vector3(12.6f, 0.08f, 0.55f), trim);
        Box(caps, "Cap_W", new Vector3(cx - 6f, WallHeight + 0.04f, 0f), new Vector3(0.55f, 0.08f, 18.6f), trim);
        Box(caps, "Cap_E", new Vector3(cx + 6f, WallHeight + 0.04f, 0f), new Vector3(0.55f, 0.08f, 18.6f), trim);
        if (r.hasDoor)
        {
            Box(caps, "Cap_DoorWall_W", new Vector3(cx - 3.8f, WallHeight + 0.04f, 6.2f), new Vector3(4.8f, 0.08f, 0.55f), trim);
            Box(caps, "Cap_DoorWall_E", new Vector3(cx + 3.8f, WallHeight + 0.04f, 6.2f), new Vector3(4.8f, 0.08f, 0.55f), trim);
        }

        // Baseboards along the inner faces.
        Box(caps, "Base_W", new Vector3(cx - HalfInnerWidth + 0.03f, 0.09f, zMid), new Vector3(0.06f, 0.18f, length), trim);
        Box(caps, "Base_E", new Vector3(cx + HalfInnerWidth - 0.03f, 0.09f, zMid), new Vector3(0.06f, 0.18f, length), trim);
        Box(caps, "Base_S", new Vector3(cx, 0.09f, zS + 0.03f), new Vector3(HalfInnerWidth * 2f, 0.18f, 0.06f), trim);

        // Accent strips: the room's colour, running down both side walls. Each room reads as its own space.
        var accents = Group(parent, "Accents");
        Box(accents, "Accent_W", new Vector3(cx - HalfInnerWidth + 0.02f, 2.2f, zMid), new Vector3(0.03f, 0.05f, length - 0.6f), accent);
        Box(accents, "Accent_E", new Vector3(cx + HalfInnerWidth - 0.02f, 2.2f, zMid), new Vector3(0.03f, 0.05f, length - 0.6f), accent);
        Box(accents, "Accent_S_W", new Vector3(cx - 3.6f, 2.2f, zS + 0.02f), new Vector3(3.6f, 0.05f, 0.03f), accent);
        Box(accents, "Accent_S_E", new Vector3(cx + 3.6f, 2.2f, zS + 0.02f), new Vector3(3.6f, 0.05f, 0.03f), accent);

        // Hanging work lamps, one under each beam - the rooms' real light sources.
        var lamps = Group(parent, "Lamps");
        for (int i = 0; i < beamZs.Count; i++)
        {
            Lamp(lamps, new Vector3(cx, 0f, beamZs[i]), steel, shade, bulb, i == beamZs.Count / 2);
        }

        // Pipes along the east wall, under the beams, with brackets; one drops to the floor in the corner.
        var pipes = Group(parent, "Pipes");
        float px = cx + HalfInnerWidth - 0.14f;
        Cyl(pipes, "Pipe_Hi", new Vector3(px, 2.72f, zMid), length - 0.3f, 0.07f, Axis.Z, pipe);
        Cyl(pipes, "Pipe_Lo", new Vector3(px, 2.5f, zMid), length - 0.3f, 0.045f, Axis.Z, pipeAccent);
        for (float z = zS + 1.5f; z < zN - 0.5f; z += 3f)
            Box(pipes, "Bracket", new Vector3(px + 0.03f, 2.61f, z), new Vector3(0.18f, 0.36f, 0.05f), steel);
        Cyl(pipes, "Pipe_Drop", new Vector3(px, 1.36f, zS + 0.25f), 2.72f, 0.07f, Axis.Y, pipe);

        // Vents on the west wall (clear of the Recon partition at z=-3 and the checkpoint terminal on the south wall).
        var vents = Group(parent, "Vents");
        Vent(vents, new Vector3(cx - HalfInnerWidth + 0.04f, 2.35f, -6.2f), vent, trim);
        Vent(vents, new Vector3(cx - HalfInnerWidth + 0.04f, 2.35f, 2.6f), vent, trim);
        Vent(vents, new Vector3(cx - HalfInnerWidth + 0.04f, 0.45f, -0.8f), vent, trim);

        // Floor guide lines 0.6m in from each side wall.
        var floor = Group(parent, "FloorLines");
        Box(floor, "Line_W", new Vector3(cx - HalfInnerWidth + 0.6f, 0.004f, zMid), new Vector3(0.08f, 0.006f, length - 0.4f), floorLine);
        Box(floor, "Line_E", new Vector3(cx + HalfInnerWidth - 0.6f, 0.004f, zMid), new Vector3(0.08f, 0.006f, length - 0.4f), floorLine);
    }

    static void Lamp(Transform parent, Vector3 floorPos, Material steel, Material shade, Material bulb, bool castShadows)
    {
        var g = Group(parent, "Lamp");
        g.position = floorPos;
        const float shadeY = 2.72f;
        Cyl(g, "Cable", new Vector3(0f, (WallHeight + 0.05f + shadeY) * 0.5f, 0f), WallHeight + 0.05f - shadeY, 0.012f, Axis.Y, steel, true);
        Cyl(g, "Shade", new Vector3(0f, shadeY, 0f), 0.09f, 0.28f, Axis.Y, shade, true);
        Cyl(g, "Bulb", new Vector3(0f, shadeY - 0.05f, 0f), 0.012f, 0.2f, Axis.Y, bulb, true);

        var lgo = new GameObject("Light");
        lgo.transform.SetParent(g, false);
        lgo.transform.localPosition = new Vector3(0f, shadeY - 0.08f, 0f);
        lgo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var l = lgo.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = new Color(1f, 0.82f, 0.6f);
        l.intensity = 5f;
        l.range = 9f;
        l.spotAngle = 140f;
        l.innerSpotAngle = 40f;
        l.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
    }

    static void Vent(Transform parent, Vector3 pos, Material grille, Material frame)
    {
        var g = Group(parent, "Vent");
        g.position = pos;
        Box(g, "Frame", Vector3.zero, new Vector3(0.05f, 0.5f, 0.9f), frame, true);
        for (int i = 0; i < 5; i++)
            Box(g, "Slat", new Vector3(0.03f, -0.18f + i * 0.09f, 0f), new Vector3(0.03f, 0.035f, 0.8f), grille, true);
    }

    // ---------------------------------------------------------------- primitives

    enum Axis { X, Y, Z }

    static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        return g;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool local = false)
    {
        return Prim(PrimitiveType.Cube, parent, name, pos, Quaternion.identity, size, mat, local);
    }

    static void IBeam(Transform parent, string name, Vector3 pos, float length, Material mat)
    {
        var g = Group(parent, name);
        g.position = pos;
        Box(g, "Web", Vector3.zero, new Vector3(length, 0.34f, 0.05f), mat, true);
        Box(g, "FlangeTop", new Vector3(0f, 0.17f, 0f), new Vector3(length, 0.04f, 0.22f), mat, true);
        Box(g, "FlangeBottom", new Vector3(0f, -0.17f, 0f), new Vector3(length, 0.04f, 0.22f), mat, true);
    }

    /// <summary>Cylinder primitive: Unity's is 2 units tall along Y, so height is halved into scale.</summary>
    static void Cyl(Transform parent, string name, Vector3 pos, float height, float radius, Axis axis, Material mat, bool local = false)
    {
        Quaternion rot = axis == Axis.Y ? Quaternion.identity : axis == Axis.Z ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 0f, 90f);
        Prim(PrimitiveType.Cylinder, parent, name, pos, rot, new Vector3(radius * 2f, height * 0.5f, radius * 2f), mat, local);
    }

    static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat, bool local)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        // Visual only - see class summary. No collider means no effect on sight, bullets, darts or movement.
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        if (local) { go.transform.localPosition = pos; go.transform.localRotation = rot; }
        else { go.transform.position = pos; go.transform.rotation = rot; }
        go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = scale.x < 0.1f && scale.z < 0.1f ? ShadowCastingMode.Off : ShadowCastingMode.On;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    // ---------------------------------------------------------------- materials (saved assets, shared across rooms)

    static Material GetLit(string name, Color color, float metallic, float smoothness)
    {
        var m = GetOrCreateMaterial(name, "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material GetEmissive(string name, Color color, float strength)
    {
        var m = GetOrCreateMaterial(name, "Universal Render Pipeline/Lit");
        var baseCol = color * 0.5f;
        baseCol.a = 1f;
        m.SetColor("_BaseColor", baseCol);
        // Plain EnableKeyword didn't stick on these saved assets (keywords came back empty, so nothing glowed).
        m.SetKeyword(new LocalKeyword(m.shader, "_EMISSION"), true);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        var emis = color * strength;
        emis.a = 1f;
        m.SetColor("_EmissionColor", emis);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material GetOrCreateMaterial(string name, string shader)
    {
        if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder("Assets/Art/Materials", "Env");
        string path = MatFolder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
        }
        return m;
    }

    static string Sanitize(string s)
    {
        return s.Replace(" ", "");
    }
}
