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

    [Header("Visual - Player")]
    public Sprite sprite; // legacy fallback if animated frames are not assigned
    public Sprite[] idleFrames;
    public Sprite[] jumpFrames;
    [Min(0.01f)] public float idleFrameDuration = 0.2f;
    [Min(0.01f)] public float jumpFrameDuration = 0.08f;

    [Header("Visual - Den Fox")]
    public Sprite denSleepSprite; // sleeping fox shown in Den
    public Sprite denAwakeSprite; // wake fox shown after claiming idle

    [Header("FX")]
    public Color flipFxColor = Color.white;

    [Header("Unlock")]
    public UnlockType unlockType = UnlockType.DefaultUnlocked;
    public int prestigeRequired = 0;  // used only if PrestigeRequired

    [Header("Paid (stub)")]
    public string priceText = "$0.99"; // UI only for now

    [Header("UI")]
    public int sortOrder = 0;
}
