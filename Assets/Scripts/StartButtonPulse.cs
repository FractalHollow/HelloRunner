using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TapPromptPulse : MonoBehaviour
{
    public TMP_Text text;
    public CanvasGroup group;       // optional: assign if you prefer fading the whole object
    public float minAlpha = 0.25f;
    public float maxAlpha = 1f;
    public float pulsesPerSecond = 1.5f;
    public float scaleBobAmount = 0.02f;   // 0 = disable scale bob

    Vector3 baseScale;

    void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    void Awake()
    {
        if (!text) text = GetComponent<TMP_Text>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        // Use unscaled time so it animates while Time.timeScale == 0
        float t = 0.5f * (1f + Mathf.Sin(Time.unscaledTime * pulsesPerSecond * Mathf.PI * 2f));
        float a = Mathf.Lerp(minAlpha, maxAlpha, t);

        if (group)                // fade entire object (preferred if parent has CanvasGroup)
        {
            group.alpha = a;
        }
        else if (text)            // fallback: fade TMP face color
        {
            var c = text.color;
            c.a = a;
            text.color = c;
        }

        if (scaleBobAmount > 0f)  // gentle scale bob
        {
            float s = 1f + scaleBobAmount * Mathf.Sin(Time.unscaledTime * pulsesPerSecond * Mathf.PI * 2f);
            transform.localScale = baseScale * s;
        }
    }
}
