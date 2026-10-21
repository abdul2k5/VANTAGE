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
    public AudioClip loseSound;

    public string defaultSubMessage = "The attempt ends here.";

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (retryCheckpointButton != null) retryCheckpointButton.onClick.AddListener(RetryFromCheckpoint);
        if (restartBeginningButton != null) restartBeginningButton.onClick.AddListener(RestartFromBeginning);
        if (restartButton != null) restartButton.onClick.AddListener(RestartFromBeginning);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(MainMenu);
    }

    public void ShowGameOver(string subMessage = null)
    {
        if (panel == null) return;

        bool hasCheckpoint = CheckpointManager.Instance != null && CheckpointManager.Instance.HasReachedCheckpoint;

        if (retryCheckpointButton != null) retryCheckpointButton.gameObject.SetActive(hasCheckpoint);
        if (restartBeginningButton != null) restartBeginningButton.gameObject.SetActive(hasCheckpoint);
        if (restartButton != null) restartButton.gameObject.SetActive(!hasCheckpoint);

        if (subText != null) subText.text = string.IsNullOrEmpty(subMessage) ? defaultSubMessage : subMessage;

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
        panel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (CheckpointManager.Instance != null) CheckpointManager.Instance.RetryFromCheckpoint();
    }

    void RestartFromBeginning()
    {
        // Full scene reload resets room/health/Sentinel/ability/ult/checkpoint state back to
        // authored defaults, same trick SettingsMenuController.QuitToMenu uses - but skip the
        // main menu screen this reload would otherwise show, since this button means "Room 1
        // gameplay now", not "back to the title screen".
        MainMenuController.SkipMainMenuOnNextLoad = true;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void MainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
