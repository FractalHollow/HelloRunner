using UnityEngine;
using System.Collections;

public class EnemyShooter : MonoBehaviour
{
    public Transform firePoint;     // empty child, slightly in front of the enemy
    public GameObject projectilePrefab;
    public float intervalMin = 1.2f;
    public float intervalMax = 2.0f;
    public float projectileSpeed = 8f;
    public bool aimAtPlayerY = true;  // vertical lead

    GameManager gm;
    Transform player;

    void Awake()
    {
        gm = FindObjectOfType<GameManager>();
        var p = FindObjectOfType<PlayerGravityFlip>();
        player = p ? p.transform : null;
    }

    void OnEnable()
    {
        StartCoroutine(FireLoop());
    }

    IEnumerator FireLoop()
    {
        // Only fire when hazards mod is ON
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervalMin, intervalMax));

            if (!(gm && gm.ModHazardsOn)) continue;
            if (!firePoint || !projectilePrefab) continue;

            Vector2 dir = Vector2.left;
            if (aimAtPlayerY && player)
            {
                var v = (new Vector2(firePoint.position.x - 10f, player.position.y) - (Vector2)firePoint.position);
                dir = v.normalized.sqrMagnitude < 0.001f ? Vector2.left : v.normalized;
            }

            var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            var proj = go.GetComponent<Projectile2D>();
            if (proj)
            {
                proj.dir = dir;
                proj.speed = projectileSpeed;
            }
        }
    }
}
