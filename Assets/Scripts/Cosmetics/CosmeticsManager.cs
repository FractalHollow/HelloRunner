using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class CosmeticsManager : MonoBehaviour
{
    public static CosmeticsManager I { get; private set; }
    public static event System.Action<bool> NewSkinIndicatorChanged;

    const string K_SelectedSkinId = "skin_selected_id";
    string K_Unlocked(string id) => $"skin_unlocked_{id}";
    string K_Seen(string id) => $"skin_seen_{id}";

    [Header("Auto-Apply Target")]
    [Tooltip("Optional. If set, this renderer will be used. If null, the manager will try to find one on the Player.")]
    public SpriteRenderer targetRenderer;

    [Tooltip("If targetRenderer is null, we try to find a SpriteRenderer under Player with this name. Leave blank to just take the first SpriteRenderer found.")]
    public string preferredRendererName = "";

    List<SkinDef> skins = new List<SkinDef>();
    bool hasUnseenUnlockedSkin;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadSkins();
        EnsureDefaultSelection();
        RefreshUnlocksFromPrestige();
        RefreshNewUnlockIndicatorState();

        // Guard against missing selected skin (removed/renamed defs)
        if (GetSelectedDef() == null)
        {
            EnsureDefaultSelection();
            PlayerPrefs.Save();
        }

    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // After reload (prestige), ensure prestige skins unlock correctly
        RefreshUnlocksFromPrestige();

        // Re-apply selected skin to the new scene's player
        ApplySelectedToPlayerIfFound();
    }

    void LoadSkins()
    {
        skins.Clear();
        skins.AddRange(Resources.LoadAll<SkinDef>("Skins"));
        skins.Sort((a, b) =>
            {
                if (!a && !b) return 0;
                if (!a) return 1;
                if (!b) return -1;

                int so = a.sortOrder.CompareTo(b.sortOrder);
                if (so != 0) return so;

                // tie-breaker so stable ordering if same sortOrder
                return string.CompareOrdinal(a.id, b.id);
            });
        Debug.Log($"[Cosmetics] Loaded {skins.Count} skins.");
    }

    void EnsureDefaultSelection()
    {
        // If nothing selected yet, select the first DefaultUnlocked skin (or first skin)
        var selected = PlayerPrefs.GetString(K_SelectedSkinId, "");
        if (!string.IsNullOrEmpty(selected)) return;

        SkinDef pick = null;
        for (int i = 0; i < skins.Count; i++)
        {
            if (skins[i] && skins[i].unlockType == SkinDef.UnlockType.DefaultUnlocked)
            {
                pick = skins[i];
                break;
            }
        }
        if (!pick && skins.Count > 0) pick = skins[0];

        if (pick != null)
        {
            PlayerPrefs.SetString(K_SelectedSkinId, pick.id);
            PlayerPrefs.Save();
        }
    }

    public IReadOnlyList<SkinDef> GetAllSkins() => skins;

    public string SelectedId => PlayerPrefs.GetString(K_SelectedSkinId, "default");

    public SkinDef GetSelectedDef()
    {
        var id = SelectedId;
        for (int i = 0; i < skins.Count; i++)
            if (skins[i] && skins[i].id == id) return skins[i];
        return null;
    }

    public bool IsUnlocked(string id)
    {
        var def = GetDef(id);
        if (def == null) return false;

        if (def.unlockType == SkinDef.UnlockType.DefaultUnlocked) return true;
        if (def.unlockType == SkinDef.UnlockType.Paid)
        {
            if (IapManager.UseRealMoneyPurchasing && IapManager.I != null && IapManager.TryGetProductIdForSkin(id, out var productId))
                return IapManager.I.IsOwned(productId) || PlayerPrefs.GetInt(K_Unlocked(id), 0) == 1;

            return PlayerPrefs.GetInt(K_Unlocked(id), 0) == 1;
        }
        return PlayerPrefs.GetInt(K_Unlocked(id), 0) == 1;
    }

    SkinDef GetDef(string id)
    {
        for (int i = 0; i < skins.Count; i++)
            if (skins[i] && skins[i].id == id) return skins[i];
        return null;
    }

    void SetUnlocked(string id)
    {
        PlayerPrefs.SetInt(K_Unlocked(id), 1);
    }

    void SetSeen(string id, bool seen)
    {
        PlayerPrefs.SetInt(K_Seen(id), seen ? 1 : 0);
    }

    bool IsSeen(string id)
    {
        return PlayerPrefs.GetInt(K_Seen(id), 0) == 1;
    }

    public bool HasUnseenUnlockedSkin => hasUnseenUnlockedSkin;

    void RefreshNewUnlockIndicatorState(bool notify = true)
    {
        bool next = false;

        for (int i = 0; i < skins.Count; i++)
        {
            var def = skins[i];
            if (!def || string.IsNullOrEmpty(def.id)) continue;
            if (!IsUnlocked(def.id)) continue;
            if (def.unlockType == SkinDef.UnlockType.DefaultUnlocked) continue;
            if (def.unlockType == SkinDef.UnlockType.Paid) continue;
            if (IsSeen(def.id)) continue;

            next = true;
            break;
        }

        if (hasUnseenUnlockedSkin == next) return;

        hasUnseenUnlockedSkin = next;
        if (notify) NewSkinIndicatorChanged?.Invoke(hasUnseenUnlockedSkin);
    }

    void UnlockSkinInternal(string id, bool markAsNew)
    {
        var def = GetDef(id);
        if (def == null || string.IsNullOrEmpty(def.id)) return;
        if (IsUnlocked(def.id)) return;

        SetUnlocked(def.id);
        SetSeen(def.id, !markAsNew);
    }

    public bool UnlockPaidSkinForTesting(string id)
    {
        var def = GetDef(id);
        if (def == null || def.unlockType != SkinDef.UnlockType.Paid) return false;
        if (IsUnlocked(id)) return false;

        UnlockSkinInternal(id, true);
        PlayerPrefs.Save();
        RefreshNewUnlockIndicatorState();
        return true;
    }

    public void MarkUnlockedSkinsAsSeen()
    {
        bool anyChanged = false;

        for (int i = 0; i < skins.Count; i++)
        {
            var def = skins[i];
            if (!def || string.IsNullOrEmpty(def.id)) continue;
            if (!IsUnlocked(def.id)) continue;
            if (def.unlockType == SkinDef.UnlockType.DefaultUnlocked) continue;
            if (IsSeen(def.id)) continue;

            SetSeen(def.id, true);
            anyChanged = true;
        }

        if (anyChanged) PlayerPrefs.Save();
        RefreshNewUnlockIndicatorState();
    }

    public void RefreshUnlocksFromPrestige()
    {
        int p = PrestigeManager.Level;

        bool anyChanged = false;

        for (int i = 0; i < skins.Count; i++)
        {
            var def = skins[i];
            if (!def || string.IsNullOrEmpty(def.id)) continue;

            if (def.unlockType == SkinDef.UnlockType.DefaultUnlocked) continue;
            if (def.unlockType == SkinDef.UnlockType.Paid) continue;

            if (def.unlockType == SkinDef.UnlockType.PrestigeRequired && p >= def.prestigeRequired)
            {
                if (!IsUnlocked(def.id))
                {
                    UnlockSkinInternal(def.id, true);
                    anyChanged = true;
                    Debug.Log($"[Cosmetics] Unlocked skin '{def.id}' via Prestige {p}.");
                }
            }
        }

        if (anyChanged) PlayerPrefs.Save();
        RefreshNewUnlockIndicatorState();
    }

    public bool TrySelect(string id)
    {
        if (!IsUnlocked(id)) return false;

        PlayerPrefs.SetString(K_SelectedSkinId, id);
        PlayerPrefs.Save();

        ApplySelectedToPlayerIfFound();
        return true;
    }

    public void ApplySelectedToPlayerIfFound()
{
    // If user explicitly assigned a renderer, use that.
    if (targetRenderer)
    {
        ApplyToRenderer(targetRenderer);
        return;
    }

    var player = FindPlayer();
    if (!player) return;

    // Only look under Player/Visual to avoid FX (ShieldVisual, particles, etc.)
    Transform visualT = player.transform.Find("Visual");
    if (!visualT)
    {
        Debug.LogWarning("[Cosmetics] Could not find Player/Visual. Assign targetRenderer or rename Visual.");
        return;
    }

    SpriteRenderer r = null;

    // Try preferred name first (must be a child under Visual)
    if (!string.IsNullOrEmpty(preferredRendererName))
    {
        var all = visualT.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] && all[i].gameObject.name == preferredRendererName)
            {
                r = all[i];
                break;
            }
        }
    }

    // Fallback: first SpriteRenderer under Visual
    if (!r)
        r = visualT.GetComponentInChildren<SpriteRenderer>(true);

    if (!r)
    {
        Debug.LogWarning("[Cosmetics] No SpriteRenderer found under Player/Visual.");
        return;
    }

    ApplyToRenderer(r);
}


    void ApplyToRenderer(SpriteRenderer r)
    {
        var def = GetSelectedDef();
        if (!def) return;

        var animator = r.GetComponent<PlayerSpriteAnimator>();
        if (animator)
        {
            animator.targetRenderer = r;
            animator.SetAnimationSet(
                def.idleFrames,
                def.jumpFrames,
                def.idleFrameDuration,
                def.jumpFrameDuration,
                def.sprite);
        }
        else if (def.sprite)
        {
            r.sprite = def.sprite;
        }

        // NEW: Apply flip FX color to match the selected skin (player only)
        var player = FindPlayer();
        if (player)
        {
            var flip = player.GetComponent<PlayerGravityFlip>();
            if (flip)
                flip.SetFlipFxColor(def.flipFxColor);
        }

        Debug.Log($"[Cosmetics] Applied skin '{def.id}' to renderer '{r.gameObject.name}'.");
    }


    GameObject FindPlayer()
    {
#if UNITY_2023_1_OR_NEWER
        var pgf = Object.FindFirstObjectByType<PlayerGravityFlip>();
        return pgf ? pgf.gameObject : null;
#else
        // Unity 2022 and earlier (avoid warnings if possible, but safe)
        var pgf = Object.FindObjectOfType<PlayerGravityFlip>();
        return pgf ? pgf.gameObject : null;
#endif
    }
}
