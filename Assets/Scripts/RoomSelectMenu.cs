using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The ROOMS tab of the pause menu: a 4x2 grid of room cards. Rooms already cleared and the furthest
/// one reached can be jumped to (reset to how they were first found); rooms beyond that stay locked
/// until the player clears the furthest room the normal way. Cards are built in code on first open.
/// </summary>
public class RoomSelectMenu : MonoBehaviour
{
    public RoomManager roomManager;
    public SettingsMenuController settings;
    public RectTransform grid;
    public Font font;

    public Color cardColor = new Color(0.12f, 0.13f, 0.12f, 1f);
    public Color lockedCardColor = new Color(0.085f, 0.09f, 0.085f, 1f);
    public Color clearedColor = new Color(0.85f, 0.85f, 0.82f, 1f);
    public Color inProgressColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    public Color lockedColor = new Color(0.4f, 0.4f, 0.38f, 1f);
    public Color clearedBarColor = new Color(0.45f, 0.75f, 0.5f, 1f);

    class Card
    {
        public Button button;
        public Image background;
        public Outline outline;
        public Text number;
        public Text title;
        public Text status;
        public Image bar;
        public GameObject lockIcon;
        public SettingsHoverHint hint;
    }

    Card[] cards;

    void OnEnable()
    {
        if (cards == null) Build();
        Refresh();
    }

    void Build()
    {
        if (roomManager == null || grid == null) return;

        int count = roomManager.rooms.Length;
        cards = new Card[count];
        for (int i = 0; i < count; i++)
        {
            int room = i;
            var card = new Card();

            var go = new GameObject("RoomCard_" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(SettingsHoverHint));
            go.transform.SetParent(grid, false);
            card.background = go.GetComponent<Image>();
            card.button = go.GetComponent<Button>();
            card.button.targetGraphic = card.background;
            var colors = card.button.colors;
            colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.disabledColor = Color.white; // locked cards are styled directly
            card.button.colors = colors;
            card.button.onClick.AddListener(() => Pick(room));
            card.outline = go.GetComponent<Outline>();
            card.outline.effectColor = inProgressColor;
            card.outline.effectDistance = new Vector2(2f, -2f);
            card.hint = go.GetComponent<SettingsHoverHint>();

            card.number = MakeText(go.transform, "Number", 34, TextAnchor.UpperLeft, new Vector2(16f, -10f), new Vector2(100f, 44f));
            card.number.text = (i + 1).ToString("00");
            card.title = MakeText(go.transform, "Name", 15, TextAnchor.UpperLeft, new Vector2(16f, -58f), new Vector2(140f, 40f));
            card.title.text = roomManager.rooms[i].title.ToUpper();
            card.status = MakeText(go.transform, "Status", 11, TextAnchor.LowerLeft, new Vector2(16f, 0f), new Vector2(140f, 18f));
            var statusRt = card.status.rectTransform;
            statusRt.anchorMin = statusRt.anchorMax = statusRt.pivot = Vector2.zero;
            statusRt.anchoredPosition = new Vector2(16f, 14f);

            var bar = new GameObject("StatusBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(go.transform, false);
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = Vector2.zero;
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.sizeDelta = new Vector2(0f, 4f);
            card.bar = bar.GetComponent<Image>();
            card.bar.raycastTarget = false;

            card.lockIcon = BuildLock(go.transform);

            cards[i] = card;
        }
    }

    Text MakeText(Transform parent, string name, int size, TextAnchor alignment, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>A small padlock drawn from three bars and a block, top-right of a locked card.</summary>
    GameObject BuildLock(Transform parent)
    {
        var root = new GameObject("Lock", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-16f, -16f);
        rt.sizeDelta = new Vector2(16f, 20f);
        LockPart(root.transform, new Vector2(0f, 0f), new Vector2(16f, 11f));   // body
        LockPart(root.transform, new Vector2(2f, 11f), new Vector2(2f, 7f));    // shackle left
        LockPart(root.transform, new Vector2(12f, 11f), new Vector2(2f, 7f));   // shackle right
        LockPart(root.transform, new Vector2(2f, 18f), new Vector2(12f, 2f));   // shackle top
        return root;
    }

    void LockPart(Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Part", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = lockedColor;
        img.raycastTarget = false;
    }

    void Refresh()
    {
        if (cards == null) return;

        int furthest = roomManager.FurthestRoom;
        int current = roomManager.CurrentRoom;
        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i];
            bool locked = i > furthest;
            bool inProgress = i == furthest;
            bool here = i == current;

            string status;
            if (locked) status = "LOCKED";
            else if (inProgress) status = here ? "IN PROGRESS · RESTART" : "IN PROGRESS";
            else status = here ? "REPLAYING · RESTART" : "CLEARED";

            Color text = locked ? lockedColor : (inProgress ? inProgressColor : clearedColor);
            card.button.interactable = !locked;
            card.background.color = locked ? lockedCardColor : cardColor;
            card.outline.enabled = here;
            card.number.color = locked ? lockedColor : (inProgress ? inProgressColor : clearedColor);
            card.title.color = locked ? lockedColor : clearedColor;
            card.status.color = text;
            card.status.text = status;
            card.bar.color = locked ? new Color(1f, 1f, 1f, 0.06f) : (inProgress ? inProgressColor : clearedBarColor);
            card.lockIcon.SetActive(locked);

            if (locked) card.hint.text = "Clear Room " + (furthest + 1) + " to unlock the rooms after it.";
            else if (here) card.hint.text = "Restart this room from its entrance. It resets to how you first found it.";
            else if (inProgress) card.hint.text = "Go back to the room you're up to, with the health and Overwatch charge you arrived with.";
            else card.hint.text = "Replay this room. It resets when you enter, and your progress is kept.";
        }
    }

    void Pick(int room)
    {
        if (roomManager == null || room > roomManager.FurthestRoom) return;
        if (settings != null) settings.Close(); // first - the jump's fade runs on scaled time
        roomManager.JumpToRoom(room);
    }
}
