using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerGravityFlip : MonoBehaviour
{
    [Header("Feel")]
    public float gravityMagnitude = 7f;   // how strong gravity feels in either direction
    public float maxYSpeed = 18f;         // clamp vertical speed so it stays readable
    public float flipCooldown = 0.08f;    // small debounce so taps don’t spam

    [Header("FX")]
    public ParticleSystem flipFXUp;      // Optional: particle effects for flipping
    public ParticleSystem flipFXDown;

    Rigidbody2D rb;
    GameManager gm;
    PlayerShield shield;
    PlayerSpriteAnimator spriteAnimator;

    bool isAlive = true;
    bool canControl = true;

    int gravDir = 1;          // +1 = normal (down), -1 = inverted (up)
    float nextFlipAllowed = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shield = GetComponent<PlayerShield>();
        gm = FindObjectOfType<GameManager>();
        spriteAnimator = GetComponentInChildren<PlayerSpriteAnimator>(true);

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
        if (!isAlive || !canControl)
            {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                Debug.Log($"[Flip] Input ignored. isAlive={isAlive}, canControl={canControl}");
            return;
            }

            bool tapDown = false;

                if (Input.touchCount > 0)
                {
                    tapDown = Input.GetTouch(0).phase == TouchPhase.Began;
                }
                else
                {
                    tapDown = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
                }

        if (tapDown)
        {
            bool overUI = IsPointerOverUI();
            bool cooldownReady = Time.time >= nextFlipAllowed;

            if (!overUI && cooldownReady)
            {
                DoFlip(playSfx: true, playFx: true);
                nextFlipAllowed = Time.time + flipCooldown;
            }
            else
            {
                Debug.Log($"[Flip] tap blocked | overUI={overUI} cooldownReady={cooldownReady} touchCount={Input.touchCount}");
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

    public void EnableControl(bool value)
        {
            canControl = value;
            Debug.Log($"[Flip] EnableControl({value})");
        }

void OnCollisionEnter2D(Collision2D collision)
{
    if (!isAlive) return;

    bool isBoundary =
        collision.collider.CompareTag("Ground") ||
        collision.collider.CompareTag("Ceiling");

    // Always bounce/flip off boundaries
    if (isBoundary)
    {
        ForceFlipAndBounce(collision,6f); // tweak bounce speed if needed

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
        gm?.NotifyFlip();
        spriteAnimator?.PlayJump();

        // flip direction
        gravDir *= -1;
        ApplyGravityFromDir();

        // crisp flip
        if (rb)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        if (playSfx)
            if (AudioManager.I) AudioManager.I.PlayFlip();

        if (playFx)
        {
            if (gravDir > 0)
            { if (flipFXDown) flipFXDown.Play(); } // now gravity down
            else
            { if (flipFXUp) flipFXUp.Play(); }     // now gravity up
        }
    }

    public void SetFlipFxColor(Color c)
    {
        ApplyColor(flipFXUp, c);
        ApplyColor(flipFXDown, c);
    }

    static void ApplyColor(ParticleSystem ps, Color c)
    {
        if (!ps) return;

        // Apply to this particle system + any child particle systems (common setup)
        var systems = ps.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var main = systems[i].main;

            // Preserve original alpha if you want (optional)
            var current = main.startColor.color;
            c.a = current.a;

            main.startColor = c;
        }
    }

    public void ForceFlipAndBounce(Collision2D collision, float bounceSpeed = 7f)
        {
            if (!rb) return;

            // Use contact normal so bounce is deterministic (no accidental double-flip)
            Vector2 n = collision.GetContact(0).normal; // floor ~ up, ceiling ~ down

            // If we hit floor (normal.y > 0), gravity should become UP (-1).
            // If we hit ceiling (normal.y < 0), gravity should become DOWN (+1).
            gravDir = (n.y > 0f) ? -1 : 1;
            ApplyGravityFromDir();

            gm?.NotifyFlip();

            // Bounce away from the surface
            float y = (n.y > 0f) ? Mathf.Abs(bounceSpeed) : -Mathf.Abs(bounceSpeed);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, y);

            // Optional: block instant tap re-flip on the same impact frame
            nextFlipAllowed = Time.time + flipCooldown;
        }

    public void ResetState()
    {
        isAlive = true;
        canControl = false; // StartGame will enable control
        Debug.Log("[Flip] ResetState() -> canControl = false");
        spriteAnimator?.ShowIdleImmediate();

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
