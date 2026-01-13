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
            // Allow clicking paid locked skins (to show confirm dialog)
            if (!unlocked && def.unlockType == SkinDef.UnlockType.Paid)
                selectButton.interactable = true;
            else
                selectButton.interactable = unlocked && !selected;
        }


        if (buttonText)
        {
            if (selected) buttonText.text = "Selected";
            else if (unlocked) buttonText.text = "Select";
            else if (def.unlockType == SkinDef.UnlockType.Paid) buttonText.text = "Buy";
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

    bool unlocked = CosmeticsManager.I.IsUnlocked(def.id);

    Debug.Log($"[CosmeticsUI] Click: {def.id} unlocked={unlocked} type={def.unlockType}");
    Debug.Log($"[CosmeticsUI] ConfirmDialog.I is {(ConfirmDialog.I ? "SET" : "NULL")}");

    // Paid + locked → confirm dialog
    if (!unlocked && def.unlockType == SkinDef.UnlockType.Paid)
    {
        ConfirmDialog.I?.Show(
            $"This skin will cost {def.priceText} on release.\nUnlock for testing?",
            () =>
            {
                // TEST unlock (Phase A only)
                PlayerPrefs.SetInt($"skin_unlocked_{def.id}", 1);
                PlayerPrefs.Save();

                CosmeticsManager.I.TrySelect(def.id);
                owner?.RefreshAll();
            }
        );
        return;
    }

    // Normal selection
    if (!unlocked) return;

    CosmeticsManager.I.TrySelect(def.id);
    owner?.RefreshAll();
}

}
