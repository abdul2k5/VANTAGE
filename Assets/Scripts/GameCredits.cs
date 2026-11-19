using UnityEngine;

/// <summary>
/// The credits text, shared by the win screen's second page (WinCreditsUI) and the main menu's CREDITS page,
/// so the two can't drift apart. Kept in step with CREDITS.md - the Stinger Drone line is a CC-BY attribution and is required.
/// </summary>
public static class GameCredits
{
    public const string AuthorName = "Abdul Hadi Ghani";
    public const string AuthorRoles = "Game design, programming, level design, interface and sound mixing";
    public const string MadeFor = "COMP3218 Game Design and Development, University of Southampton Malaysia, 2026";

    const string Heading = "#E2A53C";
    const string Dim = "#9EA699";

    /// <summary>Rich text for a Text component; fontSize is that Text's size, used to scale the author's name up.</summary>
    public static string Build(int fontSize)
    {
        return
            "<color=" + Dim + ">A game by</color>\n" +
            "<size=" + Mathf.RoundToInt(fontSize * 1.75f) + "><b>" + AuthorName + "</b></size>\n" +
            AuthorRoles + "\n" +
            "<color=" + Dim + ">" + MadeFor + "</color>\n" +
            "\n" +
            "<color=" + Heading + ">MUSIC</color>\n" +
            "Loading Screen Loop  -  Brandon Morris\n" +
            "Dark City  -  cinameng\n" +
            "Fast Fight / Battle Music Looped  -  Ville Nousiainen\n" +
            "\n" +
            "<color=" + Heading + ">SOUND EFFECTS</color>\n" +
            "Sci-fi Sounds, Impact Sounds, Interface Sounds and Music Jingles  -  Kenney\n" +
            "\n" +
            "<color=" + Heading + ">3D MODELS</color>\n" +
            "Blaster Kit  -  Kenney\n" +
            "Stinger Drone by Aaron Clifford [CC-BY 3.0] via Poly Pizza (poly.pizza/m/6CUQX98vha4)\n" +
            "\n" +
            "<color=" + Dim + ">All music, sounds and the Blaster Kit are CC0. Made with Unity 6.</color>\n" +
            "Thank you for playing.";
    }
}
