using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CosmeticRowUI : MonoBehaviour
{
    [Header("Refs")]
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text statusText;
    public Button selectButton;
    public TMP_Text buttonText;

    SkinDef def;
    CosmeticsPanelController owner;

    public void Bind(CosmeticsPanelController owner, SkinDef def)
    {
        this.owner = owner;
        this.def = def;

        Refresh();
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClickSelect);
    }

    public void Refresh()
    {
        if (def == null) return;

        if (nameText) nameText.text = def.displayName;

        if (icon)
        {
            icon.sprite = def.sprite;
            icon.enabled = (def.sprite != null);
            icon.preserveAspect = true;
        }

        bool unlocked = CosmeticsManager.I != null && CosmeticsManager.I.IsUnlocked(def.id);
        bool selected = CosmeticsManager.I != null && CosmeticsManager.I.SelectedId == def.id;

        // Status line
        if (statusText)
        {
            if (selected) statusText.text = "Selected";
            else if (unlocked) statusText.text = "Unlocked";
            else statusText.text = GetLockReason(def);
        }

        // Button state
        if (selectButton)
        {
            selectButton.interactable = unlocked && !selected;
        }

        if (buttonText)
        {
            if (selected) buttonText.text = "Selected";
            else if (unlocked) buttonText.text = "Select";
            else buttonText.text = "Locked";
        }
    }

    string GetLockReason(SkinDef def)
    {
        if (def.unlockType == SkinDef.UnlockType.PrestigeRequired)
            return $"Requires Prestige {def.prestigeRequired}";
        if (def.unlockType == SkinDef.UnlockType.Paid)
            return $"Paid ({def.priceText})";
        return "Locked";
    }

    void OnClickSelect()
    {
        if (def == null || CosmeticsManager.I == null) return;

        // Only allow selecting unlocked
        if (!CosmeticsManager.I.IsUnlocked(def.id))
        {
            // Later: show toast "Coming soon" for paid skins, etc.
            return;
        }

        CosmeticsManager.I.TrySelect(def.id);
        owner?.RefreshAll();
    }
}
