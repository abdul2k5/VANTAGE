using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Second page of the win screen. The run debrief comes first with a CONTINUE button; that opens the
/// credits (who made the game, and the music, sounds and models it uses), where PLAY AGAIN and MAIN MENU
/// now sit. Kept in step with CREDITS.md - the Stinger Drone line is a CC-BY attribution and is required.
/// The page is built here from copies of the win panel's own text and buttons, so it matches them.
/// </summary>
public class WinCreditsUI : MonoBehaviour
{
    [Header("Already on the win panel")]
    public Text titleText;
    public Text subText;
    public GameObject debrief;
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Credits")]
    public string gameTitle = "VANTAGE";
    public string authorName = "Abdul Hadi Ghani";
    public string authorRoles = "Game design, programming, level design, interface and sound mixing";
    public string madeFor = "COMP3218 Coursework 1, University of Southampton, 2026";
    public float backButtonY = -280f;

    const string Heading = "#E2A53C";
    const string Dim = "#9EA699";

    string resultsTitle;
    Text creditsText;
    Button continueButton;
    Button backButton;

    void Awake()
    {
        if (titleText != null) resultsTitle = titleText.text;
        if (playAgainButton == null) return;

        // CONTINUE takes PLAY AGAIN's place on the results page.
        continueButton = CloneButton(playAgainButton, "ContinueButton", "CONTINUE");
        continueButton.onClick.AddListener(ShowCredits);

        backButton = CloneButton(playAgainButton, "BackToResultsButton", "BACK TO RESULTS");
        var backRect = (RectTransform)backButton.transform;
        backRect.anchoredPosition = new Vector2(backRect.anchoredPosition.x, backButtonY);
        backButton.onClick.AddListener(ShowResults);

        if (subText != null)
        {
            creditsText = Instantiate(subText, subText.transform.parent);
            creditsText.name = "CreditsText";
            var rt = creditsText.rectTransform;
            // Hung from its top edge, so the block starts under the title and ends above the buttons.
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, 290f);
            rt.sizeDelta = new Vector2(1100f, 400f);
            creditsText.supportRichText = true;
            creditsText.alignment = TextAnchor.UpperCenter;
            creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsText.verticalOverflow = VerticalWrapMode.Overflow;
            creditsText.lineSpacing = 1.1f;
            creditsText.raycastTarget = false;
            creditsText.text = BuildCredits();
        }
    }

    void OnEnable()
    {
        // Every win opens on the run results, not on wherever the last one was left.
        ShowResults();
    }

    public void ShowResults()
    {
        SetPage(false);
    }

    public void ShowCredits()
    {
        SetPage(true);
    }

    void SetPage(bool credits)
    {
        if (continueButton == null) return;

        if (titleText != null) titleText.text = credits ? gameTitle : resultsTitle;
        if (subText != null) subText.gameObject.SetActive(!credits);
        if (debrief != null) debrief.SetActive(!credits);
        continueButton.gameObject.SetActive(!credits);

        if (creditsText != null) creditsText.gameObject.SetActive(credits);
        playAgainButton.gameObject.SetActive(credits);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(credits);
        backButton.gameObject.SetActive(credits);
    }

    string BuildCredits()
    {
        return
            "<color=" + Dim + ">A game by</color>\n" +
            "<size=28><b>" + authorName + "</b></size>\n" +
            authorRoles + "\n" +
            "<color=" + Dim + ">" + madeFor + "</color>\n" +
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

    static Button CloneButton(Button source, string name, string label)
    {
        var copy = Instantiate(source, source.transform.parent);
        copy.name = name;
        copy.onClick = new Button.ButtonClickedEvent(); // drop anything wired to the original
        var text = copy.GetComponentInChildren<Text>(true);
        if (text != null) text.text = label;
        return copy;
    }
}
