using UnityEngine;

public class PlayerGravityFlip : MonoBehaviour
{
    [Header("Feel")]
    public float gravityMagnitude = 6f;   // how strong gravity feels in either direction
    public float maxYSpeed = 18f;         // clamp vertical speed so it stays readable
    public float flipCooldown = 0.12f;    // small debounce so taps don’t spam

    [Header("FX")]
    public ParticleSystem flipFXUp;      // Optional: particle effects for flipping
    public ParticleSystem flipFXDown;

    Rigidbody2D rb;
    bool isAlive = true;
    bool canControl = true;
    int gravDir = 1;          // +1 = normal (down), -1 = inverted (up)
    float nextFlipAllowed = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityMagnitude * gravDir; // start falling down
        rb.freezeRotation = true;                     // optional: keep sprite upright
    }

    void Update()
    {
        if (!isAlive || !canControl) return;

        bool tapped = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.touchCount > 0;


        if (tapped && Time.time >= nextFlipAllowed)
        {
            gravDir *= -1;
            rb.gravityScale = gravityMagnitude * gravDir;
            rb.velocity = new Vector2(rb.velocity.x, 0f);  // crisp flip

            Debug.Log($"Flip -> gravDir={gravDir} ( >0 = down, <0 = up )");


            // PLAY FX
            var pos = transform.position;
            pos.y += (gravDir > 0 ? -0.2f : 0.2f);   // tiny offset opposite gravity

            if (gravDir > 0)    // now gravity points down (fell from ceiling)
            { if (flipFXDown) flipFXDown.Play(); }
            else                // now gravity points up
            { if (flipFXUp) flipFXUp.Play(); }

            nextFlipAllowed = Time.time + flipCooldown;


            // optional: zero vertical velocity so the flip is crisp/instant
            rb.velocity = new Vector2(rb.velocity.x, 0f);

            nextFlipAllowed = Time.time + flipCooldown;
        }

        // Clamp vertical speed so it never becomes unreadable
        var v = rb.velocity;
        if (Mathf.Abs(v.y) > maxYSpeed) v.y = Mathf.Sign(v.y) * maxYSpeed;
        rb.velocity = v;
    }

    public void EnableControl(bool value) => canControl = value;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isAlive) return;
        isAlive = false;
        FindObjectOfType<GameManager>()?.GameOver();
    }
}
