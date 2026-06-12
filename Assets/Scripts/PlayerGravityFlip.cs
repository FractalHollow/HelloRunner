using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerGravityFlip : MonoBehaviour
{
    public event System.Action InputFlipPerformed;
    public event System.Action BoundaryHit;

    [Header("Feel")]
    public float gravityMagnitude = 3.5f;   // how strong gravity feels in either direction
    public float maxYSpeed = 18f;         // clamp vertical speed so it stays readable
    public float flipCooldown = 0.08f;    // small debounce so taps do not spam
    public float flipInputBuffer = 0.18f; // keep short taps alive through brief cooldown/impact windows

    [Header("FX")]
    public ParticleSystem flipFXUp;       // Optional: particle effects for flipping
    public ParticleSystem flipFXDown;

    [Header("Debug")]
    [FormerlySerializedAs("logBlockedTapDetails")]
    [Tooltip("Logs accepted, buffered, executed, blocked, and disabled gameplay input in development builds.")]
    public bool logInputDiagnostics = false;

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
    float nextFallbackTutorialHitFeedbackAt;
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
                LogInputDiagnostic($"ignored | isAlive={isAlive} canControl={canControl}");
            return;
        }

        TryQueueFlipInput(out bool tapBlockedByUi);
        if (tapBlockedByUi)
            LogInputDiagnostic($"blocked by UI | {lastBlockedTapDebug}");

        float now = Time.unscaledTime;
        bool flipBuffered = bufferedFlipUntil >= now;
        bool cooldownReady = now >= nextFlipAllowed;

        if (flipBuffered && cooldownReady)
        {
            DoFlip(playSfx: true, playFx: true);
            nextFlipAllowed = now + Mathf.Max(0f, flipCooldown);
            bufferedFlipUntil = -1f;
            LogInputDiagnostic("executed");
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
        if (!value)
            ClearBufferedFlip();

        LogInputDiagnostic($"control={value}");
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
            BoundaryHit?.Invoke();

            if (TryHandleTutorialProtectedHit())
                return;

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
        if (TryHandleTutorialProtectedHit())
            return;

        if (shield && shield.IsInvulnerable)
            return;

        if (shield && shield.TryAbsorbHit())
            return;

        isAlive = false;
        gm?.GameOver();
    }

    public bool TryHandleTutorialProtectedHit()
    {
        if (!gm || !gm.IsInteractiveTutorialActive)
            return false;

        bool startedFeedback;
        if (shield)
        {
            startedFeedback = shield.PlayTutorialHitFeedback();
        }
        else
        {
            startedFeedback = Time.unscaledTime >= nextFallbackTutorialHitFeedbackAt;
            if (startedFeedback)
                nextFallbackTutorialHitFeedbackAt = Time.unscaledTime + 0.8f;
        }

        if (startedFeedback)
        {
            gm.NotifyPlayerHit();
            AudioManager.I?.PlayCrash();
        }

        return true;
    }

    public void StopTutorialHitFeedback()
    {
        nextFallbackTutorialHitFeedbackAt = 0f;
        shield?.StopTutorialHitFeedback();
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

                if (IsScreenPositionOverBlockingUi(touch.position, out GameObject blockedObject))
                {
                    sawBlockedTouch = true;
                    if (string.IsNullOrEmpty(lastBlockedTapDebug))
                        lastBlockedTapDebug = BuildBlockedTapDebug("touch", touch.fingerId, touch.position, blockedObject);

                    continue;
                }

                touchBeganOnGameplay = true;
            }

            if (touchBeganOnGameplay)
            {
                QueueBufferedFlip("touch");
                return true;
            }

            if (sawTouchBegan && sawBlockedTouch)
            {
                blockedByUi = true;
                if (string.IsNullOrEmpty(lastBlockedTapDebug))
                    lastBlockedTapDebug = BuildBlockedTapDebug("touch", -1, Vector2.zero, null);

                return false;
            }

        }

        // Do not process Unity's emulated mouse event while a real touch is active.
        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
        {
            if (IsScreenPositionOverBlockingUi(Input.mousePosition, out GameObject blockedObject))
            {
                blockedByUi = true;
                lastBlockedTapDebug = BuildBlockedTapDebug("mouse", -1, Input.mousePosition, blockedObject);

                return false;
            }

            QueueBufferedFlip("mouse");
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            QueueBufferedFlip("keyboard");
            return true;
        }

        return false;
    }

    void QueueBufferedFlip(string source)
    {
        float now = Time.unscaledTime;
        if (bufferedFlipUntil >= now)
        {
            LogInputDiagnostic($"accepted {source} | pending flip already queued");
            return;
        }

        bufferedFlipUntil = now + Mathf.Max(0f, flipInputBuffer);
        LogInputDiagnostic($"accepted {source} | bufferedUntil={bufferedFlipUntil:0.000}");
    }

    void ClearBufferedFlip()
    {
        bufferedFlipUntil = -1f;
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

    bool IsScreenPositionOverBlockingUi(Vector2 screenPosition, out GameObject blockedObject)
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
        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject hitObject = uiRaycastResults[i].gameObject;
            if (!IsBlockingGameplayInput(hitObject))
                continue;

            blockedObject = hitObject;
            return true;
        }

        return false;
    }

    static bool IsBlockingGameplayInput(GameObject hitObject)
    {
        if (!hitObject)
            return false;

        Selectable selectable = hitObject.GetComponentInParent<Selectable>(true);
        if (selectable && selectable.gameObject.activeInHierarchy)
            return true;

        if (HasPointerOrDragHandler(hitObject))
            return true;

        for (Transform current = hitObject.transform; current; current = current.parent)
        {
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group && group.isActiveAndEnabled && group.interactable && group.blocksRaycasts)
                return true;
        }

        return false;
    }

    static bool HasPointerOrDragHandler(GameObject hitObject)
    {
        return ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject) != null ||
               ExecuteEvents.GetEventHandler<IPointerDownHandler>(hitObject) != null ||
               ExecuteEvents.GetEventHandler<IPointerUpHandler>(hitObject) != null ||
               ExecuteEvents.GetEventHandler<IBeginDragHandler>(hitObject) != null ||
               ExecuteEvents.GetEventHandler<IDragHandler>(hitObject) != null ||
               ExecuteEvents.GetEventHandler<IEndDragHandler>(hitObject) != null ||
               ExecuteEvents.GetEventHandler<IScrollHandler>(hitObject) != null;
    }

    string BuildBlockedTapDebug(string source, int fingerId, Vector2 screenPosition, GameObject blockedObject)
    {
        return
            $"source={source} fingerId={fingerId} position={screenPosition} " +
            $"blockedBy={GetObjectPath(blockedObject)} touchCount={Input.touchCount} " +
            $"cooldownReady={Time.unscaledTime >= nextFlipAllowed} buffered={bufferedFlipUntil >= Time.unscaledTime} " +
            $"isAlive={isAlive} canControl={canControl}";
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    void LogInputDiagnostic(string message)
    {
        if (logInputDiagnostics)
            Debug.Log($"[Flip] {message}");
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

        InputFlipPerformed?.Invoke();
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
        nextFlipAllowed = Time.unscaledTime + Mathf.Max(0f, flipCooldown);
    }

    public void ResetState()
    {
        isAlive = true;
        canControl = false; // StartGame will enable control
        gravDir = 1;
        ApplyGravityFromDir();
        ClearBufferedFlip();
        nextFlipAllowed = 0f;
        nextFallbackTutorialHitFeedbackAt = 0f;
        LogInputDiagnostic("reset | control=false");
        spriteAnimator?.ShowIdleImmediate();

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
