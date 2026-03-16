using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    [Header("Target")]
    public SpriteRenderer targetRenderer;

    [Header("Fallback")]
    public Sprite fallbackIdleSprite;

    Sprite[] idleFrames;
    Sprite[] jumpFrames;
    float idleFrameDuration = 0.2f;
    float jumpFrameDuration = 0.08f;

    float frameTimer;
    int frameIndex;
    bool playingJump;

    void Awake()
    {
        if (!targetRenderer)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        ShowIdleImmediate();
    }

    void Update()
    {
        if (!targetRenderer)
            return;

        var activeFrames = playingJump ? jumpFrames : idleFrames;
        int frameCount = activeFrames != null ? activeFrames.Length : 0;
        if (frameCount <= 1)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = playingJump ? jumpFrameDuration : idleFrameDuration;
        if (frameTimer < frameDuration)
            return;

        frameTimer -= frameDuration;
        frameIndex++;

        if (playingJump)
        {
            if (frameIndex >= frameCount)
            {
                ShowIdleImmediate();
                return;
            }
        }
        else
        {
            frameIndex %= frameCount;
        }

        ApplyCurrentFrame();
    }

    public void SetAnimationSet(Sprite[] idle, Sprite[] jump, float idleDuration, float jumpDuration, Sprite fallbackSprite = null)
    {
        idleFrames = idle;
        jumpFrames = jump;
        idleFrameDuration = Mathf.Max(0.01f, idleDuration);
        jumpFrameDuration = Mathf.Max(0.01f, jumpDuration);

        if (fallbackSprite)
            fallbackIdleSprite = fallbackSprite;

        ShowIdleImmediate();
    }

    public void PlayJump()
    {
        if (!targetRenderer)
            return;

        if (jumpFrames == null || jumpFrames.Length == 0)
        {
            ShowIdleImmediate();
            return;
        }

        playingJump = true;
        frameIndex = 0;
        frameTimer = 0f;
        ApplyCurrentFrame();
    }

    public void ShowIdleImmediate()
    {
        playingJump = false;
        frameIndex = 0;
        frameTimer = 0f;
        ApplyCurrentFrame();
    }

    void ApplyCurrentFrame()
    {
        if (!targetRenderer)
            return;

        Sprite sprite = null;

        if (playingJump)
        {
            if (jumpFrames != null && jumpFrames.Length > 0)
                sprite = jumpFrames[Mathf.Clamp(frameIndex, 0, jumpFrames.Length - 1)];
        }
        else
        {
            if (idleFrames != null && idleFrames.Length > 0)
                sprite = idleFrames[Mathf.Clamp(frameIndex, 0, idleFrames.Length - 1)];
        }

        if (!sprite)
            sprite = fallbackIdleSprite;

        if (sprite)
            targetRenderer.sprite = sprite;
    }
}
