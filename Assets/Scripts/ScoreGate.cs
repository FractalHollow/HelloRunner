using UnityEngine;

public class ScoreGate : MonoBehaviour
{
    private bool scored = false;
    GameManager gm;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (scored) return;
        if (other.CompareTag("Player"))
        {
            scored = true; // prevent double count
            gm?.AddPoint();
        }
    }
}
