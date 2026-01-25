using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class CosmeticsManager : MonoBehaviour
{
    public static CosmeticsManager I { get; private set; }

    const string K_SelectedSkinId = "skin_selected_id";
    string K_Unlocked(string id) => $"skin_unlocked_{id}";

    [Header("Auto-Apply Target")]
    [Tooltip("Optional. If set, this renderer will be used. If null, the manager will try to find one on the Player.")]
    public SpriteRenderer targetRenderer;

    [Tooltip("If targetRenderer is null, we try to find a SpriteRenderer under Player with this name. Leave blank to just take the first SpriteRenderer found.")]
    public string preferredRendererName = "";

    List<SkinDef> skins = new List<SkinDef>();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadSkins();
        EnsureDefaultSelection();
        RefreshUnlocksFromPrestige();
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
        skins.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
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
        if (def.unlockType == SkinDef.UnlockType.Paid) return PlayerPrefs.GetInt(K_Unlocked(id), 0) == 1; // stays locked unless you manually unlock for testing
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
                if (PlayerPrefs.GetInt(K_Unlocked(def.id), 0) == 0)
                {
                    SetUnlocked(def.id);
                    anyChanged = true;
                    Debug.Log($"[Cosmetics] Unlocked skin '{def.id}' via Prestige {p}.");
                }
            }
        }

        if (anyChanged) PlayerPrefs.Save();
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
        if (!def || !def.sprite) return;

        r.sprite = def.sprite;
        // If you later add animation controllers or full prefabs, this is where we expand.
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
