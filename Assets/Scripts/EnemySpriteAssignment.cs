using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class ObstacleAppearance : MonoBehaviour
{
    // --- NEW: two-frame enemy sets (preferred) ---
    [System.Serializable]
    public struct EnemyAnimSet
    {
        public Sprite frameA;
        public Sprite frameB;
        [Tooltip("Per-enemy fps; 0 = use defaultFps")]
        public float fps;
    }

    [Header("Two-frame enemies (use this)")]
    public EnemyAnimSet[] enemies;     // assign 4 elements in Inspector
    public float defaultFps = 1.5f;

    [Header("Legacy single-sprite fallback (optional)")]
    public Sprite[] enemySprites;      // kept for backward compatibility

    // Refs
    SpriteRenderer sr;
    BoxCollider2D col;
    TwoFrameSprite flipper;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        flipper = GetComponent<TwoFrameSprite>(); // may be null; we'll add if needed
    }

    void Start()
    {
        // Prefer two-frame sets if provided
        if (enemies != null && enemies.Length > 0 && HasValidSet(enemies))
        {
            AssignTwoFrameEnemy();
        }
        else
        {
            AssignLegacySingleSprite();
        }
    }

    // ---------- Helpers ----------

    bool HasValidSet(EnemyAnimSet[] sets)
    {
        for (int i = 0; i < sets.Length; i++)
        {
            if (sets[i].frameA != null && sets[i].frameB != null)
                return true;
        }
        return false;
    }

    void AssignTwoFrameEnemy()
    {
        // Pick a random valid set
        EnemyAnimSet set = enemies[Random.Range(0, enemies.Length)];
        // If randomly chosen one is invalid, loop until valid (small array; safe)
        int guard = 8;
        while (guard-- > 0 && (set.frameA == null || set.frameB == null))
            set = enemies[Random.Range(0, enemies.Length)];

        // Ensure flipper exists on same object as the SR
        if (!flipper) flipper = gameObject.AddComponent<TwoFrameSprite>();

        // Configure flipper
        flipper.frameA = set.frameA;
        flipper.frameB = set.frameB;
        flipper.fps    = (set.fps > 0f) ? set.fps : defaultFps;
        flipper.ResetFrame();

        // Size collider from frameA (assumes both frames have same rect/PPU)
        SetColliderFromSprite(set.frameA ?? sr.sprite);
    }

    void AssignLegacySingleSprite()
    {
        if (enemySprites != null && enemySprites.Length > 0)
        {
            var chosen = enemySprites[Random.Range(0, enemySprites.Length)];
            sr.sprite = chosen;
            SetColliderFromSprite(chosen);
        }
        else
        {
            // Nothing assigned—leave as-is
        }
    }

    void SetColliderFromSprite(Sprite s)
    {
        if (!s || col == null) return;

        // Local-space size from sprite rect & PPU
        var ppu = s.pixelsPerUnit;
        const float shrink = 0.9f; // 90% of visual
        Vector2 localSize = s.rect.size / ppu;
        col.size = localSize * shrink;
    
        // Center the collider on the sprite pivot (bounds are in local units)
        col.offset = s.bounds.center;
    }
}
