using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    [Header("Distance")]
    [Tooltip("Base meters gained each second at RunSpeedMultiplier = 1")]
    public float metersPerSecond = 2.5f;

    GameManager gm;

    public bool tracking;
    public float distance;          // meters this run
    public float bestDistance;      // lifetime best (meters)

    const string KEY_BEST = "best_distance_m";

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
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
            float speedMult = (gm != null) ? gm.RunSpeedMultiplier : 1f;
            distance += metersPerSecond * speedMult * Time.unscaledDeltaTime * (Time.timeScale);
    }
    
    
}
