using UnityEngine;

public class ParallaxStrip : MonoBehaviour
{
    public float speed = 1.0f;          // units/second to the left
    public Transform tileA;              // optional: assign in Inspector
    public Transform tileB;              // optional: assign in Inspector

    private Transform _left;
    private Transform _right;
    private float _tileWidth;
    private float _anchorX;

    void Awake()
    {
        // Auto-detect two SpriteRenderer children if not assigned
        if (tileA == null || tileB == null)
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
            if (srs.Length < 2)
            {
                Debug.LogError("[ParallaxStrip] Need at least two SpriteRenderer children.");
                enabled = false;
                return;
            }
            tileA = srs[0].transform;
            tileB = srs[1].transform;
        }

        // Ensure _left is the one with the smaller X
        if (tileA.position.x <= tileB.position.x)
        {
            _left = tileA;
            _right = tileB;
        }
        else
        {
            _left = tileB;
            _right = tileA;
        }

        // Measure world-space width of a tile (includes scaling)
        _tileWidth = Mathf.Max(GetWorldWidth(_left), GetWorldWidth(_right));

        // Where the loop anchor starts (your A's original X)
        _anchorX = _left.position.x;

        // Snap spacing to exactly one width
        Vector3 r = _right.position;
        _right.position = new Vector3(_left.position.x + _tileWidth, r.y, r.z);
    }

    void Update()
    {
        float dx = -speed * Time.deltaTime;

        _left.position  += new Vector3(dx, 0f, 0f);
        _right.position += new Vector3(dx, 0f, 0f);

        // Keep references ordered (left.x <= right.x)
        if (_left.position.x > _right.position.x)
        {
            Transform t = _left; _left = _right; _right = t;
        }

        // When right reaches/passes anchor, recycle left to the far right
        if (_right.position.x <= _anchorX)
        {
            Vector3 lp = _left.position;
            _left.position = new Vector3(_right.position.x + _tileWidth, lp.y, lp.z);

            // Swap so names remain correct next frame
            Transform t = _left; _left = _right; _right = t;
        }
    }

    private float GetWorldWidth(Transform t)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        return sr != null ? sr.bounds.size.x : 0f;
    }
}
