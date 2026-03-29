using UnityEngine;

public class TapToStart : MonoBehaviour
{
    GameManager gm;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            gm?.StartGame();
    }
}
