using UnityEngine;

[DisallowMultipleComponent]
public class UIButtonPulse : MonoBehaviour
{
    public float pulsesPerSecond = 0.75f;
    public float scaleBobAmount = 0.02f;

    Vector3 baseScale;
    bool hasBaseScale;

    void Awake()
    {
        CaptureBaseScale();
    }

    void OnDisable()
    {
        ResetVisualState();
    }

    void Update()
    {
        CaptureBaseScale();

        float wave = Mathf.Sin(Time.unscaledTime * pulsesPerSecond * Mathf.PI * 2f);

        if (scaleBobAmount > 0f)
        {
            float s = 1f + scaleBobAmount * wave;
            transform.localScale = baseScale * s;
        }
    }

    public void ResetVisualState()
    {
        CaptureBaseScale();

        transform.localScale = baseScale;
    }

    void CaptureBaseScale()
    {
        if (hasBaseScale) return;

        baseScale = transform.localScale;
        hasBaseScale = true;
    }
}
