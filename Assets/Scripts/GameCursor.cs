using UnityEngine;

/// <summary>
/// Swaps the system arrow for the amber one wherever the mouse pointer is shown (menu, loadout,
/// pause, settings, game over, win screens). Set once at startup; Cursor.visible elsewhere still decides when it shows.
/// </summary>
public static class GameCursor
{
    static readonly Vector2 Hotspot = new Vector2(1f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        var tex = Resources.Load<Texture2D>("VantageCursor");
        if (tex != null) Cursor.SetCursor(tex, Hotspot, CursorMode.Auto);
    }
}
