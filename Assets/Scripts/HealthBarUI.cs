using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerHealth health;
    public Image[] segments;
    public Color filledColor = new Color(0.886f, 0.647f, 0.235f, 1f);
    public Color emptyColor = new Color(0.141f, 0.173f, 0.102f, 1f);

    void Update()
    {
        if (health == null || segments == null) return;

        float fraction = health.CurrentHealth / health.MaxHealth;
        int filledCount = Mathf.CeilToInt(fraction * segments.Length);

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].color = i < filledCount ? filledColor : emptyColor;
        }
    }
}
