using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>Short-lived kill notifications, stacking top-to-bottom, auto-expiring like AutoHidePanel.</summary>
public class KillFeedUI : MonoBehaviour
{
    public static KillFeedUI Instance;

    public RectTransform container;
    public float visibleSeconds = 3f;
    public float fadeSeconds = 0.5f;
    public int maxEntries = 4;
    public Color headshotColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    public Color normalColor = new Color(0.85f, 0.85f, 0.82f, 1f);

    void Awake()
    {
        Instance = this;
    }

    public void AddKill(bool headshot)
    {
        string text = headshot ? "SENTINEL ELIMINATED - HEADSHOT" : "SENTINEL ELIMINATED";
        Color color = headshot ? headshotColor : normalColor;
        SpawnEntry(text, color);
    }

    void SpawnEntry(string text, Color color)
    {
        if (container == null) return;
        if (container.childCount >= maxEntries)
        {
            Destroy(container.GetChild(0).gameObject);
        }

        var go = new GameObject("KillFeedEntry", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(LayoutElement), typeof(CanvasGroup));
        go.transform.SetParent(container, false);

        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleRight;
        txt.color = color;

        var layout = go.GetComponent<LayoutElement>();
        layout.minHeight = 22;
        layout.preferredHeight = 22;

        StartCoroutine(FadeAndDestroy(go));
    }

    IEnumerator FadeAndDestroy(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 1f;
        yield return new WaitForSeconds(visibleSeconds);

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            if (cg != null) cg.alpha = 1f - Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
