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
    public float visibleSeconds = 60f;

    Coroutine routine;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();

        // Keep object active; hide with alpha so we avoid activation timing issues.
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
    }

    public void ShowUnlocked(List<AchievementDef> unlocked)
    {
        if (unlocked == null || unlocked.Count == 0) return;

        // Ensure visible immediately
        if (group)
        {
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(unlocked));
    }

    IEnumerator ShowRoutine(List<AchievementDef> unlocked)
    {
        int count = unlocked.Count;

        if (titleText)
            titleText.text = (count == 1) ? "Achievement Unlocked!" : $"{count} Achievements Unlocked!";

        // List up to 3 names
        if (bodyText)
        {
            int max = Mathf.Min(3, count);
            string s = "";
            for (int i = 0; i < max; i++)
                s += $"• {unlocked[i].displayName}\n";
            if (count > max) s += $"• +{count - max} more\n";
            bodyText.text = s.TrimEnd();
        }

        yield return new WaitForSecondsRealtime(visibleSeconds);

        HideInstant();
        routine = null;
    }
}
