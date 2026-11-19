using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Second page of the win screen. The run debrief comes first with a CONTINUE button; that opens the
/// credits (who made the game, and the music, sounds and models it uses), where PLAY AGAIN and MAIN MENU
/// now sit. The text itself is GameCredits, shared with the main menu's CREDITS page.
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
    [Tooltip("Height of the BACK TO RESULTS button, under MAIN MENU.")]
    public float backButtonY = -423f;
    [Tooltip("Top edge of the credits block, and its size. It must end above PLAY AGAIN.")]
    public float creditsTopY = 346f;
    public Vector2 creditsSize = new Vector2(1485f, 540f);

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
            rt.anchoredPosition = new Vector2(0f, creditsTopY);
            rt.sizeDelta = creditsSize;
            creditsText.supportRichText = true;
            creditsText.alignment = TextAnchor.UpperCenter;
            creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsText.verticalOverflow = VerticalWrapMode.Overflow;
            creditsText.lineSpacing = 1.1f;
            creditsText.raycastTarget = false;
            creditsText.text = GameCredits.Build(creditsText.fontSize);
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
