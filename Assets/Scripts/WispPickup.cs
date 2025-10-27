using UnityEngine;

public class WispPickup : MonoBehaviour
{
    public int amount = 1;
    public float life = 10f;                 // auto-despawn
    public ParticleSystem pickupBurst;       // assign in prefab

    bool _consumed = false;                  // guard against double-trigger

    void Start()
    {
        if (life > 0) Destroy(gameObject, life);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;               // avoid multi-trigger (e.g., magnet overlap)
        if (!other.CompareTag("Player")) return;

        _consumed = true;

        // IMPORTANT: Do NOT bank during run. Only track run currency here.
        // Remove/disable ANY other currency singletons for pickups.
        // Currency.I?.Add(amount);  <-- REMOVE this line

        // Add to this-run total only (banked at GameOver)
        var gm = FindObjectOfType<GameManager>();
        if (gm) gm.AddWisps(amount);

        // Play burst
        if (pickupBurst)
        {
            pickupBurst.transform.parent = null;  // detach so it can finish
            pickupBurst.Play();
            Destroy(pickupBurst.gameObject, pickupBurst.main.duration);
        }

        // Disable collider & renderer immediately to prevent re-collection
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.enabled = false;

        // Destroy after a tiny delay to allow any audio/FX to spawn safely
        Destroy(gameObject, 0.02f);
    }
}
