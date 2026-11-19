using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a settings slider's value box typeable: click the number, type one, press Enter (or click away).
/// The number shown is the slider value times <see cref="scale"/> (100 for the percent rows), and a typed
/// number is clamped to the slider's range. The box keeps its own display text; a second, hidden text is
/// used while typing, so the menu can go on writing the display text without fighting the input field.
/// </summary>
public class SliderValueField : MonoBehaviour
{
    public Slider slider;
    public Text display;
    public float scale = 1f;

    InputField field;
    Text editText;
    float shownValue = float.NaN;

    static int lastEditFrame = -1;

    /// <summary>True while a value box is being typed in, and on the frame the typing ended - so the Escape that
    /// cancels an edit doesn't also close the settings panel.</summary>
    public static bool IsEditing { get { return Time.frameCount <= lastEditFrame + 1; } }

    public static SliderValueField Attach(Slider slider, Text display, float scale)
    {
        if (slider == null || display == null) return null;
        var box = display.transform.parent.gameObject;
        var f = box.GetComponent<SliderValueField>();
        if (f == null) f = box.AddComponent<SliderValueField>();
        f.slider = slider;
        f.display = display;
        f.scale = scale;
        f.Build();
        return f;
    }

    void Build()
    {
        if (field != null) return;

        var editGo = new GameObject("EditText", typeof(RectTransform), typeof(Text));
        editGo.transform.SetParent(transform, false);
        var rt = (RectTransform)editGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        editText = editGo.GetComponent<Text>();
        editText.font = display.font;
        editText.fontSize = display.fontSize;
        editText.color = display.color;
        editText.alignment = display.alignment;
        editText.supportRichText = false;
        editText.raycastTarget = false;
        editGo.SetActive(false);

        var box = GetComponent<Image>();
        box.raycastTarget = true;
        field = gameObject.AddComponent<InputField>();
        field.targetGraphic = box;
        field.textComponent = editText;
        field.contentType = InputField.ContentType.DecimalNumber;
        field.characterLimit = 6;
        field.caretColor = display.color;
        field.customCaretColor = true;
        field.selectionColor = new Color(display.color.r, display.color.g, display.color.b, 0.35f);
        var colors = field.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f); // the box lifts a little on hover so it reads as clickable
        colors.selectedColor = Color.white;
        field.colors = colors;

        field.onEndEdit.AddListener(Commit);
    }

    void Update()
    {
        if (field == null) return;
        bool editing = field.isFocused;
        if (editing) lastEditFrame = Time.frameCount;
        // Kept ready before the click: on focus the field selects all of it (the bare number, no % sign) to type over.
        else if (slider.value != shownValue) { shownValue = slider.value; field.text = Format(shownValue * scale); }
        if (editing == editText.gameObject.activeSelf) return;

        editText.gameObject.SetActive(editing);
        display.enabled = !editing;
    }

    void Commit(string typed)
    {
        lastEditFrame = Time.frameCount;
        float v;
        if (float.TryParse(typed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
        {
            // Two decimals as shown, so the box never reads back a different number from the one in effect.
            v = Mathf.Round(v * 100f) / 100f;
            v = Mathf.Clamp(v / scale, slider.minValue, slider.maxValue);
            if (slider.wholeNumbers) v = Mathf.Round(v);
            slider.value = v; // the slider's own listener applies it and rewrites the display text
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
        }
        editText.gameObject.SetActive(false);
        display.enabled = true;
    }

    static string Format(float v)
    {
        return Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
