using UnityEngine;
using System.Collections;
using System;

public class PanelFader : MonoBehaviour
{
    public CanvasGroup group;
    public float duration = 0.25f;
    Coroutine activeFade;

    public bool IsTransitioning => activeFade != null;

    public static PanelFader Ensure(GameObject target, float duration = 0.25f)
    {
        if (!target) return null;

        var fader = target.GetComponent<PanelFader>();
        if (!fader) fader = target.AddComponent<PanelFader>();

        var group = target.GetComponent<CanvasGroup>();
        if (!group) group = target.AddComponent<CanvasGroup>();

        fader.group = group;
        fader.duration = duration;
        return fader;
    }

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
    }

    public void ShowInstant()
    {
        StopActiveFade();
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
        StopActiveFade();
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
        if (IsTransitioning) return;

        gameObject.SetActive(true);
        StartFade(0f, 1f, onComplete, deactivateAtEnd: false);
    }

    public void FadeOut(Action onComplete = null, bool deactivateAtEnd = true)
    {
        if (IsTransitioning) return;

        StartFade(1f, 0f, onComplete, deactivateAtEnd);
    }

    void StartFade(float from, float to, Action onComplete, bool deactivateAtEnd)
    {
        activeFade = StartCoroutine(Fade(from, to, onComplete, deactivateAtEnd));
    }

    void StopActiveFade()
    {
        if (activeFade == null) return;

        StopCoroutine(activeFade);
        activeFade = null;
    }

    IEnumerator Fade(float from, float to, Action onComplete, bool deactivateAtEnd)
    {
        float t = 0f;
        if (group)
        {
            group.alpha = from;
            group.interactable = false;
            group.blocksRaycasts = true;
        }

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            if (group) group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        if (group)
        {
            group.alpha = to;

            bool visible = to > 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        if (deactivateAtEnd && to <= 0f) gameObject.SetActive(false);
        activeFade = null;
        onComplete?.Invoke();
    }

    void OnDisable()
    {
        activeFade = null;
    }
}
