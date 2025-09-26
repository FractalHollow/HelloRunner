using UnityEngine;

public class ParallaxStrip : MonoBehaviour
{
    public float speed = 1f;

    Transform a, b;          // our two tiles
    float widthA, widthB;    // world widths

    void Awake()
    {
        if (transform.childCount < 2)
        {
            Debug.LogError("[ParallaxStrip] Needs exactly two child sprites.");
            enabled = false; return;
        }

        a = transform.GetChild(0);
        b = transform.GetChild(1);

        widthA = a.GetComponent<SpriteRenderer>().bounds.size.x;
        widthB = b.GetComponent<SpriteRenderer>().bounds.size.x;

        // Ensure A is left of B at start (swap if needed)
        if (a.position.x > b.position.x) { var t = a; a = b; b = t; var tw = widthA; widthA = widthB; widthB = tw; }
    }

    void Update()
    {
        // move both left
        float dx = speed * Time.deltaTime;
        a.position += Vector3.left * dx;
        b.position += Vector3.left * dx;

        // if A fully left of screen anchor (past its full width), move it to the right of B
        if (a.position.x <= b.position.x - widthA)
        {
            a.position = new Vector3(b.position.x + widthB, a.position.y, a.position.z);
            // swap references so A is always the left tile
            var t = a; a = b; b = t; var tw = widthA; widthA = widthB; widthB = tw;
        }
        // same for B
        else if (b.position.x <= a.position.x - widthB)
        {
            b.position = new Vector3(a.position.x + widthA, b.position.y, b.position.z);
            var t = a; a = b; b = t; var tw = widthA; widthA = widthB; widthB = tw;
        }
    }
}
