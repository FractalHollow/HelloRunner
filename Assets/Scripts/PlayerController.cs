using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    public float jumpVelocity = 10f;
    public float maxFallSpeed = -20f;

    Rigidbody2D rb;
    bool isAlive = true;
    bool canControl = true;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        if (!isAlive || !canControl) return;

        bool tapped = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.touchCount > 0;
        if (tapped)
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);

        if (rb.velocity.y < maxFallSpeed)
            rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
    }

    public void EnableControl(bool value) => canControl = value;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isAlive) return;
        isAlive = false;
        FindObjectOfType<GameManager>()?.GameOver();
    }
}
