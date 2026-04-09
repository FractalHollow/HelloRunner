using System;
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
            icon.enabled = def.sprite != null;
            icon.preserveAspect = true;
        }

        bool unlocked = CosmeticsManager.I != null && CosmeticsManager.I.IsUnlocked(def.id);
        bool selected = CosmeticsManager.I != null && CosmeticsManager.I.SelectedId == def.id;
        bool useRealIap = IapManager.UseRealMoneyPurchasing;
        string productId = IapManager.ProductIdForSkin(def.id);
        bool purchaseInProgress = useRealIap && !string.IsNullOrEmpty(productId) && IapManager.I != null && IapManager.I.IsPurchaseInProgressFor(productId);
        string paidPrice = GetPaidPriceText();

        if (statusText)
        {
            if (selected) statusText.text = "Selected";
            else if (unlocked) statusText.text = "Unlocked";
            else if (purchaseInProgress) statusText.text = "Processing purchase...";
            else statusText.text = GetLockReason(def, paidPrice);
        }

        if (selectButton)
        {
            if (!unlocked && def.unlockType == SkinDef.UnlockType.Paid)
                selectButton.interactable = !purchaseInProgress;
            else
                selectButton.interactable = unlocked && !selected;
        }

        if (buttonText)
        {
            if (selected) buttonText.text = "Selected";
            else if (unlocked) buttonText.text = "Select";
            else if (purchaseInProgress) buttonText.text = "Processing...";
            else if (def.unlockType == SkinDef.UnlockType.Paid) buttonText.text = "Buy";
            else buttonText.text = "Locked";
        }
    }

    string GetLockReason(SkinDef skinDef, string paidPrice)
    {
        if (skinDef.unlockType == SkinDef.UnlockType.PrestigeRequired)
            return $"Requires Prestige {skinDef.prestigeRequired}";
        if (skinDef.unlockType == SkinDef.UnlockType.Paid)
            return $"Paid ({paidPrice})";
        return "Locked";
    }

    void OnClickSelect()
    {
        if (def == null || CosmeticsManager.I == null) return;

        bool unlocked = CosmeticsManager.I.IsUnlocked(def.id);

        Debug.Log($"[CosmeticsUI] Click: {def.id} unlocked={unlocked} type={def.unlockType}");
        Debug.Log($"[CosmeticsUI] ConfirmDialog.I is {(ConfirmDialog.I ? "SET" : "NULL")}");

        if (!unlocked && def.unlockType == SkinDef.UnlockType.Paid)
        {
            if (!ConfirmDialog.I)
            {
                Debug.LogError($"[CosmeticsUI] Paid skin '{def.id}' could not open confirm dialog because ConfirmDialog.I is null.");
                return;
            }

            Debug.Log($"[CosmeticsUI] Opening paid skin flow for '{def.id}'.");

            if (!IapManager.UseRealMoneyPurchasing)
            {
                OpenStubUnlockConfirm();
                return;
            }

            if (IapManager.I == null || !IapManager.TryGetProductIdForSkin(def.id, out var productId))
            {
                ShowInfoDialog("Store unavailable. Please try again in a moment.");
                return;
            }

            if (!IapManager.I.IsStoreReady)
            {
                ShowInfoDialog("Store is still connecting. Please try again in a moment.");
                return;
            }

            if (IapManager.I.IsPurchaseInProgressFor(productId))
            {
                ShowInfoDialog("This purchase is already in progress.");
                return;
            }

            try
            {
                ConfirmDialog.I.Show(
                    $"Buy {def.displayName} for {GetPaidPriceText()}?",
                    () =>
                    {
                        Debug.Log($"[CosmeticsUI] Confirm accepted for paid skin '{def.id}'. Starting store purchase.");
                        IapManager.I.PurchaseProduct(productId);
                        owner?.RefreshAll();
                    }
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CosmeticsUI] Exception while starting purchase for paid skin '{def.id}': {ex}");
            }

            return;
        }

        if (!unlocked) return;

        CosmeticsManager.I.TrySelect(def.id);
        owner?.RefreshAll();
    }

    string GetPaidPriceText()
    {
        if (def == null)
            return string.Empty;

        if (def.unlockType != SkinDef.UnlockType.Paid)
            return def.priceText;

        if (IapManager.UseRealMoneyPurchasing && IapManager.I != null && IapManager.TryGetProductIdForSkin(def.id, out var productId))
        {
            var localized = IapManager.I.GetLocalizedPrice(productId);
            if (!string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        return def.priceText;
    }

    void OpenStubUnlockConfirm()
    {
        try
        {
            ConfirmDialog.I.Show(
                $"Unlock {def.displayName} for local testing?",
                () =>
                {
                    Debug.Log($"[CosmeticsUI] Confirm accepted for paid skin '{def.id}'. Unlocking test skin.");
                    CosmeticsManager.I.UnlockPaidSkinForTesting(def.id);
                    CosmeticsManager.I.TrySelect(def.id);
                    owner?.RefreshAll();
                }
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CosmeticsUI] Exception while showing stub confirm dialog for paid skin '{def.id}': {ex}");
        }
    }

    void ShowInfoDialog(string message)
    {
        try
        {
            ConfirmDialog.I.Show(message, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CosmeticsUI] Exception while showing info dialog for paid skin '{def.id}': {ex}");
        }
    }
}
