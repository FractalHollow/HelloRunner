using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PrestigeUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text requirementText;
    [SerializeField] TMP_Text rewardText;
    [SerializeField] TMP_Text levelText;
    [SerializeField] Button prestigeButton;

    [Header("Confirm Modal")]
    [SerializeField] GameObject confirmPanel;
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;
    [SerializeField] TMP_Text confirmBodyText;

    void OnEnable()
    {
        Wire();
        Refresh();
        if (confirmPanel) confirmPanel.SetActive(false);
    }

    void Wire()
    {
        if (prestigeButton)
        {
            prestigeButton.onClick.RemoveAllListeners();
            prestigeButton.onClick.AddListener(OpenConfirm);
        }

        if (confirmButton)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmPrestige);
        }

        if (cancelButton)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelPrestige);
        }
    }

    public void Refresh()
    {
        int lvl = PrestigeManager.Level;
        int best = PrestigeManager.BestDistanceThisPrestigeM;
        int req = PrestigeManager.PrestigeDistanceRequirementM;

        bool can = PrestigeManager.CanPrestige();

        if (levelText)
        {
            levelText.gameObject.SetActive(lvl > 0);
            if (lvl > 0)
                levelText.text = $"Prestige {lvl}  (x{PrestigeManager.ScoreMult:0.#} Score & Embers)";
        }

        if (requirementText)
            requirementText.text = $"Requirement: Reach {req}m (This Prestige Best: {best}m)";

        if (rewardText)
        {
            float nextMult = Mathf.Pow(1.5f, lvl + 1);
            rewardText.text = $"Next Prestige Reward: x{nextMult:0.#} Score & Embers";
        }

        if (prestigeButton)
            prestigeButton.interactable = can;
    }

    void OpenConfirm()
    {
        Refresh();

        if (!confirmPanel) return;

        if (confirmBodyText)
        {
            float nextMult = Mathf.Pow(1.5f, PrestigeManager.Level + 1);
            confirmBodyText.text =
                $"Prestiging will:\n" +
                $"• Reset ALL upgrades\n" +
                $"• Re-lock Upgrades + Run Modifiers\n" +
                $"• Reset Embers to 0\n" +
                $"• Permanent increased difficulty scaling\n\n" +
                $"You will gain:\n" +
                $"• Permanent x{nextMult:0.#} Score & Embers\n" +
                $"• Permanent Idle Ember generation bonus\n\n" +
                $"Achievements will be preserved.\n" +
                $"This cannot be undone.\n\n" +
                $"Are you sure you want to Prestige?";
        }

        confirmPanel.SetActive(true);
    }

    void CancelPrestige()
    {
        if (confirmPanel) confirmPanel.SetActive(false);
    }

    void ConfirmPrestige()
    {
        if (!PrestigeManager.CanPrestige()) { Refresh(); return; }

        PrestigeManager.DoPrestige();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
