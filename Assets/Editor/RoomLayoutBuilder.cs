using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Phase 17 Pass 2: the solid layout pieces that turn each room from "one box" into a space built around its
/// lesson. Unlike EnvironmentDresser (visual only), every piece here is real gameplay geometry: Blocking layer
/// (stops drone sight and dart scans), a collider, and a carving NavMeshObstacle so drones path round it.
/// Kept as data in code so the layout can be rebuilt in one click and re-verified after any change.
/// Pieces avoid the dresser's hanging lamps (x = room centre at z -6.5/-3/0.5/4) and the low west vent (z -0.8).
/// </summary>
public static class RoomLayoutBuilder
{
    const string RootName = "RoomLayout";
    const int BlockingLayer = 8;

    [MenuItem("Vantage/Environment/Rebuild Layout Pieces")]
    public static void Rebuild()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject(RootName).transform;

        var wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Wall_Concrete.mat");
        var teal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Cover_Teal.mat");
        var orange = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Cover_Orange.mat");
        var trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Env/Mat_Env_Trim.mat");

        // Range (x=0): a waist-high shooting counter with a walk-through gap - it reads as a range, and it's
        // low enough that standing aim still clears it while crouching behind it doesn't.
        var range = Group(root, "Range");
        Counter(range, "Counter_W", new Vector3(-3f, 0f, -4f), 3.2f, wall, trim);
        Counter(range, "Counter_E", new Vector3(3f, 0f, -4f), 3.2f, wall, trim);

        // Weak Point (x=-40): a knee-high firing line facing the drone lanes. Frames where you shoot from
        // without blocking a standing shot at the eyes.
        var weak = Group(root, "WeakPoint");
        Piece(weak, "FiringLine_W", new Vector3(-43.7f, 0.45f, -5.5f), new Vector3(4.2f, 0.9f, 0.3f), orange);
        Piece(weak, "FiringLine_E", new Vector3(-36.3f, 0.45f, -5.5f), new Vector3(4.2f, 0.9f, 0.3f), orange);

        // Holding Angle (x=40): two pillars. Behind the left one you can only see the right drone, and vice
        // versa - "peek one angle at a time" made physical instead of just told.
        var hold = Group(root, "HoldingAngle");
        Pillar(hold, "Pillar_W", new Vector3(36f, 0f, -1f), wall, trim);
        Pillar(hold, "Pillar_E", new Vector3(44f, 0f, -1f), wall, trim);

        // Recon (x=-120): the partition stays; a tall crate stack in the far east corner is one more place
        // something could be hiding - more reason to scout than to guess.
        var recon = Group(root, "Recon");
        Piece(recon, "CrateStack_NE", new Vector3(-115.6f, 1.1f, 4.4f), new Vector3(1.4f, 2.2f, 1.4f), orange);

        // Smoke (x=80): a wall across the room with one 3m gap, and both drones watch the gap. The Smoke
        // Wall is 3.2m wide - smoke the gap, walk through it.
        var smoke = Group(root, "Smoke");
        Wall(smoke, "Wall_W", new Vector3(76.35f, 0f, -1.5f), 4.3f, wall, trim);
        Wall(smoke, "Wall_E", new Vector3(83.65f, 0f, -1.5f), 4.3f, wall, trim);

        // Flash (x=120): a wall just short of the drones with a corner on the west side. From that corner
        // both drones (and the whole patrol) are inside the 7m flash radius: walk up safe, pop, peek.
        var flash = Group(root, "Flash");
        Wall(flash, "Wall", new Vector3(121.65f, 0f, 2.2f), 8.3f, wall, trim);

        // Dash (x=160): two staggered cover blocks with an exposed lane between them. Hide behind one, dash
        // to the other - the orb sits beside the second as the reward for committing.
        var dash = Group(root, "Dash");
        Piece(dash, "Cover_A", new Vector3(157f, 1.1f, -1.5f), new Vector3(1.8f, 2.2f, 1.2f), teal);
        Piece(dash, "Cover_B", new Vector3(162.5f, 1.1f, 1.2f), new Vector3(1.8f, 2.2f, 1.2f), teal);

        // The Round (x=200): a central crate stack splits the approach into two lanes, each leading to one of
        // the existing site covers; a low crate on the west flank gives an off-angle on the patrol. 2.2m, not a
        // full wall - still hides a standing player from the drones, without filling the view from the entry.
        var round = Group(root, "TheRound");
        Piece(round, "SplitCrates", new Vector3(200f, 1.1f, -4f), new Vector3(2f, 2.2f, 1f), teal);
        Piece(round, "FlankCrate_W", new Vector3(195f, 0.6f, 2f), new Vector3(1.2f, 1.2f, 1.2f), orange);

        // The Flash orb sat in the now-safe half of the room; past the corner it's a risk-for-reward pickup.
        var flashOrb = GameObject.Find("UltOrb_Flash");
        if (flashOrb != null) flashOrb.transform.position = new Vector3(124.5f, 0.9f, 3.8f);

        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
    }

    static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        return g;
    }

    /// <summary>Full-height wall, 0.4m thick, running along X, with a trim cap on top.</summary>
    static void Wall(Transform parent, string name, Vector3 floorCentre, float length, Material mat, Material cap, float thickness = 0.4f)
    {
        Piece(parent, name, floorCentre + Vector3.up * 1.5f, new Vector3(length, 3f, thickness), mat);
        Cap(parent, name + "_Cap", floorCentre + Vector3.up * 3.02f, new Vector3(length + 0.1f, 0.06f, thickness + 0.1f), cap);
    }

    /// <summary>Waist-high counter: concrete body with a lighter top so it doesn't read as a black block.</summary>
    static void Counter(Transform parent, string name, Vector3 floorCentre, float length, Material mat, Material top)
    {
        Piece(parent, name, floorCentre + Vector3.up * 0.47f, new Vector3(length, 0.94f, 0.5f), mat);
        Cap(parent, name + "_Top", floorCentre + Vector3.up * 0.97f, new Vector3(length + 0.08f, 0.06f, 0.6f), top);
    }

    static void Pillar(Transform parent, string name, Vector3 floorCentre, Material mat, Material cap)
    {
        Piece(parent, name, floorCentre + Vector3.up * 1.5f, new Vector3(0.8f, 3f, 0.8f), mat);
        Cap(parent, name + "_Base", floorCentre + Vector3.up * 0.09f, new Vector3(0.9f, 0.18f, 0.9f), cap);
    }

    static GameObject Piece(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        g.transform.localScale = size;
        g.layer = BlockingLayer;
        if (mat != null) g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);

        var obstacle = g.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = Vector3.one; // local space - the cube's own unit size, scaled with the transform
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        return g;
    }

    /// <summary>Visual-only trim: no collider, so it never changes gameplay.</summary>
    static void Cap(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        g.transform.localScale = size;
        Object.DestroyImmediate(g.GetComponent<BoxCollider>());
        if (mat != null) g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
    }
}
