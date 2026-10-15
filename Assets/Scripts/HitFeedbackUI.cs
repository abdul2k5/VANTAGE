using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Hit marker (confirms a shot landed) and a screen-edge indicator pointing at incoming damage.</summary>
public class HitFeedbackUI : MonoBehaviour
{
    public static HitFeedbackUI Instance;

    [Header("Hit marker")]
    public CanvasGroup hitMarkerGroup;
    public Image[] hitMarkerBars;
    public Color normalHitColor = new Color(0.95f, 0.95f, 0.9f, 1f);
    public Color headshotColor = new Color(0.9f, 0.15f, 0.1f, 1f);
    public float hitMarkerSeconds = 0.18f;
    public float headshotMarkerScale = 1.4f;

    [Header("Damage direction")]
    public RectTransform damageIndicator;
    public CanvasGroup damageIndicatorGroup;
    public float damageIndicatorRadius = 160f;
    public float damageIndicatorSeconds = 0.8f;

    Transform playerRoot;
    Coroutine hitMarkerRoutine;
    Coroutine damageRoutine;

    void Awake()
    {
        Instance = this;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerRoot = player.transform;
    }

    public void ShowHitMarker(bool headshot)
    {
        if (hitMarkerGroup == null) return;

        Color c = headshot ? headshotColor : normalHitColor;
        if (hitMarkerBars != null)
        {
            foreach (var bar in hitMarkerBars) { if (bar != null) bar.color = c; }
        }
        var rt = hitMarkerGroup.GetComponent<RectTransform>();
        if (rt != null) rt.localScale = Vector3.one * (headshot ? headshotMarkerScale : 1f);

        if (hitMarkerRoutine != null) StopCoroutine(hitMarkerRoutine);
        hitMarkerRoutine = StartCoroutine(FlashThenHide(hitMarkerGroup, hitMarkerSeconds));
    }

    public void ShowDamageDirection(Vector3 sourceWorldPosition)
    {
        if (damageIndicator == null || playerRoot == null) return;

        Vector3 toSource = sourceWorldPosition - playerRoot.position;
        toSource.y = 0f;
        if (toSource.sqrMagnitude < 0.0001f) return;

        float signedAngle = Vector3.SignedAngle(playerRoot.forward, toSource.normalized, Vector3.up);
        // Screen space: rotate clockwise for a threat to the player's right.
        damageIndicator.localRotation = Quaternion.Euler(0f, 0f, -signedAngle);

        if (damageRoutine != null) StopCoroutine(damageRoutine);
        damageRoutine = StartCoroutine(FadeOut(damageIndicatorGroup, damageIndicatorSeconds));
    }

    IEnumerator FlashThenHide(CanvasGroup group, float seconds)
    {
        group.alpha = 1f;
        yield return new WaitForSeconds(seconds);
        group.alpha = 0f;
    }

    IEnumerator FadeOut(CanvasGroup group, float seconds)
    {
        group.alpha = 1f;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(1f, 0f, t / seconds);
            yield return null;
        }
        group.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
