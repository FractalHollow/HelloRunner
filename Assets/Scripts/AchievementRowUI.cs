using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementRowUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text progressText;
    public Button claimButton;
    public TMP_Text descriptionText;

    AchievementDef _def;
    GameManager _gm;

    public void Bind(AchievementDef def, int progress, int target, bool unlocked, bool claimed, GameManager gm)
    {
        _def = def;
        _gm = gm;

        if (titleText) titleText.text = def ? def.displayName : "Achievement";
        if (progressText) progressText.text = $"{progress}/{target}";
        if (descriptionText) descriptionText.text = def ? def.description : "";

        if (claimButton)
        {
            claimButton.onClick.RemoveAllListeners();

            bool canClaim = unlocked && !claimed;
            claimButton.gameObject.SetActive(unlocked);       // hide until unlocked (clean)
            claimButton.interactable = canClaim;

            if (canClaim)
            {
                claimButton.onClick.AddListener(OnClaim);
                // Optional: change button text to show reward
                var tmp = claimButton.GetComponentInChildren<TMP_Text>();
                if (tmp) tmp.text = $"Claim +{def.rewardEmbers}";
            }
            else
            {
                var tmp = claimButton.GetComponentInChildren<TMP_Text>();
                if (tmp) tmp.text = claimed ? "Claimed" : "Locked";
            }
        }
    }

    void OnClaim()
    {
        if (_def == null || _gm == null) return;

        bool ok = AchievementManager.I != null && AchievementManager.I.TryClaim(_def, _gm);
        if (ok)
        {
            // Refresh panel (the controller will handle this)
            // We find parent controller in a safe way:
            var controller = GetComponentInParent<AchievementsPanelController>();
            if (controller) controller.Refresh();
        }
    }
}
