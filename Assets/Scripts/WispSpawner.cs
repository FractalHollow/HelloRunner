using UnityEngine;
using System.Collections;

public class WispSpawner : MonoBehaviour
{
    public GameObject wispPrefab;

    [Header("Where to spawn")]
    public float spawnX = 9f;           // just off-screen to the right
    public Vector2 yBounds = new Vector2(-3.5f, 3.5f); // between floor/ceiling
    public bool clampToSafeZone = true; // keep away from extreme edges

    [Header("When to spawn")]
    public Vector2 intervalRange = new Vector2(0.8f, 1.8f);

    [Header("Motion")]
    public float moveSpeed = 5f;        // match obstacle/world speed

    Coroutine loop;

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
            yield return new WaitForSeconds(Random.Range(intervalRange.x, intervalRange.y));

            float y = Random.Range(yBounds.x, yBounds.y);
            var pos = new Vector3(spawnX, y, 0f);
            var go  = Instantiate(wispPrefab, pos, Quaternion.identity);

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb) rb.velocity = Vector2.left * moveSpeed;

        }
    }
}
