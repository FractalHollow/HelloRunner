using UnityEngine;
using System.Collections;

public class PlayerShield : MonoBehaviour
{
    [Header("Runtime")]
    public int charges = 0;      // current charges this run
    public int maxCharges = 0;   // set by upgrades

    [Header("Invulnerability")]
    public float invulnDuration = 1.8f;      // seconds after absorb
    float invulnUntil = 0f;
    public bool IsInvulnerable => Time.time < invulnUntil;

    [Header("FX")]
    public GameObject shieldVisual;          // ring shown while charges > 0
    public ParticleSystem breakBurst;        // one-shot burst when a charge is used
    public AudioClip shieldBreakClip;        // SFX on absorb
    public AudioClip shieldRegenClip;
    [Range(0f, 1f)] public float shieldRegenVolume = 0.8f;


    [Header("Visual Tiers (2 vs 1 charges)")]
    public Color colorOne = new Color(0.45f, 0.9f, 1f, 0.45f);  // 1 charge
    public Color colorTwo = new Color(0.45f, 1f, 1f, 0.70f);    // 2+ charges

    [Header("Collision Ghosting")]
    public string playerLayerName = "Player";
    public string obstacleLayerName = "Obstacle";
    public string enemyProjectileLayerName = "EnemyProjectile";
    public bool freezeXDuringInvuln = true;

    AudioSource audioSrc;
    SpriteRenderer[] blinkRenderers;
    SpriteRenderer shieldSR;
    Rigidbody2D rb;
    RigidbodyConstraints2D originalConstraints;
    int playerLayer = -1, obstacleLayer = -1, enemyProjLayer = -1;

    [Header("Regen")]
    public bool regenEnabled = false;
    public float regenCooldown = 12f;
    Coroutine regenCo;
    float regenEta = -1f;   // when regen completes (used by UI + logic)

    [Header("Regen UI")]
    public UnityEngine.UI.Image regenRing;  // drag a UI Image here (type = Filled → Radial)

    [Header("Tap Bounce")]
    public bool tapBounceEnabled = true;
    [Range(0.05f, 0.3f)] public float tapBounceDuration = 0.12f;
    [Range(0.85f, 1f)] public float tapBounceCompressY = 0.97f;
    [Range(1f, 1.25f)] public float tapBounceStretchY = 1.1f;
    [Range(0f, 0.08f)] public float tapBounceXCompensation = 0.02f;
    [Range(0.1f, 1f)] public float regenRingBounceMultiplier = 0.7f;

    Coroutine shieldBounceCo;
    Coroutine regenRingBounceCo;
    Vector3 shieldBaseScale = Vector3.one;
    Vector3 regenRingBaseScale = Vector3.one;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (shieldVisual) shieldSR = shieldVisual.GetComponent<SpriteRenderer>();

        audioSrc = GetComponent<AudioSource>();
        if (!audioSrc) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;

        playerLayer    = LayerMask.NameToLayer(playerLayerName);
        obstacleLayer  = LayerMask.NameToLayer(obstacleLayerName);
        enemyProjLayer = LayerMask.NameToLayer(enemyProjectileLayerName);

        // Make sure burst doesn’t auto-fire
        if (breakBurst)
        {
            var main = breakBurst.main;
            main.loop = false;
            main.playOnAwake = false;

            var emission = breakBurst.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
        }

