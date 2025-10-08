// UpgradeDef.cs
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDef", menuName = "Game/Upgrade Definition")]
public class UpgradeDef : ScriptableObject
{
    public string id;             // "shield", "magnet", etc.
    public string displayName;    // shown in UI
    [TextArea] public string description;

    public int baseCost = 50;
    public float costScale = 1.5f;
    public int maxLevel = 3;

    // Type of effect; later switch on this to apply upgrades
    public enum EffectType { None, Shield, Magnet, SmallerHitbox, ComboBoost, RunModifier_Vertical, RunModifier_Projectiles}
    public EffectType effectType;
}
