using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CosmeticsPanelController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;      // Panel_Cosmetics root
    public Button closeButton;

    [Header("List")]
    public Transform contentRoot;     // ScrollView/Viewport/Content
    public CosmeticRowUI rowPrefab;

    readonly List<CosmeticRowUI> rows = new List<CosmeticRowUI>();

    void Awake()
    {
        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    void Start()
    {
        // Start closed
        if (panelRoot) panelRoot.SetActive(false);
    }

    public void Open()
    {
        if (panelRoot) panelRoot.SetActive(true);
        RebuildList();
    }

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (!panelRoot) return;
        if (panelRoot.activeSelf) Close();
        else Open();
    }

    public void RebuildList()
    {
        // Clear old
        for (int i = 0; i < rows.Count; i++)
            if (rows[i]) Destroy(rows[i].gameObject);
        rows.Clear();

        if (CosmeticsManager.I == null)
        {
            Debug.LogWarning("[CosmeticsUI] CosmeticsManager.I is null. Ensure CosmeticsManager exists in scene.");
            return;
        }

        var skins = CosmeticsManager.I.GetAllSkins();
        if (skins == null) return;

        for (int i = 0; i < skins.Count; i++)
        {
            var def = skins[i];
            if (!def) continue;

            var row = Instantiate(rowPrefab, contentRoot);
            row.Bind(this, def);
            rows.Add(row);
        }

        RefreshAll();
    }

    public void RefreshAll()
    {
        for (int i = 0; i < rows.Count; i++)
            if (rows[i]) rows[i].Refresh();
    }
}
