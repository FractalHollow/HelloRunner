using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [Tooltip("How far the magnet reaches (world units).")]
    public float radius = 1.5f;

    [Tooltip("How fast pickups are pulled toward the player.")]
    public float pullSpeed = 6f;

    [Tooltip("Limit the pull so pickups don't teleport.")]
    public float maxChaseSpeed = 10f;

    [Tooltip("Only affect objects with this component or layer. If left at -1, we'll just check for WispPickup by component.")]
    public LayerMask layerMask = ~0;

    // --- internals ---
    readonly Collider2D[] hits = new Collider2D[32];
    Transform self;

    void Awake()
    {
        self = transform;
        enabled = false; // stays off until an upgrade enables us
    }

    void Update()
    {
        ContactFilter2D filter = default;
        filter.NoFilter();
        if (layerMask != ~0)
            filter.SetLayerMask(layerMask);

        int count = Physics2D.OverlapCircle(self.position, radius, filter, hits);

        for (int i = 0; i < count; i++)
        {
            var col = hits[i];
            if (!col) continue;

            // Only act on pickups
            var pickup = col.GetComponent<WispPickup>();
            if (!pickup) continue;

            // Move toward the player (Rigidbody2D if present, else transform)
            Vector3 target = self.position;
            Vector3 pos = col.transform.position;
            Vector3 dir = (target - pos);
            float dist = dir.magnitude;
            if (dist < 0.001f) continue;

            dir /= dist;

            // Prefer physics if the pickup has a rigidbody
            var rb = col.attachedRigidbody;
            if (rb != null)
            {
                // accelerate toward player, but clamp speed
                Vector2 v = rb.linearVelocity + (Vector2)(dir * pullSpeed);
                if (v.magnitude > maxChaseSpeed) v = v.normalized * maxChaseSpeed;
                rb.linearVelocity = v;
            }
            else
            {
                // non-physics fallback
                col.transform.position += dir * pullSpeed * Time.deltaTime;
            }
        }
    }

    // Nice visual aid in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
