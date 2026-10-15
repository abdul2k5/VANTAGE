using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the post-plant phase: Sentinels push the site in waves, a countdown ticks to
/// detonation, and the player has to survive it. Dying during an active round is a loss,
/// not a normal respawn - PlayerHealth checks IsRoundActive before respawning.
/// </summary>
public class RoundController : MonoBehaviour
{
    public static RoundController Instance;

    [Header("Timer")]
    public float detonationSeconds = 30f;
    public Text timerLabel;

    [Header("Defenders (push toward the site in waves once planted)")]
    public Sentinel[] siteDefenders;
    public float[] waveDelaysSeconds = { 0f, 10f, 20f };

    [Header("Audio - accelerating beep as detonation nears, hook only until Phase 6")]
    public AudioClip beepSound;
    public float beepIntervalStart = 1.5f;
    public float beepIntervalEnd = 0.2f;

    [Header("Win/Lose")]
    public GameObject winPanel;
    public GameObject losePanel;

    bool roundActive;
    AudioSource audioSource;

    public bool IsRoundActive { get { return roundActive; } }

    void Awake()
    {
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void BeginRound(Vector3 sitePosition)
    {
        if (roundActive) return;
        roundActive = true;
        StartCoroutine(RunWaves(sitePosition));
        StartCoroutine(RunTimer());
    }

    IEnumerator RunWaves(Vector3 sitePosition)
    {
        if (siteDefenders == null || siteDefenders.Length == 0) yield break;

        int waveCount = waveDelaysSeconds != null && waveDelaysSeconds.Length > 0 ? waveDelaysSeconds.Length : 1;
        var waves = new List<List<Sentinel>>();
        for (int i = 0; i < waveCount; i++) waves.Add(new List<Sentinel>());
        for (int i = 0; i < siteDefenders.Length; i++)
        {
            waves[i % waveCount].Add(siteDefenders[i]);
        }

        for (int w = 0; w < waveCount; w++)
        {
            float delay = waveDelaysSeconds != null && w < waveDelaysSeconds.Length ? waveDelaysSeconds[w] : 0f;
            yield return new WaitForSeconds(delay - (w > 0 ? waveDelaysSeconds[w - 1] : 0f));
            if (!roundActive) yield break;

            foreach (var s in waves[w])
            {
                if (s != null && s.gameObject.activeSelf) s.PushTowards(sitePosition);
            }
        }
    }

    IEnumerator RunTimer()
    {
        float remaining = detonationSeconds;
        float beepTimer = 0f;

        while (remaining > 0f && roundActive)
        {
            remaining -= Time.deltaTime;
            float elapsedFrac = 1f - Mathf.Clamp01(remaining / detonationSeconds);
            float beepInterval = Mathf.Lerp(beepIntervalStart, beepIntervalEnd, elapsedFrac);

            beepTimer -= Time.deltaTime;
            if (beepTimer <= 0f)
            {
                if (beepSound != null && audioSource != null) audioSource.PlayOneShot(beepSound);
                beepTimer = beepInterval;
            }

            if (timerLabel != null)
            {
                timerLabel.gameObject.SetActive(true);
                timerLabel.text = "BREACH CHARGE: " + Mathf.CeilToInt(Mathf.Max(remaining, 0f)) + "s";
            }

            yield return null;
        }

        if (roundActive)
        {
            roundActive = false;
            ShowWin();
        }
    }

    public void OnPlayerDied()
    {
        if (!roundActive) return;
        roundActive = false;
        StopAllCoroutines();
        ShowLose();
    }

    void ShowWin()
    {
        if (timerLabel != null) timerLabel.gameObject.SetActive(false);
        if (winPanel != null) winPanel.transform.SetAsLastSibling();
        if (winPanel != null) winPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void ShowLose()
    {
        if (timerLabel != null) timerLabel.gameObject.SetActive(false);
        if (losePanel != null) losePanel.transform.SetAsLastSibling();
        if (losePanel != null) losePanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
