using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public float startInterval = 1.6f;
    public float minInterval = 0.8f;
    public float difficultyRamp = 0.02f;
    public float minY = -2f, maxY = 2f;
    public bool allowVerticalMovement;
    public bool allowProjectiles;
    float currentInterval;
    bool spawning;
    Coroutine loop;

    void Awake()
    {
        currentInterval = startInterval;
    }

    public void Begin()
    {
         var gm = FindObjectOfType<GameManager>();
      //  allowVerticalMovement = gm && gm.mod_EnemyVerticalMovement;
      //  allowProjectiles      = gm && gm.mod_EnemyProjectiles;
    
        currentInterval = startInterval;   // reset difficulty each run
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
            Vector3 pos = new Vector3(transform.position.x, Random.Range(minY, maxY), 0f);
            Instantiate(obstaclePrefab, pos, Quaternion.identity);
            
            // Enable optional behaviors
           // if (allowVerticalMovement)
          //  {
           //     var bob = enemy.GetComponent<EnemyVerticalBob2D>();
          //      if (bob) bob.enabled = true;
          //  }
         //   if (allowProjectiles)
          //  {
          //      var shoot = enemy.GetComponent<EnemyShooter2D>();
          //      if (shoot) shoot.enabled = true;
          //  }

            
            yield return new WaitForSeconds(currentInterval);
            currentInterval = Mathf.Max(minInterval, currentInterval - difficultyRamp);
           
        }
    }
}
