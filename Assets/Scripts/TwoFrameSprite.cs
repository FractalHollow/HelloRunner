using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TwoFrameSprite : MonoBehaviour
{
    public Sprite frameA;
    public Sprite frameB;
    [Tooltip("Frames per second; 1.5 ≈ swap every ~0.67s")]
    public float fps = 1.5f;

    SpriteRenderer sr;
    float timer;
    bool showingA = true;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void OnEnable() { ResetFrame(); }

    public void ResetFrame()
    {
        timer = 0f;
        showingA = true;
        if (sr && frameA) sr.sprite = frameA;
    }

    void Update()
    {
        if (!frameA || !frameB || fps <= 0f) return;

        timer += Time.deltaTime;
        float period = 1f / fps; // time between swaps
        if (timer >= period)
        {
            timer -= period;
            showingA = !showingA;
            if (sr) sr.sprite = showingA ? frameA : frameB;
        }
    }
}
