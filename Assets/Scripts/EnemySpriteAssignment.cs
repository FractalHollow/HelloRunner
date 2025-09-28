using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class ObstacleAppearance : MonoBehaviour
{
    public Sprite[] enemySprites;   // assign in Inspector

    SpriteRenderer sr;
    BoxCollider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        // pick a sprite
        if (enemySprites != null && enemySprites.Length > 0)
        {
            sr.sprite = enemySprites[Random.Range(0, enemySprites.Length)];
        }

        // resize collider in LOCAL space to match the sprite's unscaled size
        if (sr.sprite != null)
        {
            var ppu = sr.sprite.pixelsPerUnit;
            Vector2 localSize = sr.sprite.rect.size / ppu;   // local-space size
            col.size = localSize;

            // center the collider on the sprite's pivot
            // For a sprite with center pivot, this is (0,0).
            // If you use a non-center pivot, this keeps it correct:
            col.offset = sr.sprite.bounds.center; // bounds.center is in local units
        }
    }
}
