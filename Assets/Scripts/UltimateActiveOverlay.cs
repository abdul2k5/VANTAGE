using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes the ultimate's effect impossible to miss: a bright flash the instant it activates, then
/// a steady screen tint + "OVERWATCH ACTIVE" countdown for its whole duration, since the actual
/// effect (reveal + damage reduction) has no guaranteed on-screen presence by itself.
/// </summary>
public class UltimateActiveOverlay : MonoBehaviour
{
    public static UltimateActiveOverlay Instance;

    public Image flashImage;
    public Image tintImage;
    public Text activeLabel;

    public Color flashColor = new Color(0.4f, 0.85f, 1f, 0.55f);
    public Color tintColor = new Color(0.25f, 0.6f, 0.75f, 0.1f);
    public float flashSeconds = 0.25f;

    Coroutine routine;

    void Awake()
    {
        Instance = this;
    }

    public void Play(float duration)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(RunOverlay(duration));
    }

    IEnumerator RunOverlay(float duration)
    {
        if (tintImage != null) tintImage.color = tintColor;
        if (activeLabel != null) activeLabel.gameObject.SetActive(true);

        // Sharp flash so the moment of activation reads unmistakably, even with no Sentinels on screen.
        if (flashImage != null)
        {
            float t = 0f;
            while (t < flashSeconds)
            {
                t += Time.deltaTime;
                Color c = flashColor;
                c.a = Mathf.Lerp(flashColor.a, 0f, t / flashSeconds);
                flashImage.color = c;
                yield return null;
            }
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        }

        float remaining = duration;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            if (activeLabel != null) activeLabel.text = "OVERWATCH ACTIVE - " + Mathf.CeilToInt(Mathf.Max(remaining, 0f)) + "s";
            yield return null;
        }

        if (tintImage != null) tintImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, 0f);
        if (activeLabel != null) activeLabel.gameObject.SetActive(false);
        routine = null;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
