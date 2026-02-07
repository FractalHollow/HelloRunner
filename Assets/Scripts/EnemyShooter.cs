using UnityEngine;
using System.Collections;

public class EnemyShooter : MonoBehaviour
{
    [Header("Wiring")]
    public Transform firePoint;              // optional; auto-creates if null
    public GameObject projectilePrefab;      // PF_EnemyProjectile

    [Header("Timing")]
    public float intervalMin = 1.2f;
    public float intervalMax = 2.0f;

    [Header("Projectile")]
    public float projectileSpeed = 8f;
    public bool aimAtPlayerY = true;        // if false -> straight left

    [Header("Debug")]
    public bool debugLogs = false;
    public Color gizmoColor = Color.red;

    [Header("SFX")]
    public AudioClip launchClip;
    [Range(0f,1f)] public float launchVol = 0.8f;

    [Header("Projectile Rotation")]
    public float projectileAngleOffset = 0f; // try 0, 90, -90, 180

    GameManager gm;
    Transform player;
    Coroutine loop;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        var p = FindFirstObjectByType<PlayerGravityFlip>();
        player = p ? p.transform : null;

        // Auto-create a firePoint if none assigned
        if (!firePoint)
        {
            var fp = new GameObject("firePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(-0.5f, 0f, 0f); // slightly left of enemy
            firePoint = fp.transform;
        }
    }

    void OnEnable()
    {
        if (loop == null) loop = StartCoroutine(FireLoop());
    }

    void OnDisable()
    {
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }

    IEnumerator FireLoop()
    {
        // small delay so spawns settle
        yield return new WaitForSeconds(0.25f);

        while (true)
        {
            // Only when Hazards is ON
            if (gm && gm.ModHazardsOn)
            {
                FireOnce();
            }

            float wait = Random.Range(intervalMin, intervalMax);
            yield return new WaitForSeconds(wait);
        }
    }

    void FireOnce()
    {
        if (!projectilePrefab || !firePoint) return;

        // Direction
        Vector2 dir = Vector2.left; // default
        if (aimAtPlayerY && player)
        {
            Vector2 to = new Vector2(firePoint.position.x - 10f, player.position.y) - (Vector2)firePoint.position;
            if (to.sqrMagnitude > 0.0001f) dir = to.normalized;
        }

        // Spawn
        var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        var proj = go.GetComponent<Projectile2D>();
        if (proj)
        {
            proj.dir = dir;
            proj.speed = projectileSpeed;
        }

        var s = go.transform.localScale;
        go.transform.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), s.z);

        // Rotate to face travel direction
        if (dir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle + projectileAngleOffset);
        }


        if (launchClip)
            {
                AudioManager.I?.PlayShoot(launchClip, launchVol);
            }

    }

    void OnDrawGizmosSelected()
    {
        if (!firePoint) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(firePoint.position, 0.06f);
        Gizmos.DrawLine(firePoint.position, firePoint.position + Vector3.left * 0.75f);
    }
}
