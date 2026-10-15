using UnityEngine;
using UnityEngine.UI;

/// <summary>Charge pips for the ultimate - lights up left to right as charge builds, pulses when ready.</summary>
public class UltimateHudUI : MonoBehaviour
{
    public UltimateController controller;
    public Image[] pips;
    public Image icon;
    public Text label;
    public Text descriptionLabel;

    public Color emptyColor = new Color(0.35f, 0.38f, 0.32f, 1f);
    public Color filledColor = new Color(0.25f, 0.75f, 0.95f, 1f);
    public Color readyColor = new Color(1f, 0.85f, 0.2f, 1f);
    public float pulseSpeed = 4f;
    public float pulseScaleAmount = 0.12f;

    void Start()
    {
        if (descriptionLabel != null)
        {
            descriptionLabel.text = "REVEALS ALL SENTINELS + REDUCES DAMAGE TAKEN";
        }
    }

    void Update()
    {
        if (controller == null || pips == null) return;

        float charge01 = controller.CurrentCharge01;
        int litCount = Mathf.RoundToInt(charge01 * pips.Length);
        bool ready = controller.IsReady;
        float pulse = ready ? 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount : 1f;

        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            pips[i].color = ready ? readyColor : (i < litCount ? filledColor : emptyColor);
            pips[i].rectTransform.localScale = new Vector3(1f, pulse, 1f);
        }

        if (label != null)
        {
            label.text = ready ? "OVERWATCH PROTOCOL READY - PRESS X" : "OVERWATCH PROTOCOL";
            label.color = ready ? readyColor : new Color(0.85f, 0.85f, 0.82f, 1f);
        }
    }
}
