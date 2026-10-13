using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class AutoHidePanel : MonoBehaviour
{
    public float visibleSeconds = 6f;
    public float fadeSeconds = 1f;

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        group.alpha = 1f;
        StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleSeconds);

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }

        group.alpha = 0f;
        gameObject.SetActive(false);
    }
}
