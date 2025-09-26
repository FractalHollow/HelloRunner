using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    public float speed = 1f;

    SpriteRenderer sr;
    float spriteWidth;   // world units
    float startX;

    void Awake()
    {
              // Debugging for sprite width
        var sr = GetComponent<SpriteRenderer>();
        Debug.Log("BG_Far width = " + sr.bounds.size.x);
    
        sr = GetComponent<SpriteRenderer>();
        // Bounds are in world units at current scale
        spriteWidth = sr.bounds.size.x;
        startX = transform.position.x;


    }

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        // when we've moved left by one sprite width from the start,
        // jump forward by two widths to keep the endless loop
        if (transform.position.x <= startX - spriteWidth)
        {
            transform.position += Vector3.right * spriteWidth * 2f;
            startX += -spriteWidth; // keep threshold sliding forward
        }
    }
}
