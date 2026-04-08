using UnityEngine;

public class EnemyVerticalBob2D : MonoBehaviour
{
    [Header("Bobbing")]
    public float amplitudeMin = 0.25f;
    public float amplitudeMax = 0.6f;
    public float frequencyMin = 0.7f;
    public float frequencyMax = 1.3f;

    float baseY;
    float amplitude;
    float frequency;
    float phase;

    void OnEnable()
    {
        baseY = transform.position.y;
        amplitude = Random.Range(amplitudeMin, amplitudeMax);
        frequency = Random.Range(frequencyMin, frequencyMax);
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * frequency * Mathf.PI * 2f + phase) * amplitude;
        transform.position = pos;
    }
}
