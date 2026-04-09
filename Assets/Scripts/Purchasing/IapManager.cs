using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public class IapManager : MonoBehaviour
{
    public const string PremiumSkinProductId = "premium_skin";

    static readonly List<ProductDefinition> s_ProductsToFetch = new()
    {
        new ProductDefinition(PremiumSkinProductId, ProductType.NonConsumable)
    };

    public static IapManager I { get; private set; }

    public static event Action StateChanged;

    StoreController storeController;
    readonly HashSet<string> ownedProductIds = new();

    bool isConnecting;
    bool isInitialized;
    bool isStoreConnected;
    bool isFetchingProducts;
    bool isFetchingPurchases;
    string purchaseInProgressProductId;
    string selectOnGrantProductId;
    string lastStatusDetail;

    public bool IsInitialized => isInitialized;
    public bool IsStoreReady => isInitialized && isStoreConnected && storeController != null;
    public bool IsPurchaseInProgress => !string.IsNullOrEmpty(purchaseInProgressProductId);
    public string LastStatusDetail => lastStatusDetail;

    public static bool UseRealMoneyPurchasing =>
#if UNITY_ANDROID && !UNITY_EDITOR
        true;
#else
        false;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (I != null)
            return;

        var go = new GameObject(nameof(IapManager));
        DontDestroyOnLoad(go);
        go.AddComponent<IapManager>();
    }

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!UseRealMoneyPurchasing)
            return;

        EnsureInitialized();
    }

    void OnDestroy()
    {
        if (I == this)
            I = null;

        UnsubscribeFromStore();
    }

    public static string ProductIdForSkin(string skinId)
    {
        return string.Equals(skinId, PremiumSkinProductId, StringComparison.Ordinal) ? PremiumSkinProductId : null;
    }

    public static bool TryGetProductIdForSkin(string skinId, out string productId)
    {
        productId = ProductIdForSkin(skinId);
        return !string.IsNullOrEmpty(productId);
    }

    public bool IsOwned(string productId)
    {
        return ownedProductIds.Contains(productId);
    }

    public bool IsPurchaseInProgressFor(string productId)
    {
        return string.Equals(purchaseInProgressProductId, productId, StringComparison.Ordinal);
    }

    public string GetLocalizedPrice(string productId)
    {
        var product = GetProduct(productId);
        var price = product?.metadata?.localizedPriceString;
        return string.IsNullOrWhiteSpace(price) ? null : price;
    }

    public void PurchasePremiumSkin()
    {
        PurchaseProduct(PremiumSkinProductId);
    }

    public void EnsureInitialized()
    {
        if (!UseRealMoneyPurchasing)
            return;

        if (isConnecting || isFetchingProducts || isInitialized)
            return;

        InitializeStore();
    }

    public void PurchaseProduct(string productId)
    {
        if (!UseRealMoneyPurchasing)
        {
            Debug.LogWarning($"[IAP] PurchaseProduct called for '{productId}' while real-money purchasing is disabled.");
            return;
        }

        if (!IsStoreReady)
        {
            EnsureInitialized();
            Debug.LogWarning($"[IAP] Purchase blocked for '{productId}' because the store is not ready.");
            NotifyStateChanged();
            return;
        }

        if (IsOwned(productId))
        {
            Debug.Log($"[IAP] Purchase skipped for '{productId}' because it is already owned.");
            GrantEntitlement(productId);
            return;
        }

        var product = GetProduct(productId);
        if (product == null || !product.availableToPurchase)
        {
            Debug.LogWarning($"[IAP] Product '{productId}' is unavailable for purchase.");
            NotifyStateChanged();
            return;
        }

        purchaseInProgressProductId = productId;
        selectOnGrantProductId = productId;
        NotifyStateChanged();
        Debug.Log($"[IAP] Starting purchase for '{productId}'.");
        storeController.PurchaseProduct(product);
    }

    public void RefreshPurchases()
    {
        if (!UseRealMoneyPurchasing || storeController == null || !isInitialized || isFetchingPurchases)
            return;

        isFetchingPurchases = true;
        NotifyStateChanged();
        Debug.Log("[IAP] Fetching owned purchases from Google Play.");
        storeController.FetchPurchases();
    }

    async void InitializeStore()
    {
        if (isConnecting || isInitialized)
            return;

        isConnecting = true;
        isStoreConnected = false;
        isFetchingProducts = false;
        lastStatusDetail = "Connecting to Google Play...";
        NotifyStateChanged();

        try
        {
            if (storeController != null)
                UnsubscribeFromStore();

            storeController = UnityIAPServices.StoreController();
            SubscribeToStore();
            storeController.ProcessPendingOrdersOnPurchasesFetched(false);

            await storeController.Connect();
        }
        catch (Exception ex)
        {
            isConnecting = false;
            isStoreConnected = false;
            lastStatusDetail = $"Store initialization exception: {ex.Message}";
            Debug.LogError($"[IAP] Store initialization failed: {ex}");
            NotifyStateChanged();
        }
    }

    void SubscribeToStore()
    {
        if (storeController == null)
            return;

        storeController.OnStoreConnected += OnStoreConnected;
        storeController.OnStoreDisconnected += OnStoreDisconnected;
        storeController.OnProductsFetched += OnProductsFetched;
        storeController.OnProductsFetchFailed += OnProductsFetchFailed;
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed += OnPurchaseFailed;
        storeController.OnPurchaseDeferred += OnPurchaseDeferred;
        storeController.OnPurchasesFetched += OnPurchasesFetched;
        storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
    }

    void UnsubscribeFromStore()
    {
        if (storeController == null)
            return;

        storeController.OnStoreConnected -= OnStoreConnected;
        storeController.OnStoreDisconnected -= OnStoreDisconnected;
        storeController.OnProductsFetched -= OnProductsFetched;
        storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
        storeController.OnPurchasePending -= OnPurchasePending;
        storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
        storeController.OnPurchaseFailed -= OnPurchaseFailed;
        storeController.OnPurchaseDeferred -= OnPurchaseDeferred;
        storeController.OnPurchasesFetched -= OnPurchasesFetched;
        storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
    }

    void OnStoreConnected()
    {
        isConnecting = false;
        isStoreConnected = true;
        isFetchingProducts = true;
        lastStatusDetail = "Connected to Google Play. Fetching products...";
        Debug.Log("[IAP] Connected to Google Play.");
        NotifyStateChanged();
        storeController?.FetchProducts(s_ProductsToFetch);
    }

    void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        isConnecting = false;
        isInitialized = false;
        isStoreConnected = false;
        isFetchingProducts = false;
        lastStatusDetail = $"Store disconnected: {failure?.Message}";
        Debug.LogWarning($"[IAP] Store disconnected: {failure?.Message}");
        NotifyStateChanged();
    }

    void OnProductsFetched(List<Product> products)
    {
        isFetchingProducts = false;
        isInitialized = true;
        lastStatusDetail = $"Products fetched: {string.Join(", ", products.Select(p => p.definition.id))}";

        Debug.Log($"[IAP] Products fetched: {string.Join(", ", products.Select(p => p.definition.id))}");
        NotifyStateChanged();
        RefreshPurchases();
    }

    void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        isConnecting = false;
        isInitialized = false;
        isStoreConnected = true;
        isFetchingProducts = false;
        lastStatusDetail = $"Product fetch failed: {failure?.FailureReason}";
        Debug.LogError($"[IAP] Product fetch failed: {failure?.FailureReason}");
        NotifyStateChanged();
    }

    void OnPurchasePending(PendingOrder order)
    {
        var productIds = GetProductIds(order).ToList();
        foreach (var productId in productIds)
        {
            GrantEntitlement(productId, saveImmediately: true, selectIfRequested: true);
        }

        Debug.Log($"[IAP] Purchase pending confirmed for: {string.Join(", ", productIds)}");
        storeController?.ConfirmPurchase(order);
    }

    void OnPurchaseConfirmed(Order order)
    {
        purchaseInProgressProductId = null;
        Debug.Log($"[IAP] Purchase confirmed for: {string.Join(", ", GetProductIds(order))}");
        NotifyStateChanged();
    }

    void OnPurchaseFailed(FailedOrder failedOrder)
    {
        purchaseInProgressProductId = null;
        selectOnGrantProductId = null;
        Debug.LogWarning($"[IAP] Purchase failed: {failedOrder.FailureReason} | {failedOrder.Details}");
        NotifyStateChanged();
    }

    void OnPurchaseDeferred(DeferredOrder deferredOrder)
    {
        purchaseInProgressProductId = null;
        selectOnGrantProductId = null;
        Debug.LogWarning($"[IAP] Purchase deferred for: {string.Join(", ", GetProductIds(deferredOrder))}");
        NotifyStateChanged();
    }

    void OnPurchasesFetched(Orders orders)
    {
        isFetchingPurchases = false;
        lastStatusDetail = $"Owned products synced: {string.Join(", ", ownedProductIds)}";

        var fetchedProductIds = new HashSet<string>();

        foreach (var order in orders.ConfirmedOrders)
        {
            foreach (var productId in GetProductIds(order))
            {
                fetchedProductIds.Add(productId);
                GrantEntitlement(productId, saveImmediately: false);
            }
        }

        foreach (var order in orders.PendingOrders)
        {
            foreach (var productId in GetProductIds(order))
            {
                fetchedProductIds.Add(productId);
                GrantEntitlement(productId, saveImmediately: false);
            }
        }

        RevokeMissingOwnedProducts(fetchedProductIds);
        PlayerPrefs.Save();

        Debug.Log($"[IAP] Purchases fetched. Owned products: {string.Join(", ", ownedProductIds)}");
        NotifyStateChanged();
    }

    void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        isFetchingPurchases = false;
        lastStatusDetail = $"Fetch purchases failed: {failure?.FailureReason} | {failure?.Message}";
        Debug.LogWarning($"[IAP] Fetch purchases failed: {failure?.FailureReason} | {failure?.Message}");
        NotifyStateChanged();
    }

    void GrantEntitlement(string productId, bool saveImmediately = true, bool selectIfRequested = false)
    {
        if (string.IsNullOrEmpty(productId))
            return;

        ownedProductIds.Add(productId);
        PlayerPrefs.SetInt($"skin_unlocked_{productId}", 1);

        if (selectIfRequested && string.Equals(selectOnGrantProductId, productId, StringComparison.Ordinal))
        {
            CosmeticsManager.I?.TrySelect(productId);
            selectOnGrantProductId = null;
        }

        if (saveImmediately)
            PlayerPrefs.Save();

        NotifyStateChanged();
    }

    void RevokeMissingOwnedProducts(HashSet<string> fetchedProductIds)
    {
        var trackedProducts = new[] { PremiumSkinProductId };
        foreach (var productId in trackedProducts)
        {
            if (fetchedProductIds.Contains(productId))
                continue;

            ownedProductIds.Remove(productId);
            PlayerPrefs.DeleteKey($"skin_unlocked_{productId}");
        }
    }

    Product GetProduct(string productId)
    {
        return storeController?.GetProductById(productId);
    }

    static IEnumerable<string> GetProductIds(Order order)
    {
        return order?.CartOrdered?.Items()
            ?.Select(item => item?.Product?.definition?.id)
            ?.Where(id => !string.IsNullOrEmpty(id))
            ?? Enumerable.Empty<string>();
    }

    static void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
