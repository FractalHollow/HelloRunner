using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerAppearance : MonoBehaviour
{
    public SpriteRenderer visual;  // assign your Visual child SR here
    BoxCollider2D col;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        if (visual == null || visual.sprite == null) return;

        // 1) Get sprite bounds in WORLD space (accounts for child position/scale)
        var worldBounds = visual.bounds;                  // Bounds in world units
        Vector2 worldSize   = worldBounds.size;
        Vector2 worldCenter = worldBounds.center - transform.position; // relative to root

        // 2) Convert WORLD to ROOT-LOCAL for the BoxCollider2D
        Vector3 rootScale = transform.lossyScale;
        // guard against zero scale
        float sx = Mathf.Approximately(rootScale.x, 0f) ? 1f : rootScale.x;
        float sy = Mathf.Approximately(rootScale.y, 0f) ? 1f : rootScale.y;

        Vector2 localSize   = new Vector2(worldSize.x / sx,   worldSize.y / sy);
        Vector2 localCenter = new Vector2(worldCenter.x / sx, worldCenter.y / sy);

        col.size   = localSize;
        col.offset = localCenter;
    }
}
