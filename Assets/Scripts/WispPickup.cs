using UnityEngine;

public class WispPickup : MonoBehaviour
{
    public int amount = 1;
    public float life = 10f;                 // auto-despawn
    public ParticleSystem pickupBurst;       // assign in prefab
    public float collectionRadius = 0.55f;
    public Vector2 collectionOffset = Vector2.zero;

    bool _consumed = false;                  // guard against double-trigger
    Collider2D pickupCollider;
    PlayerGravityFlip player;

    void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (life > 0) Destroy(gameObject, life);
    }

    void Update()
    {
        if (_consumed) return;

        if (!player)
            player = FindFirstObjectByType<PlayerGravityFlip>();

        if (player && IsWithinCollectionRadius())
            Collect();
    }

    void Collect()
    {
        if (_consumed) return;
        _consumed = true;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm) gm.AddWisps(amount);

        if (pickupBurst)
        {
            pickupBurst.transform.parent = null;  // detach so it can finish
            pickupBurst.Play();
            Destroy(pickupBurst.gameObject, pickupBurst.main.duration);
        }

        // Disable collider & renderer immediately to prevent re-collection
        if (pickupCollider) pickupCollider.enabled = false;
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.enabled = false;

        // Destroy after a tiny delay to allow any audio/FX to spawn safely
        Destroy(gameObject, 0.02f);
    }

    bool IsWithinCollectionRadius()
    {
        Vector2 playerCenter = (Vector2)player.transform.position + collectionOffset;
        Vector2 emberCenter = transform.position;
        float radius = Mathf.Max(0f, collectionRadius);
        return (emberCenter - playerCenter).sqrMagnitude <= radius * radius;
    }
}
