using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public float startInterval = 1.6f;
    public float minInterval = 0.8f;
    public float difficultyRamp = 0.02f;
    public float minY = -2f, maxY = 2f;

    private float currentInterval;
    private bool spawning;

    void Start()
    {
        currentInterval = startInterval;
        Begin();
    }

    public void Begin()
    {
        if (spawning) return;
        spawning = true;
        StartCoroutine(SpawnLoop());
    }

    public void StopSpawning() => spawning = false;

    IEnumerator SpawnLoop()
    {
        while (spawning)
        {
            Vector3 pos = new Vector3(transform.position.x, Random.Range(minY, maxY), 0f);
            Instantiate(obstaclePrefab, pos, Quaternion.identity);
            yield return new WaitForSeconds(currentInterval);
            currentInterval = Mathf.Max(minInterval, currentInterval - difficultyRamp);
        }
    }
}
