using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space markers for Recon-revealed Sentinels - a UI overlay reads through walls
/// without needing a custom shader, same trick as the hit marker/damage indicator.
/// </summary>
public class RevealMarkerUI : MonoBehaviour
{
    public RectTransform canvasRect;
    public Color markerColor = new Color(0.25f, 0.75f, 0.95f, 0.95f);

    Camera cam;
    readonly Dictionary<Sentinel, RectTransform> activeMarkers = new Dictionary<Sentinel, RectTransform>();

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (cam == null || canvasRect == null) return;

        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var seen = new HashSet<Sentinel>();

        foreach (var s in sentinels)
        {
            if (!s.IsRevealed) continue;
            seen.Add(s);

            RectTransform marker;
            if (!activeMarkers.TryGetValue(s, out marker))
            {
                marker = BuildMarker();
                activeMarkers[s] = marker;
            }

            Vector3 headPos = s.transform.position + Vector3.up * 1.7f;
            Vector3 viewPos = cam.WorldToViewportPoint(headPos);
            bool behindCamera = viewPos.z < 0f;
            marker.gameObject.SetActive(!behindCamera);
            if (behindCamera) continue;

            Vector2 screenFraction = new Vector2(Mathf.Clamp01(viewPos.x), Mathf.Clamp01(viewPos.y));
            Vector2 anchored = new Vector2(
                (screenFraction.x - 0.5f) * canvasRect.sizeDelta.x,
                (screenFraction.y - 0.5f) * canvasRect.sizeDelta.y);
            marker.anchoredPosition = anchored;
        }

        // Clean up markers for Sentinels no longer revealed.
        var toRemove = new List<Sentinel>();
        foreach (var kv in activeMarkers)
        {
            if (!seen.Contains(kv.Key))
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
                toRemove.Add(kv.Key);
            }
        }
        foreach (var s in toRemove) activeMarkers.Remove(s);
    }

    RectTransform BuildMarker()
    {
        var go = new GameObject("RevealMarker", typeof(RectTransform));
        go.transform.SetParent(canvasRect, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(18, 18);

        AddDiamondBar(go.transform, 45f);
        AddDiamondBar(go.transform, -45f);

        return rt;
    }

    void AddDiamondBar(Transform parent, float rotation)
    {
        var bar = new GameObject("Bar", typeof(RectTransform));
        bar.transform.SetParent(parent, false);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(16, 2.5f);
        rt.localRotation = Quaternion.Euler(0, 0, rotation);
        var img = bar.AddComponent<Image>();
        img.color = markerColor;
    }

    void OnDestroy()
    {
        foreach (var kv in activeMarkers)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
    }
}
