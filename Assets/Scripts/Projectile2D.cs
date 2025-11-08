using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 6f;
    public Vector2 dir = Vector2.left;  // default: shoot left
    public int damage = 1;               // future use

    float deathAt;
    GameManager gm;

    void OnEnable()
    {
        if (!gm) gm = FindObjectOfType<GameManager>();
        deathAt = Time.time + lifetime;
    }

    void Update()
    {
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);
        if (Time.time >= deathAt) Destroy(gameObject);
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
