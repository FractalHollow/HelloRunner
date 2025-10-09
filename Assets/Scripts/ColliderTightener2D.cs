using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ColliderTightener2D : MonoBehaviour
{
    [Header("Base size (no upgrade)")]
    [Range(0.5f,1f)] public float baseWidthFactor  = 0.95f;
    [Range(0.5f,1f)] public float baseHeightFactor = 0.95f;

    [Header("Per-level shrink")]
    [Range(0f,0.2f)] public float perLevelWidth  = 0.03f;
    [Range(0f,0.2f)] public float perLevelHeight = 0.03f;

    [Header("Clamps")]
    [Range(0.5f,1f)] public float minWidth  = 0.80f;
    [Range(0.5f,1f)] public float minHeight = 0.80f;

    int currentLevel = 0;
    BoxCollider2D col;
    SpriteRenderer sr;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr  = GetComponentInChildren<SpriteRenderer>();
        ApplyLevel(currentLevel); // apply defaults on load
    }

    void OnValidate()
    {
        if (!col) col = GetComponent<BoxCollider2D>();
        if (!sr)  sr  = GetComponentInChildren<SpriteRenderer>();
        ApplyLevel(currentLevel);
    }

    public void ApplyLevel(int level)
    {
        currentLevel = Mathf.Max(0, level);

        // compute factors from level
        float w = Mathf.Max(minWidth,  baseWidthFactor  - perLevelWidth  * currentLevel);
        float h = Mathf.Max(minHeight, baseHeightFactor - perLevelHeight * currentLevel);

        // base on current collider size if no sprite; else base on sprite bounds in local space
        if (sr && sr.sprite)
        {
            var lossy = transform.lossyScale;
            float sx = Mathf.Abs(lossy.x) < 1e-5f ? 1f : Mathf.Abs(lossy.x);
            float sy = Mathf.Abs(lossy.y) < 1e-5f ? 1f : Mathf.Abs(lossy.y);
            var b = sr.bounds; // world
            Vector2 localSize = new Vector2(b.size.x / sx, b.size.y / sy);
            col.size = new Vector2(localSize.x * w, localSize.y * h);
            col.offset = Vector2.zero;
        }
        else
        {
            var s = col.size;
            col.size = new Vector2(s.x * w, s.y * h);
        }
    }
}
