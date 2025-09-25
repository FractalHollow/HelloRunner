using UnityEngine;

public class TapToStart : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            FindObjectOfType<GameManager>()?.StartGame();
    }
}
