using System.Collections;
using UnityEngine;

public class WeaponFeedback : MonoBehaviour
{
    public float tracerSeconds = 0.05f;
    public Color tracerColor = new Color(1f, 0.85f, 0.45f, 0.95f);
    public Color flashColor = new Color(1f, 0.92f, 0.6f, 1f);

    LineRenderer tracer;
    SpriteRenderer muzzleFlash;
    Coroutine activeRoutine;

    void Awake()
    {
        var tracerGo = new GameObject("Tracer");
        tracerGo.transform.SetParent(transform, false);
        tracer = tracerGo.AddComponent<LineRenderer>();
        tracer.useWorldSpace = true;
        tracer.positionCount = 2;
        tracer.startWidth = 0.06f;
        tracer.endWidth = 0.02f;
        tracer.material = new Material(Shader.Find("Sprites/Default"));
        tracer.startColor = tracerColor;
        tracer.endColor = new Color(tracerColor.r, tracerColor.g, tracerColor.b, 0f);
        tracer.sortingOrder = 20;
        tracer.enabled = false;

        var flashGo = new GameObject("MuzzleFlash");
        flashGo.transform.SetParent(transform, false);
        flashGo.transform.localPosition = new Vector3(0.55f, 0f, 0f);
        muzzleFlash = flashGo.AddComponent<SpriteRenderer>();
        muzzleFlash.sprite = MakeDotSprite();
        muzzleFlash.color = flashColor;
        muzzleFlash.sortingOrder = 21;
        flashGo.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        muzzleFlash.enabled = false;
    }

    static Sprite MakeDotSprite()
    {
        var tex = new Texture2D(2, 2);
        var pixels = new Color[4] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
    }

    public void PlayShot(Vector3 origin, Vector3 end)
    {
        tracer.SetPosition(0, origin);
        tracer.SetPosition(1, end);

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(ShowThenHide());
    }

    IEnumerator ShowThenHide()
    {
        tracer.enabled = true;
        muzzleFlash.enabled = true;
        yield return new WaitForSeconds(tracerSeconds);
        tracer.enabled = false;
        muzzleFlash.enabled = false;
        activeRoutine = null;
    }
}
