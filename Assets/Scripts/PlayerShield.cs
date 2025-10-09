using UnityEngine;
using System.Collections;

public class PlayerShield : MonoBehaviour
{
    [Header("Runtime")]
    public int charges = 0;      // current charges this run
    public int maxCharges = 0;   // set by upgrades

    [Header("Invulnerability")]
    public float invulnDuration = 0.5f;  // seconds after absorb
    float invulnUntil = 0f;
    public bool IsInvulnerable => Time.time < invulnUntil;

    [Header("FX")]
    public GameObject shieldVisual;      // ring shown while charges > 0
    public ParticleSystem breakBurst;    // one-shot burst when a charge is used
    public AudioClip shieldBreakClip;    // SFX on absorb
    AudioSource audioSrc;
    SpriteRenderer[] blinkRenderers;

    void Awake()
    {
        audioSrc = GetComponent<AudioSource>();
        if (!audioSrc) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;

        // cache all SpriteRenderers for blink (player + children)
        blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
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

    if (shieldBreakClip && audioSrc) audioSrc.PlayOneShot(shieldBreakClip);

    // --- NEW: one-shot emission, no lingering state ---
    if (breakBurst)
    {
        breakBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        breakBurst.transform.position = transform.position;   // pop at player location
        breakBurst.Emit(32); // emit N particles instantly (tune 20–40)
    }

    StartInvulnerability(invulnDuration);
    RefreshVisual();
    return true;
}


    public void RefreshVisual()
    {
        if (shieldVisual) shieldVisual.SetActive(charges > 0);
    }

    // --- Invulnerability helpers ---
    void StartInvulnerability(float duration)
    {
        invulnUntil = Time.time + duration;
        StopCoroutineSafe("BlinkCo");
        StartCoroutine("BlinkCo");
    }

    IEnumerator BlinkCo()
    {
        // simple 10 Hz blink for the duration
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

        SetAlpha(1f); // restore
    }

    void SetAlpha(float a)
    {
        if (blinkRenderers == null) return;
        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            if (!blinkRenderers[i]) continue;
            var c = blinkRenderers[i].color;
            c.a = a;
            blinkRenderers[i].color = c;
        }
    }

    void StopCoroutineSafe(string name)
    {
        try { StopCoroutine(name); } catch { }
    }
}
