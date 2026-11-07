using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The ROOMS tab of the pause menu. Lists every room: ones already cleared and the furthest one
/// reached can be jumped to (reset to how they were first found), rooms beyond that stay locked
/// until the player clears the furthest room the normal way. Rows are cloned from the Restart Room
/// button at runtime, so they match the menu's existing button style.
/// </summary>
public class RoomSelectMenu : MonoBehaviour
{
    public RoomManager roomManager;
    public SettingsMenuController settings;
    public Button rowTemplate;
    public float firstRowY = -88f;
    public float rowSpacing = 40f;
    public Vector2 rowSize = new Vector2(460f, 34f);
    public Color clearedColor = new Color(0.85f, 0.85f, 0.82f, 1f);
    public Color inProgressColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    public Color lockedColor = new Color(0.45f, 0.45f, 0.43f, 1f);

    Button[] rows;
    Text[] names;
    Text[] statuses;

    void OnEnable()
    {
        if (rows == null) Build();
        Refresh();
    }

    void Build()
    {
        if (roomManager == null || rowTemplate == null) return;

        int count = roomManager.rooms.Length;
        rows = new Button[count];
        names = new Text[count];
        statuses = new Text[count];

        for (int i = 0; i < count; i++)
        {
            var row = Instantiate(rowTemplate, transform);
            row.name = "RoomRow_" + (i + 1);
            row.onClick = new Button.ButtonClickedEvent(); // drop Restart Room's own wiring
            int room = i;
            row.onClick.AddListener(() => Pick(room));

            var rt = (RectTransform)row.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = rowSize;
            rt.anchoredPosition = new Vector2(0f, firstRowY - i * rowSpacing);

            var name = row.GetComponentInChildren<Text>(true);
            name.alignment = TextAnchor.MiddleLeft;
            Stretch(name.rectTransform, 18f, -18f);

            var status = Instantiate(name, name.transform.parent);
            status.name = "Status";
            status.alignment = TextAnchor.MiddleRight;
            status.fontSize = Mathf.Max(10, name.fontSize - 2);

            rows[i] = row;
            names[i] = name;
            statuses[i] = status;
        }
    }

    static void Stretch(RectTransform rt, float left, float right)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, 0f);
        rt.offsetMax = new Vector2(right, 0f);
    }

    void Refresh()
    {
        if (rows == null) return;

        int furthest = roomManager.FurthestRoom;
        int current = roomManager.CurrentRoom;
        for (int i = 0; i < rows.Length; i++)
        {
            bool open = i <= furthest;
            string status;
            if (i > furthest) status = "LOCKED";
            else if (i == furthest) status = i == current ? "IN PROGRESS  (RESTART)" : "IN PROGRESS";
            else status = i == current ? "REPLAYING  (RESTART)" : "CLEARED";

            Color color = i > furthest ? lockedColor : (i == furthest ? inProgressColor : clearedColor);
            rows[i].interactable = open;
            names[i].text = (i + 1) + "   " + roomManager.rooms[i].title.ToUpper();
            names[i].color = color;
            statuses[i].text = status;
            statuses[i].color = color;
        }
    }

    void Pick(int room)
    {
        if (roomManager == null || room > roomManager.FurthestRoom) return;
        if (settings != null) settings.Close(); // first - the jump's fade runs on scaled time
        roomManager.JumpToRoom(room);
    }
}
