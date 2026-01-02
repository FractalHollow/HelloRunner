using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AchievementToast : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Header("Timing")]
    public float visibleSeconds = 3.0f;

    Coroutine routine;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        HideInstant();
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

    public void ShowUnlocked(List<AchievementDef> unlocked)
    {
        if (unlocked == null || unlocked.Count == 0) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(unlocked));
    }

    IEnumerator ShowRoutine(List<AchievementDef> unlocked)
    {
        gameObject.SetActive(true);

        int count = unlocked.Count;
        if (titleText) titleText.text = (count == 1) ? "Achievement Unlocked!" : $"{count} Achievements Unlocked!";

        // List up to 3 names
        int max = Mathf.Min(3, count);
        string s = "";
        for (int i = 0; i < max; i++)
        {
            s += $"• {unlocked[i].displayName}\n";
        }
        if (count > max) s += $"• +{count - max} more\n";
        if (bodyText) bodyText.text = s.TrimEnd();

        if (group)
        {
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        yield return new WaitForSecondsRealtime(visibleSeconds);
        HideInstant();
    }
}
