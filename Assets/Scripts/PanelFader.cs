using UnityEngine;
using System.Collections;
using System; // <-- needed for Action

public class PanelFader : MonoBehaviour
{
    public CanvasGroup group;
    public float duration = 0.25f;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
    }

    public void ShowInstant()
    {
        gameObject.SetActive(true);
        if (group) group.alpha = 1f;
    }

    public void HideInstant()
    {
        if (group) group.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void FadeIn(Action onComplete = null)
    {
        gameObject.SetActive(true);
        StartCoroutine(Fade(0f, 1f, onComplete, deactivateAtEnd: false));
    }

    public void FadeOut(Action onComplete = null, bool deactivateAtEnd = true)
    {
        // We assume the panel is currently visible (alpha ~1)
        StartCoroutine(Fade(1f, 0f, onComplete, deactivateAtEnd));
    }

    IEnumerator Fade(float from, float to, Action onComplete, bool deactivateAtEnd)
    {
        float t = 0f;
        if (group) group.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // unaffected by Time.timeScale
            if (group) group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        if (group) group.alpha = to;
        if (deactivateAtEnd && to <= 0f) gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}
