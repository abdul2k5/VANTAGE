using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;

/// <summary>One-off headless NavMesh rebake for Phase 5's new rooms. Run via
/// `Unity.exe -batchmode -quit -executeMethod RebakeNavMesh.Run -projectPath ...`</summary>
public static class RebakeNavMesh
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Corridor3D.unity");
        NavMeshBuilder.BuildNavMesh();
        EditorSceneManager.SaveScene(scene);
    }
}
