using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerGravityFlip : MonoBehaviour
{
    [Header("Feel")]
    public float gravityMagnitude = 3.5f;   // how strong gravity feels in either direction
    public float maxYSpeed = 18f;         // clamp vertical speed so it stays readable
    public float flipCooldown = 0.08f;    // small debounce so taps do not spam
    public float flipInputBuffer = 0.12f; // keep short taps alive through brief cooldown/impact windows

    [Header("FX")]
    public ParticleSystem flipFXUp;       // Optional: particle effects for flipping
    public ParticleSystem flipFXDown;

    [Header("Debug")]
    public bool logBlockedTapDetails = false;

    Rigidbody2D rb;
    GameManager gm;
    PlayerShield shield;
    PlayerSpriteAnimator spriteAnimator;
    readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    bool isAlive = true;
    bool canControl = true;

    int gravDir = 1; // +1 = normal (down), -1 = inverted (up)
    float nextFlipAllowed = 0f;
    float bufferedFlipUntil = -1f;
    string lastBlockedTapDebug = "";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shield = GetComponent<PlayerShield>();
        gm = FindFirstObjectByType<GameManager>();
        spriteAnimator = GetComponentInChildren<PlayerSpriteAnimator>(true);

        ApplyGravityFromDir();

        if (rb)
            rb.freezeRotation = true;
    }

    void ApplyGravityFromDir()
    {
        if (!rb) return;
        rb.gravityScale = EffectiveGravityMagnitude() * gravDir;
    }

    float FoxVerticalPrestigeMultiplier()
    {
        return gm ? gm.FoxVerticalPrestigeMultiplier : 1f;
    }

    float EffectiveGravityMagnitude()
    {
        return gravityMagnitude * FoxVerticalPrestigeMultiplier();
    }

    float EffectiveMaxYSpeed()
    {
        return maxYSpeed * FoxVerticalPrestigeMultiplier();
    }

    void Update()
    {
        if (!isAlive || !canControl)
        {
            if (Input.GetMouseButtonDown(0) || HasAnyTouchBegan())
                Debug.Log($"[Flip] Input ignored. isAlive={isAlive}, canControl={canControl}");
            return;
        }

        TryQueueFlipInput(out bool tapBlockedByUi);
        if (tapBlockedByUi)
            Debug.Log(logBlockedTapDetails
                ? $"[Flip] tap blocked by UI | {lastBlockedTapDebug}"
                : $"[Flip] tap blocked by UI | touchCount={Input.touchCount}");

        bool flipBuffered = bufferedFlipUntil >= Time.time;
        bool cooldownReady = Time.time >= nextFlipAllowed;

        if (flipBuffered && cooldownReady)
        {
            DoFlip(playSfx: true, playFx: true);
            nextFlipAllowed = Time.time + flipCooldown;
            bufferedFlipUntil = -1f;
        }

        // Clamp vertical speed so it never becomes unreadable
        if (rb)
        {
            var v = rb.linearVelocity;
            float effectiveMaxYSpeed = EffectiveMaxYSpeed();
            if (Mathf.Abs(v.y) > effectiveMaxYSpeed) v.y = Mathf.Sign(v.y) * effectiveMaxYSpeed;
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
            ForceFlipAndBounce(collision, 6f); // tweak bounce speed if needed

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

    bool TryQueueFlipInput(out bool blockedByUi)
    {
        blockedByUi = false;
        lastBlockedTapDebug = "";

        if (Input.touchCount > 0)
        {
            bool sawTouchBegan = false;
            bool sawBlockedTouch = false;
            bool touchBeganOnGameplay = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                sawTouchBegan = true;

                if (IsTouchOverUi(touch.fingerId, touch.position, out GameObject blockedObject))
                {
                    sawBlockedTouch = true;
                    if (logBlockedTapDetails && string.IsNullOrEmpty(lastBlockedTapDebug))
                        lastBlockedTapDebug = BuildBlockedTapDebug("touch", touch.fingerId, touch.position, blockedObject);

                    continue;
                }

                touchBeganOnGameplay = true;
            }

            if (touchBeganOnGameplay)
            {
                QueueBufferedFlip();
                return true;
            }

            if (sawTouchBegan && sawBlockedTouch)
            {
                blockedByUi = true;
                if (logBlockedTapDetails && string.IsNullOrEmpty(lastBlockedTapDebug))
                    lastBlockedTapDebug = BuildBlockedTapDebug("touch", -1, Vector2.zero, null);

                return false;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsMousePointerOverUi(out GameObject blockedObject))
            {
                blockedByUi = true;
                if (logBlockedTapDetails)
                    lastBlockedTapDebug = BuildBlockedTapDebug("mouse", -1, Input.mousePosition, blockedObject);

                return false;
            }

            QueueBufferedFlip();
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            QueueBufferedFlip();
            return true;
        }

        return false;
    }

    void QueueBufferedFlip()
    {
        bufferedFlipUntil = Mathf.Max(bufferedFlipUntil, Time.time + Mathf.Max(0f, flipInputBuffer));
    }

    bool HasAnyTouchBegan()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }

    bool IsMousePointerOverUi(out GameObject blockedObject)
    {
        blockedObject = null;
        bool screenHit = IsScreenPositionOverUi(Input.mousePosition, out blockedObject);
        return EventSystem.current != null &&
               (EventSystem.current.IsPointerOverGameObject() || screenHit);
    }

    bool IsTouchOverUi(int fingerId, Vector2 screenPosition, out GameObject blockedObject)
    {
        blockedObject = null;
        bool screenHit = IsScreenPositionOverUi(screenPosition, out blockedObject);
        return EventSystem.current != null &&
               (EventSystem.current.IsPointerOverGameObject(fingerId) || screenHit);
    }

    bool IsScreenPositionOverUi(Vector2 screenPosition, out GameObject blockedObject)
    {
        blockedObject = null;
        if (EventSystem.current == null)
            return false;

        uiRaycastResults.Clear();
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        EventSystem.current.RaycastAll(eventData, uiRaycastResults);
        if (uiRaycastResults.Count > 0)
            blockedObject = uiRaycastResults[0].gameObject;

        return uiRaycastResults.Count > 0;
    }

    string BuildBlockedTapDebug(string source, int fingerId, Vector2 screenPosition, GameObject blockedObject)
    {
        return
            $"source={source} fingerId={fingerId} position={screenPosition} " +
            $"blockedBy={GetObjectPath(blockedObject)} touchCount={Input.touchCount} " +
            $"cooldownReady={Time.time >= nextFlipAllowed} buffered={bufferedFlipUntil >= Time.time} " +
            $"isAlive={isAlive} canControl={canControl}";
    }

    static string GetObjectPath(GameObject go)
    {
        if (!go) return "UNKNOWN";

        Transform current = go.transform;
        string path = current.name;
        while (current.parent)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    void DoFlip(bool playSfx, bool playFx)
    {
        gm?.NotifyFlip();
        spriteAnimator?.PlayJump();
        shield?.PlayTapBounce();

        // flip direction
        gravDir *= -1;
        ApplyGravityFromDir();

        // crisp flip
        if (rb)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        if (playSfx && AudioManager.I)
            AudioManager.I.PlayFlip();

        if (playFx)
        {
            if (gravDir > 0)
            {
                if (flipFXDown) flipFXDown.Play(); // now gravity down
            }
            else
            {
                if (flipFXUp) flipFXUp.Play(); // now gravity up
            }
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
        gravDir = 1;
        ApplyGravityFromDir();
        bufferedFlipUntil = -1f;
        nextFlipAllowed = 0f;
        Debug.Log("[Flip] ResetState() -> canControl = false");
        spriteAnimator?.ShowIdleImmediate();

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
