using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float speed = 3f;
    public float destroyX = -15f;

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }
}
