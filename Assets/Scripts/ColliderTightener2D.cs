using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class ColliderTightener2D : MonoBehaviour
{
    [Range(0.5f, 1f)] public float widthFactor  = 0.90f; // 90% width
    [Range(0.5f, 1f)] public float heightFactor = 0.90f; // 90% height
    public Vector2 pixelInset = Vector2.zero;            // optional exact pixels to trim inside

    SpriteRenderer sr;
    BoxCollider2D col;

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }

    void Apply()
    {
        if (!col) col = GetComponent<BoxCollider2D>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();

        // If you have no sprite, just scale existing size.
        if (!sr)
        {
            var s = col.size;
            col.size = new Vector2(s.x * widthFactor, s.y * heightFactor);
            return;
        }

        // Use the sprite's world bounds as a baseline, then shrink.
        var b = sr.bounds;
        // Convert world size to local collider size
        var lossy = transform.lossyScale;
        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;

        Vector2 localSize = new Vector2(b.size.x / sx, b.size.y / sy);

        // Optional pixel inset -> convert pixels to world -> then to local
        var sprite = sr.sprite;
        Vector2 insetLocal = Vector2.zero;
        if (sprite != null && (pixelInset.x != 0 || pixelInset.y != 0))
        {
            float ppu = sprite.pixelsPerUnit > 0 ? sprite.pixelsPerUnit : 100f;
            Vector2 worldInset = pixelInset / ppu;
            insetLocal = new Vector2(worldInset.x / sx, worldInset.y / sy);
        }

        col.size = new Vector2(localSize.x * widthFactor, localSize.y * heightFactor) - insetLocal;
        col.offset = Vector2.zero; // center; adjust if your sprite is off-center
    }
}
