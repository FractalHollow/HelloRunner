using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class GizmoColliderVisualizer2D : MonoBehaviour
{
    public bool drawSpriteBounds = true;   // blue
    public bool drawColliderBounds = true; // green
    public Color spriteColor   = new Color(0.2f, 0.6f, 1f, 0.8f);
    public Color colliderColor = new Color(0.3f, 1f, 0.3f, 0.9f);

    SpriteRenderer sr;
    Collider2D col;

    void OnDrawGizmos()
    {
        if (!sr)  sr  = GetComponentInChildren<SpriteRenderer>();
        if (!col) col = GetComponent<Collider2D>();

        if (drawSpriteBounds && sr && sr.sprite)
            DrawBounds(sr.bounds, spriteColor);

        if (drawColliderBounds && col)
            DrawBounds(col.bounds, colliderColor);
    }

    void DrawBounds(Bounds b, Color c)
    {
        Gizmos.color = c;
        Vector3 a = new Vector3(b.min.x, b.min.y, 0);
        Vector3 b1= new Vector3(b.max.x, b.min.y, 0);
        Vector3 c1= new Vector3(b.max.x, b.max.y, 0);
        Vector3 d = new Vector3(b.min.x, b.max.y, 0);
        Gizmos.DrawLine(a, b1); Gizmos.DrawLine(b1, c1);
        Gizmos.DrawLine(c1, d); Gizmos.DrawLine(d, a);
    }
}
