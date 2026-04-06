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
        if (I != null && I != this)
        {
            Debug.LogWarning($"[ConfirmDialog] Duplicate instance on '{gameObject.name}'. Destroying duplicate.");
            Destroy(this);
            return;
        }

        I = this;

        DebugReferences("Awake");
        if (!ValidateReferences("Awake"))
            return;

        root.SetActive(false);
        cancelButton.onClick.RemoveListener(Hide);
        cancelButton.onClick.AddListener(Hide);
        confirmButton.onClick.RemoveListener(Confirm);
        confirmButton.onClick.AddListener(Confirm);

        Debug.Log($"[ConfirmDialog] Awake complete on '{gameObject.name}'. Root='{root.name}' active={root.activeSelf}");
    }

    void OnEnable()
    {
        I = this;
        DebugReferences("OnEnable");
        ValidateReferences("OnEnable");
    }

    void OnDisable()
    {
        if (I == this)
            I = null;
    }

    public void Show(string message, Action onConfirm)
    {
        Debug.Log($"[ConfirmDialog] Show requested on '{gameObject.name}'. message='{message}'");
        DebugReferences("Show");
        if (!ValidateReferences("Show"))
            return;

        this.onConfirm = onConfirm;
        messageText.text = message;

        NormalizePresentationState();
        root.SetActive(true);

        Debug.Log($"[ConfirmDialog] Show completed. Root activeSelf={root.activeSelf} activeInHierarchy={root.activeInHierarchy}");
    }

    void Confirm()
    {
        if (!ValidateReferences("Confirm"))
            return;

        root.SetActive(false);
        onConfirm?.Invoke();
        onConfirm = null;
        Debug.Log("[ConfirmDialog] Confirm pressed.");
    }

    void Hide()
    {
        if (!ValidateReferences("Hide"))
            return;

        root.SetActive(false);
        onConfirm = null;
        Debug.Log("[ConfirmDialog] Cancel pressed.");
    }

    bool ValidateReferences(string context)
    {
        bool valid = true;

        if (!root)
        {
            Debug.LogError($"[ConfirmDialog] {context}: root is missing or destroyed.");
            valid = false;
        }

        if (!messageText)
        {
            Debug.LogError($"[ConfirmDialog] {context}: messageText is missing or destroyed.");
            valid = false;
        }

        if (!cancelButton)
        {
            Debug.LogError($"[ConfirmDialog] {context}: cancelButton is missing or destroyed.");
            valid = false;
        }

        if (!confirmButton)
        {
            Debug.LogError($"[ConfirmDialog] {context}: confirmButton is missing or destroyed.");
            valid = false;
        }

        return valid;
    }

    void DebugReferences(string context)
    {
        Debug.Log(
            $"[ConfirmDialog] {context}: root={(root ? root.name : "NULL")} " +
            $"messageText={(messageText ? messageText.name : "NULL")} " +
            $"cancelButton={(cancelButton ? cancelButton.name : "NULL")} " +
            $"confirmButton={(confirmButton ? confirmButton.name : "NULL")}");
    }

    void NormalizePresentationState()
    {
        if (!root)
            return;

        var groups = root.GetComponentsInParent<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            var group = groups[i];
            Debug.Log(
                $"[ConfirmDialog] CanvasGroup '{group.gameObject.name}' alpha={group.alpha:0.###} " +
                $"interactable={group.interactable} blocksRaycasts={group.blocksRaycasts}");
        }

        var canvas = root.GetComponentInParent<Canvas>(true);
        if (canvas)
        {
            Debug.Log(
                $"[ConfirmDialog] Canvas '{canvas.gameObject.name}' renderMode={canvas.renderMode} " +
                $"sortingOrder={canvas.sortingOrder} overrideSorting={canvas.overrideSorting}");

            // The confirm modal was authored under StartScreen. Once gameplay starts,
            // that branch becomes inactive, so the modal cannot become active in hierarchy.
            ReparentToCanvasRootIfNeeded(canvas);
        }
        else
        {
            Debug.LogWarning("[ConfirmDialog] No parent Canvas found for confirm dialog root.");
        }

        root.transform.SetAsLastSibling();
    }

    void ReparentToCanvasRootIfNeeded(Canvas canvas)
    {
        if (!root || !canvas)
            return;

        var rootTransform = root.transform as RectTransform;
        var canvasTransform = canvas.transform as RectTransform;
        if (!rootTransform || !canvasTransform)
            return;

        if (rootTransform.parent == canvasTransform && root.activeInHierarchy)
            return;

        bool parentInactive = rootTransform.parent && !rootTransform.parent.gameObject.activeInHierarchy;
        if (!parentInactive && rootTransform.parent == canvasTransform)
            return;

        Debug.Log(
            $"[ConfirmDialog] Reparenting modal root from '{(rootTransform.parent ? rootTransform.parent.name : "NULL")}' " +
            $"to canvas root '{canvasTransform.name}' so it can render while gameplay UI is active.");

        rootTransform.SetParent(canvasTransform, false);
        rootTransform.anchorMin = Vector2.zero;
        rootTransform.anchorMax = Vector2.one;
        rootTransform.offsetMin = Vector2.zero;
        rootTransform.offsetMax = Vector2.zero;
        rootTransform.localScale = Vector3.one;
    }
}
