using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ConfirmDialog : MonoBehaviour
{
    public static ConfirmDialog I;

    [Header("UI")]
    public GameObject root;
    public TMP_Text messageText;
    public Button cancelButton;
    public Button confirmButton;

    Action onConfirm;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        root.SetActive(false);

        cancelButton.onClick.AddListener(Hide);
        confirmButton.onClick.AddListener(Confirm);
    }

    public void Show(string message, Action onConfirm)
    {
        this.onConfirm = onConfirm;
        messageText.text = message;
        root.SetActive(true);
    }

    void Confirm()
    {
        root.SetActive(false);
        onConfirm?.Invoke();
        onConfirm = null;
    }

    void Hide()
    {
        root.SetActive(false);
        onConfirm = null;
    }
}
