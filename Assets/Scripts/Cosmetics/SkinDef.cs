using UnityEngine;

[CreateAssetMenu(menuName = "FoxRun/SkinDef", fileName = "SkinDef_")]
public class SkinDef : ScriptableObject
{
    public enum UnlockType
    {
        DefaultUnlocked,
        PrestigeRequired,
        Paid
    }

    [Header("Identity")]
    public string id = "skin_default";
    public string displayName = "Default Fox";

    [Header("Visual")]
    public Sprite sprite; // sprite that replaces the fox sprite

    [Header("Unlock")]
    public UnlockType unlockType = UnlockType.DefaultUnlocked;
    public int prestigeRequired = 0;  // used only if PrestigeRequired

    [Header("Paid (stub)")]
    public string priceText = "$0.99"; // UI only for now
}
