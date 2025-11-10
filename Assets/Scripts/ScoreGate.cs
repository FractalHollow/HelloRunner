using UnityEngine;

public class ScoreGate : MonoBehaviour
{
    private bool scored = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (scored) return;
        if (other.CompareTag("Player"))
        {
            scored = true; // prevent double count
            FindObjectOfType<GameManager>()?.AddPoint();
        }
    }
}
