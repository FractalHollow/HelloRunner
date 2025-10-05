using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafe;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void OnEnable()
    {
        // In case it enabled after a size/orientation change
        if (!rt) rt = GetComponent<RectTransform>();
        Apply();
    }

    void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    void Apply()
    {
        if (!rt) return; // safety

        Rect safe = Screen.safeArea;

        // Convert from absolute pixels to normalized anchors
        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;  min.y /= Screen.height;
        max.x /= Screen.width;  max.y /= Screen.height;

        // Only update if changed
        if (safe != lastSafe || rt.anchorMin != min || rt.anchorMax != max)
        {
            lastSafe = safe;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
