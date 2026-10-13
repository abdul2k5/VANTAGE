using UnityEngine;
using UnityEngine.InputSystem;

public class CrosshairController : MonoBehaviour
{
    public static CrosshairController Instance;

    public RectTransform[] styleRoots;
    public RectTransform canvasRect;

    int currentStyle;
    bool crosshairVisible = true;

    void Awake()
    {
        Instance = this;
        Cursor.visible = false;
        currentStyle = PlayerPrefs.GetInt("VantageCrosshairStyle", 0);
        ApplyStyle();
    }

    public void SetStyle(int index)
    {
        if (index < 0 || index >= styleRoots.Length) return;
        currentStyle = index;
        PlayerPrefs.SetInt("VantageCrosshairStyle", currentStyle);
        ApplyStyle();
    }

public void SetCrosshairVisible(bool visible)
    {
        crosshairVisible = visible;
        if (!visible)
        {
            for (int i = 0; i < styleRoots.Length; i++)
            {
                styleRoots[i].gameObject.SetActive(false);
            }
        }
        else
        {
            ApplyStyle();
        }
    }


void ApplyStyle()
    {
        for (int i = 0; i < styleRoots.Length; i++)
        {
            styleRoots[i].gameObject.SetActive(crosshairVisible && i == currentStyle);
        }
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || canvasRect == null) return;

        Vector2 screenPos = mouse.position.ReadValue();
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPoint))
        {
            for (int i = 0; i < styleRoots.Length; i++)
            {
                if (styleRoots[i].gameObject.activeSelf)
                {
                    styleRoots[i].anchoredPosition = localPoint;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Cursor.visible = true;
    }
}
