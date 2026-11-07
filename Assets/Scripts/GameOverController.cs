using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the Game Over screen shown on every death, not just failing The Round - lecturer
/// feedback was that dying anywhere else was a free respawn with no real stakes. Offers a
/// checkpoint retry (Room 5) when one has been reached and checkpoints are enabled in
/// Settings > General; otherwise only a from-beginning restart. Reuses the old
/// "BREACH FAILED" round-lose panel, retitled MISSION FAILED since it's now general-purpose.
/// </summary>
public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance;

    public GameObject panel;
    public Text subText;
    public Button retryCheckpointButton;
    public Button restartBeginningButton;
    public Button restartButton;
    public Button mainMenuButton;
    public float firstButtonY = -110f;
    public float buttonSpacing = 60f;
    public AudioClip loseSound;

    public string defaultSubMessage = "The attempt ends here.";
    public string replaySubMessage = "Replaying a cleared room. Your progress is safe.";

    // Shown instead of the checkpoint buttons when the player dies replaying a room through Room Select.
    Button retryRoomButton;
    Button backToFurthestButton;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (retryCheckpointButton != null)
        {
            retryRoomButton = CloneButton(retryCheckpointButton, "RetryRoomButton", "RETRY ROOM");
            retryRoomButton.onClick.AddListener(RetryRoom);
            backToFurthestButton = CloneButton(retryCheckpointButton, "BackToFurthestButton", "BACK");
            backToFurthestButton.onClick.AddListener(BackToFurthestRoom);
        }
        if (retryCheckpointButton != null) retryCheckpointButton.onClick.AddListener(RetryFromCheckpoint);
        if (restartBeginningButton != null) restartBeginningButton.onClick.AddListener(RestartFromBeginning);
        if (restartButton != null) restartButton.onClick.AddListener(RestartFromBeginning);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(MainMenu);
    }

    /// <summary>
    /// Buttons used to sit at fixed heights authored for the 3-button (checkpoint) layout, so the
    /// 2-button layout left an empty slot between RESTART and MAIN MENU. Stack whichever are shown
    /// top-down from the first slot instead, in display order.
    /// </summary>
    void StackVisibleButtons()
    {
        Button[] order = { retryRoomButton, backToFurthestButton, retryCheckpointButton, restartBeginningButton, restartButton, mainMenuButton };
        float y = firstButtonY;
        for (int i = 0; i < order.Length; i++)
        {
            if (order[i] == null || !order[i].gameObject.activeSelf) continue;
            var rt = (RectTransform)order[i].transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            y -= buttonSpacing;
        }
    }

    public void ShowGameOver(string subMessage = null)
    {
        if (panel == null) return;

        // Set here, not in PlayerHealth.Die, because losing to a disarm can leave the player alive.
        var rm = RoomManager.Instance;
        var health = rm != null && rm.player != null ? rm.player.GetComponent<PlayerHealth>() : null;
        if (health != null) health.AttemptOver = true;

        bool hasCheckpoint = CheckpointManager.Instance != null && CheckpointManager.Instance.HasReachedCheckpoint;
        // A death while replaying a cleared room is practice - it doesn't cost the run.
        bool replaying = rm != null && rm.IsReplaying;

        if (retryRoomButton != null) retryRoomButton.gameObject.SetActive(replaying);
        if (backToFurthestButton != null)
        {
            backToFurthestButton.gameObject.SetActive(replaying);
            if (replaying) SetLabel(backToFurthestButton, "BACK TO ROOM " + (rm.FurthestRoom + 1));
        }
        if (retryCheckpointButton != null) retryCheckpointButton.gameObject.SetActive(hasCheckpoint && !replaying);
        if (restartBeginningButton != null) restartBeginningButton.gameObject.SetActive(hasCheckpoint && !replaying);
        if (restartButton != null) restartButton.gameObject.SetActive(!hasCheckpoint && !replaying);
        StackVisibleButtons();

        if (string.IsNullOrEmpty(subMessage)) subMessage = replaying ? replaySubMessage : defaultSubMessage;
        if (subText != null) subText.text = subMessage;

        panel.transform.SetAsLastSibling();
        panel.SetActive(true);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(loseSound);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    void RetryFromCheckpoint()
    {
        ResumePlay();
        if (CheckpointManager.Instance != null) CheckpointManager.Instance.RetryFromCheckpoint();
    }

    void RetryRoom()
    {
        ResumePlay();
        if (RoomManager.Instance != null) RoomManager.Instance.JumpToRoom(RoomManager.Instance.CurrentRoom);
    }

    void BackToFurthestRoom()
    {
        ResumePlay();
        if (RoomManager.Instance != null) RoomManager.Instance.JumpToRoom(RoomManager.Instance.FurthestRoom);
    }

    void ResumePlay()
    {
        panel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    static Button CloneButton(Button source, string name, string label)
    {
        var copy = Instantiate(source, source.transform.parent);
        copy.name = name;
        copy.onClick = new Button.ButtonClickedEvent(); // drop anything wired to the original
        SetLabel(copy, label);
        copy.gameObject.SetActive(false);
        return copy;
    }

    static void SetLabel(Button button, string label)
    {
        var text = button.GetComponentInChildren<Text>(true);
        if (text != null) text.text = label;
    }

    /// <summary>Public so the win screen's PLAY AGAIN button can reuse it.</summary>
    public void RestartFromBeginning()
    {
        // Full scene reload resets room/health/Sentinel/ability/ult/checkpoint state back to
        // authored defaults, same trick SettingsMenuController.QuitToMenu uses - but skip the
        // main menu screen this reload would otherwise show, since this button means "Room 1
        // gameplay now", not "back to the title screen".
        MainMenuController.SkipMainMenuOnNextLoad = true;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
