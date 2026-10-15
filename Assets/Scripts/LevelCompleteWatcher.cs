using UnityEngine;

/// <summary>Polls the Room 6 Sentinels; when both are down, shows the level-complete panel.</summary>
public class LevelCompleteWatcher : MonoBehaviour
{
    public GameObject[] watchedSentinels;
    public GameObject completePanel;

    bool triggered;

    void Update()
    {
        if (triggered) return;
        if (watchedSentinels == null || watchedSentinels.Length == 0) return;

        for (int i = 0; i < watchedSentinels.Length; i++)
        {
            if (watchedSentinels[i] != null && watchedSentinels[i].activeSelf) return;
        }

        triggered = true;
        if (completePanel != null) completePanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
