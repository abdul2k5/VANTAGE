using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class RoomDefinition
{
    public string title = "READ THE LINE";
    public Transform entryPoint;
    public GameObject introPanel;
}

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public RoomDefinition[] rooms;
    public Transform player;
    public Image fadeOverlay;
    public Text roomTitleLabel;
    public FollowCamera followCamera;
    public AbilityController abilities;
    public float fadeSeconds = 0.35f;
    public float holdSeconds = 0.45f;

    int currentRoom = -1;
    bool transitioning;

    public int CurrentRoom { get { return currentRoom; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnterRoom(0, false);
    }

    public void GoToNextRoom()
    {
        if (transitioning) return;
        int next = currentRoom + 1;
        if (next >= rooms.Length) return;
        StartCoroutine(TransitionTo(next));
    }

public void EnterRoom(int index, bool movePlayer = true)
    {
        if (index < 0 || index >= rooms.Length) return;

        currentRoom = index;
        var room = rooms[index];

        if (movePlayer && player != null && room.entryPoint != null)
        {
            player.position = room.entryPoint.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.SetRespawnPoint(room.entryPoint.position);
        }

        // The camera must not smooth-slide across the gap between rooms.
        if (followCamera != null) followCamera.SnapToTarget();

        if (abilities != null)
        {
            abilities.UnlockForRoom(currentRoom);
            abilities.ResetAllCooldowns();
        }

        ShowOnlyCurrentPanel();
        UpdateTitle();
    }

    void ShowOnlyCurrentPanel()
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].introPanel == null) continue;
            rooms[i].introPanel.SetActive(i == currentRoom);
        }
    }

    void UpdateTitle()
    {
        if (roomTitleLabel == null) return;
        var room = rooms[currentRoom];
        roomTitleLabel.text = "ROOM " + (currentRoom + 1) + " / " + rooms.Length + "   " + room.title.ToUpper();
    }

    IEnumerator TransitionTo(int index)
    {
        transitioning = true;

        yield return Fade(0f, 1f);

        EnterRoom(index);
        yield return new WaitForSeconds(holdSeconds);

        yield return Fade(1f, 0f);

        transitioning = false;
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeOverlay == null) yield break;

        float t = 0f;
        Color c = fadeOverlay.color;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeSeconds));
            fadeOverlay.color = c;
            yield return null;
        }
        c.a = to;
        fadeOverlay.color = c;
    }
}
