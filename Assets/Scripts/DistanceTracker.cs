using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    [Header("Distance")]
    [Tooltip("Meters gained each second while tracking")]
    public float metersPerSecond = 2.5f;

    public bool tracking;
    public float distance;          // meters this run
    public float bestDistance;      // lifetime best (meters)

    const string KEY_BEST = "best_distance_m";

    void Awake()
    {
        bestDistance = PlayerPrefs.GetFloat(KEY_BEST, 0f);
        if (metersPerSecond < 0f) metersPerSecond = 0f;
    }

    public void ResetRun() => distance = 0f;

    public void StopAndRecordBest()
    {
        tracking = false;
        if (distance > bestDistance)
        {
            bestDistance = distance;
            PlayerPrefs.SetFloat(KEY_BEST, bestDistance);
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        if (tracking && metersPerSecond > 0f)
            distance += metersPerSecond * Time.unscaledDeltaTime * (Time.timeScale);
        // Using scaled time so pause (timeScale=0) halts distance.
    }
    
    
}
