using UnityEngine;
using System.Collections;

public class WispSpawner : MonoBehaviour
{
    public GameObject wispPrefab;

    [Header("Where to spawn")]
    public float spawnX = 9f;                     // just off-screen to the right
    public Vector2 yBounds = new Vector2(-3.5f, 3.5f);
    public bool clampToSafeZone = true;

    [Header("When to spawn")]
    public Vector2 intervalRange = new Vector2(0.8f, 1.8f);

    [Header("Motion")]
    public float baseMoveSpeed = 5f;              // renamed for clarity

    Coroutine loop;
    GameManager gm;

    void Awake()
    {
        gm = FindObjectOfType<GameManager>();
    }

    public void StartSpawning()
    {
        if (loop == null && wispPrefab != null)
            loop = StartCoroutine(Loop());
    }

    public void StopSpawning()
    {
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }

    IEnumerator Loop()
    {
        while (true)
        {
            // scale spawn timing by current speed multiplier
            float mult = (gm ? gm.RunSpeedMultiplier : 1f);
            float wait = Random.Range(intervalRange.x, intervalRange.y) / Mathf.Max(0.0001f, mult);
            yield return new WaitForSeconds(wait);

            float y = Random.Range(yBounds.x, yBounds.y);
            var pos = new Vector3(spawnX, y, 0f);
            var go  = Instantiate(wispPrefab, pos, Quaternion.identity);

            // move wisps leftward at world speed
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb)
            {
                float speed = baseMoveSpeed * mult;
                rb.linearVelocity = Vector2.left * speed;
            }
        }
    }
}
