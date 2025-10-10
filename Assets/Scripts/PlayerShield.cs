using UnityEngine;
using System.Collections;

public class PlayerShield : MonoBehaviour
{
    [Header("Runtime")]
    public int charges = 0;      // current charges this run
    public int maxCharges = 0;   // set by upgrades

    [Header("Invulnerability")]
    public float invulnDuration = 0.5f;      // seconds after absorb
    float invulnUntil = 0f;
    public bool IsInvulnerable => Time.time < invulnUntil;

    [Header("FX")]
    public GameObject shieldVisual;          // ring shown while charges > 0
    public ParticleSystem breakBurst;        // one-shot burst when a charge is used
    public AudioClip shieldBreakClip;        // SFX on absorb

    [Header("Visual Tiers (2 vs 1 charges)")]
    public Color colorOne = new Color(0.45f, 0.9f, 1f, 0.45f);  // 1 charge
    public Color colorTwo = new Color(0.45f, 1f, 1f, 0.70f);    // 2+ charges

    [Header("Collision Ghosting")]
    public string playerLayerName = "Player";
    public string obstacleLayerName = "Obstacle";
    public bool freezeXDuringInvuln = true;

    AudioSource audioSrc;
    SpriteRenderer[] blinkRenderers;
    SpriteRenderer shieldSR;
    Rigidbody2D rb;
    RigidbodyConstraints2D originalConstraints;
    int playerLayer = -1, obstacleLayer = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        //cache the ring SR and all child SRs for blinking
        blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (shieldVisual) shieldSR = shieldVisual.GetComponent<SpriteRenderer>();

        audioSrc = GetComponent<AudioSource>();
        if (!audioSrc) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;

        blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (shieldVisual) shieldSR = shieldVisual.GetComponent<SpriteRenderer>();

        playerLayer   = LayerMask.NameToLayer(playerLayerName);
        obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);

        // Make sure burst doesn’t auto-fire
if (breakBurst)
{
    var main = breakBurst.main;
    main.loop = false;
    main.playOnAwake = false;

    var emission = breakBurst.emission;
    emission.rateOverTime = 0f;

    // Replace bursts with an empty array (not null)
    emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>()); // ✅

    // (Optional belt-and-suspenders: clear any already-configured bursts)
    // if (emission.burstCount > 0) emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
}

    }

    /// Called by GM at run start
    public void SetCharges(int max)
    {
        maxCharges = Mathf.Max(0, max);
        charges    = maxCharges;
        RefreshVisual();
    }

    /// Called from collision; returns true if the hit was absorbed.
    public bool TryAbsorbHit()
    {
        if (charges <= 0) return false;

        charges--;

        // SFX
        if (shieldBreakClip && audioSrc) audioSrc.PlayOneShot(shieldBreakClip);

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

    public void RefreshVisual()
    {
        if (!shieldVisual) return;

        shieldVisual.SetActive(charges > 0);

        if (shieldSR)
        {
            // 2+ charges = brighter/less transparent; 1 charge = dimmer/more transparent
            shieldSR.color = (charges >= 2) ? colorTwo : colorOne;
        }
    }

    // ---------- Invulnerability/ghosting ----------

    void StartInvulnerability(float duration)
    {
        invulnUntil = Time.time + duration;

        // Ignore collisions with obstacles
        EnableGhostCollisions(true);

        // Freeze X to prevent physics shove
        if (freezeXDuringInvuln && rb)
        {
            originalConstraints = rb.constraints;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // zero horizontal push
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

        // Restore collisions
        EnableGhostCollisions(false);

        // Restore constraints
        if (freezeXDuringInvuln && rb)
            rb.constraints = originalConstraints;

        SetAlpha(1f); // ensure visible at end
    }

    void EnableGhostCollisions(bool on)
    {
        if (playerLayer >= 0 && obstacleLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, obstacleLayer, on);
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
        if (r == shieldSR) continue;   // <-- don’t blink the ring
        var c = r.color; c.a = a; r.color = c;
    }
}


    void StopCoroutineSafe(string name)
    {
        try { StopCoroutine(name); } catch { }
    }
}