        CacheBounceBaseScales();
    }

    void OnEnable()
    {
        CacheBounceBaseScales();
    }

    /// Called by GM at run start
    public void SetCharges(int max)
    {
        EnableGhostCollisions(false);
        invulnUntil = 0f;
        SetAlpha(1f);
        
        maxCharges = Mathf.Max(0, max);
        charges = maxCharges;
        RefreshVisual();

        // Reset regen timer for a clean run start
        regenEta = -1f;

        // If regen is enabled and we aren't full at start (rare), begin cooldown
        BeginRegenIfNeeded();
    }

    /// Called from collision; returns true if the hit was absorbed.
    public bool TryAbsorbHit()
    {
        FindFirstObjectByType<GameManager>()?.NotifyPlayerHit();
        
        // If currently invulnerable, treat as absorbed without consuming a charge
        if (IsInvulnerable) return true;

        if (charges <= 0) return false;

        charges--;

        // 🔁 IMPORTANT FIX:
        // If we took shield damage and regen is enabled, restart the cooldown NOW.
        // This ensures a second hit mid-cooldown pushes regen back.
        if (regenEnabled && charges < maxCharges)
        {
            RestartRegenCooldown();
        }

        // SFX
        if (shieldBreakClip)
        AudioManager.I?.Play2D(shieldBreakClip, 1f);

        // Burst (single instant emit at player's position)
        if (breakBurst)
        {
            breakBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            breakBurst.transform.position = transform.position;
            breakBurst.Emit(32); // tweak 20–64 to taste
        }

        // Start i-frames (ghost collisions + blink + freeze X)
        StartInvulnerability(invulnDuration);

        // Update ring for remaining charges
        RefreshVisual();
        return true;
    }

    void RestartRegenCooldown()
    {
        // Ensure regen loop is running
        BeginRegenIfNeeded();

        // Push the regen completion time forward
        regenEta = Time.time + regenCooldown;

        // Optional: make the ring immediately show "fresh cooldown started"
        // (Since your UI currently fills 0→1, set to 0 at the start.)
        if (regenRing)
        {
            regenRing.enabled = true;
            regenRing.fillAmount = 0f;
        }
    }

    public void RefreshVisual()
    {
        if (!shieldVisual) return;

        shieldVisual.SetActive(charges > 0);

        if (shieldSR)
        {
            shieldSR.color = (charges >= 2) ? colorTwo : colorOne;
        }
    }

    public void PlayTapBounce()
    {
        if (!tapBounceEnabled)
            return;

        if (shieldVisual)
        {
            if (shieldBounceCo != null)
                StopCoroutine(shieldBounceCo);

            shieldVisual.transform.localScale = shieldBaseScale;
            shieldBounceCo = StartCoroutine(BounceScaleCo(
                shieldVisual.transform,
                shieldBaseScale,
                1f));
        }

        if (regenRing)
        {
            if (regenRingBounceCo != null)
                StopCoroutine(regenRingBounceCo);

            regenRing.rectTransform.localScale = regenRingBaseScale;
            regenRingBounceCo = StartCoroutine(BounceScaleCo(
                regenRing.rectTransform,
                regenRingBaseScale,
                Mathf.Clamp01(regenRingBounceMultiplier)));
        }
    }

    // ---------- Invulnerability/ghosting ----------

    void StartInvulnerability(float duration)
    {
        // Use the later of existing timer or new timer
        invulnUntil = Mathf.Max(invulnUntil, Time.time + duration);

        EnableGhostCollisions(true);

        if (freezeXDuringInvuln && rb)
        {
            originalConstraints = rb.constraints;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.constraints = originalConstraints | RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }

        StopCoroutineSafe("BlinkCo");
        StartCoroutine("BlinkCo");
        StopCoroutineSafe("EndInvulnCo");
        StartCoroutine("EndInvulnCo");
    }

    IEnumerator EndInvulnCo()
    {
        while (IsInvulnerable) yield return null;

        EnableGhostCollisions(false);

        if (freezeXDuringInvuln && rb)
            rb.constraints = originalConstraints;

        SetAlpha(1f);
    }

    void EnableGhostCollisions(bool on)
    {
        if (playerLayer >= 0 && obstacleLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, obstacleLayer, on);

        if (playerLayer >= 0 && enemyProjLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyProjLayer, on);
    }

    IEnumerator BlinkCo()
    {
        const float blinkHz = 10f;
        float nextFlip = 0f;
        bool visible = true;

        while (IsInvulnerable)
        {
            if (Time.time >= nextFlip)
            {
                visible = !visible;
                SetAlpha(visible ? 1f : 0.35f);
                nextFlip = Time.time + (1f / blinkHz);
            }
            yield return null;
        }

        SetAlpha(1f);
    }

    void SetAlpha(float a)
    {
        if (blinkRenderers == null) return;
        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            var r = blinkRenderers[i];
            if (!r) continue;
            if (r == shieldSR) continue;   // don’t blink the ring
            var c = r.color; c.a = a; r.color = c;
        }
    }

    void StopCoroutineSafe(string name)
    {
        try { StopCoroutine(name); } catch { }
    }

    // ---------- Regen ----------

    public void BeginRegenIfNeeded()
    {
        if (!regenEnabled)
        {
            StopRegen();
            return;
        }

        if (regenCo == null)
            regenCo = StartCoroutine(RegenLoop());
    }

    void StopRegen()
    {
        if (regenCo != null)
        {
            StopCoroutine(regenCo);
            regenCo = null;
        }
        regenEta = -1f;
    }

        public void StopAllRegen()
        {
            // Disable regen logic
            regenEnabled = false;

            // Stop regen coroutine safely
            if (regenCo != null)
            {
                StopCoroutine(regenCo);
                regenCo = null;
            }

            // Clear regen timing
            regenEta = -1f;

            // Hide and reset regen UI ring
            if (regenRing)
            {
                regenRing.enabled = false;
                regenRing.fillAmount = 0f;
                regenRing.rectTransform.localScale = regenRingBaseScale;
            }

            if (shieldVisual)
                shieldVisual.transform.localScale = shieldBaseScale;

            shieldBounceCo = null;
            regenRingBounceCo = null;
        }

    IEnumerator RegenLoop()
    {
        while (regenEnabled)
        {
            // Wait until we need a charge
            while (regenEnabled && charges >= maxCharges)
            {
                regenEta = -1f;
                yield return null;
            }
            if (!regenEnabled) break;

            // If nobody has started a cooldown yet, start one.
            if (regenEta < 0f)
                regenEta = Time.time + regenCooldown;

            // Wait until regen time is reached (regenEta can be pushed back by hits)
            while (regenEnabled && Time.time < regenEta)
                yield return null;

            if (!regenEnabled) break;

            if (charges < maxCharges)
            {
                charges++;
                RefreshVisual();

                // Regen SFX (only when a charge is actually restored)
                if (shieldRegenClip)
                AudioManager.I?.Play2D(shieldRegenClip, shieldRegenVolume);
            }

            // If still not full, schedule the next one; else clear.
            regenEta = (charges < maxCharges) ? Time.time + regenCooldown : -1f;
        }
    }

    IEnumerator BounceScaleCo(Transform target, Vector3 baseScale, float intensity)
    {
        if (!target)
            yield break;

        float duration = Mathf.Max(0.01f, tapBounceDuration);
        float phaseDuration = duration / 3f;

        float compressY = Mathf.Lerp(1f, tapBounceCompressY, intensity);
        float stretchY = Mathf.Lerp(1f, tapBounceStretchY, intensity);
        float xComp = tapBounceXCompensation * intensity;
        float compressX = 1f + xComp;
        float stretchX = Mathf.Max(0.01f, 1f - xComp);

        float t = 0f;
        while (t < phaseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / phaseDuration);
            target.localScale = ScaleFrom(baseScale,
                Mathf.Lerp(1f, compressX, k),
                Mathf.Lerp(1f, compressY, k));
            yield return null;
        }

        t = 0f;
        while (t < phaseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / phaseDuration);
            target.localScale = ScaleFrom(baseScale,
                Mathf.Lerp(compressX, stretchX, k),
                Mathf.Lerp(compressY, stretchY, k));
            yield return null;
        }

        t = 0f;
        while (t < phaseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / phaseDuration);
            target.localScale = ScaleFrom(baseScale,
                Mathf.Lerp(stretchX, 1f, k),
                Mathf.Lerp(stretchY, 1f, k));
            yield return null;
        }

        if (target)
            target.localScale = baseScale;

        if (shieldVisual && target == shieldVisual.transform)
            shieldBounceCo = null;
        else if (regenRing && target == regenRing.rectTransform)
            regenRingBounceCo = null;
    }

    Vector3 ScaleFrom(Vector3 baseScale, float xMultiplier, float yMultiplier)
    {
        return new Vector3(
            baseScale.x * xMultiplier,
            baseScale.y * yMultiplier,
            baseScale.z);
    }

    void CacheBounceBaseScales()
    {
        if (shieldVisual)
            shieldBaseScale = shieldVisual.transform.localScale;

        if (regenRing)
            regenRingBaseScale = regenRing.rectTransform.localScale;
    }

    void LateUpdate()
    {
        if (!regenRing) return;

        if (!regenEnabled || charges >= maxCharges || regenEta < 0f)
        {
            regenRing.enabled = false;
            return;
        }

        regenRing.enabled = true;

        // Your current behavior: fill increases 0 → 1 as cooldown elapses
        float elapsed01 = Mathf.Clamp01(1f - ((regenEta - Time.time) / regenCooldown));
        regenRing.fillAmount = elapsed01;
    }

    void OnDisable()
    {
        if (shieldVisual)
            shieldVisual.transform.localScale = shieldBaseScale;

        if (regenRing)
            regenRing.rectTransform.localScale = regenRingBaseScale;

        shieldBounceCo = null;
        regenRingBounceCo = null;

        // Safety: if we get disabled/destroyed mid-invuln (scene change, prestige, etc.)
        // ensure we restore collisions globally.
        EnableGhostCollisions(false);

        // Also stop invuln visuals/behavior in case we re-enable later.
        invulnUntil = 0f;
        SetAlpha(1f);

        if (freezeXDuringInvuln && rb)
            rb.constraints = originalConstraints;
    }

    void OnDestroy()
    {
        // Same safety net for destroy paths.
        EnableGhostCollisions(false);
    }

}
