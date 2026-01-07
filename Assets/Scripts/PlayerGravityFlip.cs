using UnityEngine;
using UnityEngine.EventSystems;

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
    GameManager gm;
    PlayerShield shield;

    bool isAlive = true;
    bool canControl = true;

    int gravDir = 1;          // +1 = normal (down), -1 = inverted (up)
    float nextFlipAllowed = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shield = GetComponent<PlayerShield>();
        gm = FindObjectOfType<GameManager>();

        ApplyGravityFromDir();

        if (rb)
            rb.freezeRotation = true;
    }

    void ApplyGravityFromDir()
    {
        if (!rb) return;
        rb.gravityScale = gravityMagnitude * gravDir;
    }

    void Update()
    {
        if (!isAlive || !canControl) return;

        bool tapDown =
            Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) ||
            Input.GetKeyDown(KeyCode.Space);

        if (tapDown)
        {
            if (!IsPointerOverUI() && Time.time >= nextFlipAllowed)
            {
                DoFlip(playSfx: true, playFx: true);
                nextFlipAllowed = Time.time + flipCooldown;
            }
        }

        // Clamp vertical speed so it never becomes unreadable
        if (rb)
        {
            var v = rb.linearVelocity;
            if (Mathf.Abs(v.y) > maxYSpeed) v.y = Mathf.Sign(v.y) * maxYSpeed;
            rb.linearVelocity = v;
        }
    }

    public void EnableControl(bool value) => canControl = value;

void OnCollisionEnter2D(Collision2D collision)
{
    if (!isAlive) return;

    bool isBoundary =
        collision.collider.CompareTag("Ground") ||
        collision.collider.CompareTag("Ceiling");

    // ✅ Always bounce/flip off boundaries
    if (isBoundary)
    {
        ForceFlipAndBounce(5f); // tweak bounce speed if needed

        // But still treat it as a HIT (unless invulnerable)
        if (shield && shield.IsInvulnerable)
            return;

        if (shield && shield.TryAbsorbHit())
            return;

        // No shield to absorb -> death
        isAlive = false;
        gm?.GameOver(); 
        return;
    }

    // Non-boundary hits:
    if (shield && shield.IsInvulnerable)
        return;

    if (shield && shield.TryAbsorbHit())
        return;

    isAlive = false;
    gm?.GameOver();
}


    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Mouse / editor
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        // Touch (any finger)
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;

        return false;
    }

    void DoFlip(bool playSfx, bool playFx)
    {
        // flip direction
        gravDir *= -1;
        ApplyGravityFromDir();

        // crisp flip
        if (rb)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        if (playSfx)
            AudioManager.I?.PlayFlip();

        if (playFx)
        {
            if (gravDir > 0)
            { if (flipFXDown) flipFXDown.Play(); } // now gravity down
            else
            { if (flipFXUp) flipFXUp.Play(); }     // now gravity up
        }
    }

    public void ForceFlipAndBounce(float bounceSpeed = 7f)
    {
        if (!rb) return;

        // Flip using the same logic as tap flips so gravDir stays in sync
        DoFlip(playSfx: false, playFx: true);

        // Push away from the surface (opposite current gravity direction)
        // If gravity points down (gravDir = +1), we want an upward velocity (negative y).
        float awayFromGravityY = -gravDir * bounceSpeed;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, awayFromGravityY);

        // Optional: prevent instant re-flip spam
        nextFlipAllowed = Time.time + flipCooldown;
    }

    public void ResetState()
    {
        isAlive = true;
        canControl = false; // StartGame will enable control

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
