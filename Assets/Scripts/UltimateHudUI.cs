using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Segmented ring around the ultimate's icon, sitting at the end of the ability bar - segment
/// count is read from UltimateController.SegmentCount every frame it changes, so it can never
/// drift out of sync with the actual charge cost. Also owns the "+1" charge-gain popup, the
/// ready sound/banner, and the key-prompt swap on the tile's own KeyLabel.
/// </summary>
public class UltimateHudUI : MonoBehaviour
{
    public UltimateController controller;

    [Header("Ring")]
    public RectTransform ring;
    public Sprite segmentSprite;
    public Image icon;
    public Text keyLabel;
    public Text nameLabel;

    [Header("Ready banner (brief, shown once per charge-up)")]
    public GameObject readyBanner;
    public Text readyBannerText;

    [Header("+1 popup")]
    public RectTransform popupAnchor;
    public GameObject popupTemplate;

    [Header("Misc")]
    public Text descriptionLabel;

    public Color emptyColor = new Color(0.35f, 0.38f, 0.32f, 1f);
    public Color filledColor = new Color(0.25f, 0.75f, 0.95f, 1f);
    public Color readyColor = new Color(1f, 0.85f, 0.2f, 1f);
    public float pulseSpeed = 4f;
    public float pulseScaleAmount = 0.12f;
    [Range(0.4f, 0.95f)] public float segmentArcFraction = 0.78f;

    Image[] segments;
    int builtSegmentCount = -1;
    float prevCharge01;
    bool wasReady;
    Coroutine readyBannerRoutine;

    void Start()
    {
        if (descriptionLabel != null) descriptionLabel.text = "REVEALS ALL SENTINELS + REDUCES DAMAGE TAKEN";
        if (readyBanner != null) readyBanner.SetActive(false);
        if (popupTemplate != null) popupTemplate.SetActive(false);
        if (controller != null) prevCharge01 = controller.CurrentCharge01;
    }

    void Update()
    {
        if (controller == null || ring == null) return;

        int segmentCount = controller.SegmentCount;
        if (segments == null || builtSegmentCount != segmentCount) BuildSegments(segmentCount);

        float charge01 = controller.CurrentCharge01;
        bool ready = controller.IsReady;
        int litCount = Mathf.RoundToInt(charge01 * segments.Length);

        float pulse = ready ? 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount : 1f;
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null) continue;
            segments[i].color = ready ? readyColor : (i < litCount ? filledColor : emptyColor);
        }
        ring.localScale = Vector3.one * pulse;
        if (icon != null) icon.rectTransform.localScale = Vector3.one * (ready ? pulse : 1f);

        if (nameLabel != null) nameLabel.color = ready ? readyColor : Color.white;
        if (keyLabel != null)
        {
            keyLabel.text = ready ? "[X]" : "X";
            keyLabel.color = ready ? readyColor : Color.white;
        }

        // Charge gained this frame (kill or orb) -> "+1" popup. The drop back to 0 on activation
        // is a decrease, not a gain, so it's naturally excluded by the comparison below.
        if (charge01 > prevCharge01 + 0.0001f)
        {
            SpawnPopup();
        }

        // Rising edge into "ready" -> sound + brief banner, once per charge-up, not every frame while ready.
        if (ready && !wasReady)
        {
            if (AudioManager.Instance != null && controller.readySound != null) AudioManager.Instance.PlaySFX(controller.readySound);
            ShowReadyBanner();
        }

        prevCharge01 = charge01;
        wasReady = ready;
    }

    void BuildSegments(int count)
    {
        if (segments != null)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] != null) Destroy(segments[i].gameObject);
            }
        }

        segments = new Image[count];
        float arcWidth = (360f / count) * segmentArcFraction;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Segment_" + i, typeof(RectTransform));
            go.transform.SetParent(ring, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = ring.sizeDelta;
            rt.localRotation = Quaternion.Euler(0f, 0f, -(360f / count) * i);

            var img = go.AddComponent<Image>();
            img.sprite = segmentSprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.fillAmount = arcWidth / 360f;
            img.color = emptyColor;
            img.raycastTarget = false;

            segments[i] = img;
        }

        builtSegmentCount = count;
    }

    void SpawnPopup()
    {
        if (popupTemplate == null || popupAnchor == null) return;
        var go = Instantiate(popupTemplate, popupAnchor);
        go.SetActive(true);
        StartCoroutine(RunPopup(go));
    }

    IEnumerator RunPopup(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        var text = go.GetComponent<Text>();
        Color baseColor = text != null ? text.color : Color.white;
        const float duration = 0.8f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float frac = t / duration;
            if (rt != null) rt.anchoredPosition = Vector2.up * (24f * frac);
            if (text != null)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(1f, 0f, frac);
                text.color = c;
            }
            yield return null;
        }
        Destroy(go);
    }

    void ShowReadyBanner()
    {
        if (readyBannerRoutine != null) StopCoroutine(readyBannerRoutine);
        readyBannerRoutine = StartCoroutine(RunReadyBanner());
    }

    IEnumerator RunReadyBanner()
    {
        if (readyBanner == null) yield break;
        if (readyBannerText != null) readyBannerText.text = "OVERWATCH PROTOCOL READY - PRESS [" + controller.activateKeyDisplay + "]";
        readyBanner.SetActive(true);
        yield return new WaitForSeconds(2.2f);
        readyBanner.SetActive(false);
        readyBannerRoutine = null;
    }
}
