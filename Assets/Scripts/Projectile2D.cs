using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 6f;

    [Tooltip("Direction of travel in world space.")]
    public Vector2 dir = Vector2.left;  // default: shoot left

    public int damage = 1; // future use

    [Header("Visuals")]
    [Tooltip("Degrees to rotate the sprite so it points along travel direction. Use 0 if your sprite points right by default. Try 90, -90, or 180 if needed.")]
    public float angleOffset = 0f;

    [Tooltip("If true, re-applies rotation every frame (useful if something changes dir after spawn).")]
    public bool rotateContinuously = false;

    float deathAt;
    GameManager gm;

    void OnEnable()
    {
        if (!gm) gm = FindFirstObjectByType<GameManager>();
        deathAt = Time.time + lifetime;

        // Prevent mirrored sprites from parent/scale weirdness
        var s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), s.z);

        ApplyRotationFromDir();
    }

    void Update()
    {
        // Move in world space (rotation does NOT affect movement — dir does)
        Vector2 d = dir;
        if (d.sqrMagnitude > 0.0001f)
            d = d.normalized;
        else
            d = Vector2.left; // safe fallback

        transform.position += (Vector3)(d * speed * Time.deltaTime);

        if (rotateContinuously)
            ApplyRotationFromDir();

        if (Time.time >= deathAt)
            Destroy(gameObject);
    }

    void ApplyRotationFromDir()
    {
        if (dir.sqrMagnitude <= 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only care about player
        var player = other.GetComponent<PlayerGravityFlip>();
        if (!player) return;

        // Try shield first if present
        var shield = player.GetComponent<PlayerShield>();
        if (shield && shield.TryAbsorbHit())
        {
            Destroy(gameObject);
            return;
        }

        // No shield or no charges -> end run
        if (gm) gm.EndRun();
    }
}
