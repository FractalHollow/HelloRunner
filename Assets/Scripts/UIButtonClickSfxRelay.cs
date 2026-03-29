using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonClickSfxRelay : MonoBehaviour
{
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (!button) button = GetComponent<Button>();
        if (button) button.onClick.AddListener(HandleClick);
    }

    void OnDisable()
    {
        if (button) button.onClick.RemoveListener(HandleClick);
    }

    void HandleClick()
    {
        if (GetComponent<UIButtonClickSfxSuppressor>()) return;
        AudioManager.I?.PlayUiClick();
    }
}
