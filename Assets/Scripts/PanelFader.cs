using UnityEngine;
using System.Collections;
using System;

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
        if (group)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    public void HideInstant()
    {
        if (group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    public void FadeIn(Action onComplete = null)
    {
        gameObject.SetActive(true);
        StartCoroutine(Fade(0f, 1f, onComplete, deactivateAtEnd: false));
    }

    public void FadeOut(Action onComplete = null, bool deactivateAtEnd = true)
    {
        StartCoroutine(Fade(1f, 0f, onComplete, deactivateAtEnd));
    }

    IEnumerator Fade(float from, float to, Action onComplete, bool deactivateAtEnd)
    {
        float t = 0f;
        if (group) group.alpha = from;

        // Pre-set interactivity for fade-out so clicks pass through immediately
        if (group && to <= 0f)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            if (group) group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        if (group) group.alpha = to;

        if (group && to > 0f)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        if (deactivateAtEnd && to <= 0f) gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}
