using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonClickSfxInstaller : MonoBehaviour
{
    [SerializeField] float rescanInterval = 0.5f;

    float nextScanTime;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshBindings();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Time.unscaledTime < nextScanTime) return;
        nextScanTime = Time.unscaledTime + rescanInterval;
        RefreshBindings();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshBindings();
    }

    void RefreshBindings()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (!button) continue;
            if (button.GetComponent<UIButtonClickSfxRelay>()) continue;

            button.gameObject.AddComponent<UIButtonClickSfxRelay>();
        }
    }
}
