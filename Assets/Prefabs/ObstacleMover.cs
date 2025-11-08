using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float baseSpeed = 3f;   // renamed for clarity
    public float destroyX = -15f;

    GameManager gm;

    void Awake()
    {
        gm = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        float mult = (gm ? gm.RunSpeedMultiplier : 1f);
        float currentSpeed = baseSpeed * mult;

        transform.position += Vector3.left * currentSpeed * Time.deltaTime;

        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }
}
