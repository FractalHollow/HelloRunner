using UnityEngine;

public class BoundaryApproachGlow : MonoBehaviour
{
    [Header("Refs")]
    public GameManager gameManager;
    public Collider2D playerCollider;
    public Collider2D groundCollider;
    public Collider2D ceilingCollider;
    public SpriteRenderer floorGlow;
    public SpriteRenderer ceilingGlow;

    [Header("Glow Tuning")]
    public Color glowColor = new Color(1f, 0.93f, 0.72f, 1f);
    [Min(0.1f)] public float triggerDistance = 3f;
    [Range(0f, 1f)] public float maxAlpha = 0.35f;
    [Min(0.05f)] public float maxGlowHeight = 1.75f;
    [Min(0.1f)] public float responseSharpness = 1.8f;
    [Min(0f)] public float smoothingSpeed = 10f;
    [Min(0f)] public float edgeOffset = 0.6f;
    [Min(0f)] public float widthPadding = 0.5f;

    const float HiddenGlowHeight = 0.01f;

    float floorIntensity;
    float ceilingIntensity;

    void Awake()
    {
        AutoAssignRefs();
        HideImmediate();
    }

    void OnEnable()
    {
        AutoAssignRefs();
        HideImmediate();
    }

    void OnValidate()
    {
        triggerDistance = Mathf.Max(0.1f, triggerDistance);
        maxGlowHeight = Mathf.Max(0.05f, maxGlowHeight);
        responseSharpness = Mathf.Max(0.1f, responseSharpness);
        smoothingSpeed = Mathf.Max(0f, smoothingSpeed);
        edgeOffset = Mathf.Max(0f, edgeOffset);
        widthPadding = Mathf.Max(0f, widthPadding);

        if (!Application.isPlaying)
        {
            AutoAssignRefs();
            HideImmediate();
        }
    }

    void LateUpdate()
    {
        AutoAssignRefs();

        if (!HasRequiredRefs())
            return;

        if (!ShouldShowGlow())
        {
            HideImmediate();
            return;
        }

        float delta = smoothingSpeed <= 0f ? 1f : smoothingSpeed * Time.deltaTime;
        float floorTarget = EvaluateFloorIntensity();
        float ceilingTarget = EvaluateCeilingIntensity();

        floorIntensity = Mathf.MoveTowards(floorIntensity, floorTarget, delta);
        ceilingIntensity = Mathf.MoveTowards(ceilingIntensity, ceilingTarget, delta);

        ApplyGlow(floorGlow, groundCollider.bounds, floorIntensity, 1f);
        ApplyGlow(ceilingGlow, ceilingCollider.bounds, ceilingIntensity, -1f);
    }

    void AutoAssignRefs()
    {
        if (!gameManager)
            gameManager = FindFirstObjectByType<GameManager>();

        if (!playerCollider)
        {
            var player = FindFirstObjectByType<PlayerGravityFlip>();
            if (player)
                playerCollider = player.GetComponent<Collider2D>();
        }

        if (!groundCollider)
        {
            var ground = GameObject.FindGameObjectWithTag("Ground");
            if (ground)
                groundCollider = ground.GetComponent<Collider2D>();
        }

        if (!ceilingCollider)
        {
            var ceiling = GameObject.FindGameObjectWithTag("Ceiling");
            if (ceiling)
                ceilingCollider = ceiling.GetComponent<Collider2D>();
        }

        if (!floorGlow)
        {
            var glow = GameObject.Find("FloorGlow");
            if (glow)
                floorGlow = glow.GetComponent<SpriteRenderer>();
        }

        if (!ceilingGlow)
        {
            var glow = GameObject.Find("CeilingGlow");
            if (glow)
                ceilingGlow = glow.GetComponent<SpriteRenderer>();
        }
    }

    bool HasRequiredRefs()
    {
        return playerCollider && groundCollider && ceilingCollider && floorGlow && ceilingGlow;
    }

    bool ShouldShowGlow()
    {
        return Application.isPlaying &&
               gameManager &&
               gameManager.IsPlaying &&
               Time.timeScale > 0f;
    }

    float EvaluateFloorIntensity()
    {
        float distance = playerCollider.bounds.min.y - groundCollider.bounds.max.y;
        return EvaluateDistance(distance);
    }

    float EvaluateCeilingIntensity()
    {
        float distance = ceilingCollider.bounds.min.y - playerCollider.bounds.max.y;
        return EvaluateDistance(distance);
    }

    float EvaluateDistance(float distance)
    {
        float normalized = 1f - Mathf.Clamp01(distance / triggerDistance);
        return Mathf.Pow(normalized, responseSharpness);
    }

    void ApplyGlow(SpriteRenderer glowRenderer, Bounds boundaryBounds, float intensity, float direction)
    {
        if (!glowRenderer)
            return;

        float height = Mathf.Max(HiddenGlowHeight, maxGlowHeight * intensity);
        float edgeY = direction > 0f ? boundaryBounds.max.y : boundaryBounds.min.y;
        float centerY = edgeY + direction * (height * 0.5f - edgeOffset);
        float width = boundaryBounds.size.x + widthPadding;

        Transform glowTransform = glowRenderer.transform;
        Vector3 position = glowTransform.position;
        glowTransform.position = new Vector3(boundaryBounds.center.x, centerY, position.z);
        glowTransform.localScale = new Vector3(width, height, 1f);

        Color c = glowColor;
        c.a *= maxAlpha * intensity;
        glowRenderer.color = c;
    }

    void HideImmediate()
    {
        floorIntensity = 0f;
        ceilingIntensity = 0f;

        if (groundCollider && floorGlow)
            ApplyGlow(floorGlow, groundCollider.bounds, 0f, 1f);

        if (ceilingCollider && ceilingGlow)
            ApplyGlow(ceilingGlow, ceilingCollider.bounds, 0f, -1f);
    }
}
