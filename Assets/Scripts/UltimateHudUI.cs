using UnityEngine;
using UnityEngine.UI;

/// <summary>Charge pips for the ultimate - lights up left to right as charge builds, all lit + highlighted when ready.</summary>
public class UltimateHudUI : MonoBehaviour
{
    public UltimateController controller;
    public Image[] pips;
    public Text label;

    public Color emptyColor = new Color(0.35f, 0.38f, 0.32f, 1f);
    public Color filledColor = new Color(0.25f, 0.75f, 0.95f, 1f);
    public Color readyColor = new Color(1f, 0.85f, 0.2f, 1f);

    void Update()
    {
        if (controller == null || pips == null) return;

        float charge01 = controller.CurrentCharge01;
        int litCount = Mathf.RoundToInt(charge01 * pips.Length);
        bool ready = controller.IsReady;

        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            if (ready) pips[i].color = readyColor;
            else pips[i].color = i < litCount ? filledColor : emptyColor;
        }

        if (label != null)
        {
            label.text = ready ? "OVERWATCH PROTOCOL READY - X" : "OVERWATCH PROTOCOL";
            label.color = ready ? readyColor : new Color(0.85f, 0.85f, 0.82f, 1f);
        }
    }
}
