using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public GameObject obstaclePrefab;

    [Header("Timing")]
    public float startInterval = 1.6f;
    public float minInterval   = 0.8f;
    public float difficultyRamp = 0.02f; // reduces base interval each spawn

    [Header("Spawn Area")]
    public float minY = -2f, maxY = 2f;

    [Header("Optional Behaviors")]
    public bool allowVerticalMovement = false; // if your enemies have a bob script
    public bool respectHazardsToggle  = true;  // enable EnemyShooter when Hazards is ON

    float currentBaseInterval;
    bool spawning;
    Coroutine loop;

    GameManager gm; // ⬅ cache

    void Awake()
    {
        currentBaseInterval = startInterval;
        gm = FindObjectOfType<GameManager>();
    }

    public void Begin()
    {
        // Reset difficulty each run
        currentBaseInterval = startInterval;

        if (spawning) return;
        spawning = true;
        loop = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        spawning = false;
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }

    IEnumerator SpawnLoop()
    {
        while (spawning)
        {
            // Spawn
            Vector3 pos = new Vector3(transform.position.x, Random.Range(minY, maxY), 0f);
            var enemy = Instantiate(obstaclePrefab, pos, Quaternion.identity);

            // Optional: enable per-enemy behaviors
            //if (allowVerticalMovement)
            //{
            //    var bob = enemy.GetComponent<EnemyVerticalBob2D>();
            //    if (bob) bob.enabled = true;
            //}

            if (respectHazardsToggle)
            {
                var shooter = enemy.GetComponent<EnemyShooter>();
                if (shooter)
                {
                    // Hazards toggle controls whether shooters actually fire
                    // (EnemyShooter also checks gm.ModHazardsOn internally, but we gate here too)
                    shooter.enabled = (gm && gm.ModHazardsOn);
                }
            }

            // Wait for next spawn; scale by speed so density feels similar
            float effective = EffectiveInterval(currentBaseInterval);
            yield return new WaitForSeconds(effective);

            // Ramp difficulty on the BASE interval (effective will be recomputed next loop)
            currentBaseInterval = Mathf.Max(minInterval, currentBaseInterval - difficultyRamp);
        }
    }

    // Faster world speed => shorter wait between spawns
    float EffectiveInterval(float baseInterval)
    {
        float mult = (gm ? gm.RunSpeedMultiplier : 1f);
        return baseInterval / Mathf.Max(0.0001f, mult);
    }
}
