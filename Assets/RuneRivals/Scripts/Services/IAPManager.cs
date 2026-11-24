using System;
using System.Collections.Generic;
using UnityEngine;
#if RR_IAP
using UnityEngine.Purchasing;
#endif

namespace RuneRivals
{
    /// <summary>
    /// Жинхэнэ мөнгөөр худалдан авалт (Unity IAP v5).
    /// RR_IAP тэмдэгтийг Editor скрипт "In App Purchasing" багц суусны дараа автоматаар нэмнэ.
    /// Багц суугаагүй үед Editor дээр худалдан авалтыг ДУУРАЙЖ (туршилт) ажиллана, утсан дээр "Store unavailable".
    ///
    /// Google Play Console дээр яг ижил ID-тай бүтээгдэхүүн үүсгэнэ (GameConfig.cs):
    ///   gems_small, gems_medium, gems_large   (Consumable / нэг удаагийн)
    ///   no_ads, starter_pack                  (Non-consumable / байнгын)
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        public event Action OnProductsReady;
        public event Action<string> OnMessage;

        public bool IsReady { get; private set; }
        public string Status { get; private set; } = "Connecting to store...";

        void Awake()
        {
            Instance = this;
        }

        /// <summary>Худалдан авсан зүйлийг олгоно. Non-consumable-ийг дахин дуудахад давхар олгохгүй.</summary>
        void Grant(string productId)
        {
            var p = Save.P;
            switch (productId)
            {
                case GameConfig.IapGemsSmall: p.gems += GameConfig.GemsSmall; break;
                case GameConfig.IapGemsMedium: p.gems += GameConfig.GemsMedium; break;
                case GameConfig.IapGemsLarge: p.gems += GameConfig.GemsLarge; break;
                case GameConfig.IapNoAds: p.noAds = true; break;
                case GameConfig.IapStarterPack:
                    if (p.starterPack) break;
                    p.starterPack = true;
                    p.gems += GameConfig.StarterGems;
                    p.gold += GameConfig.StarterGold;
                    if (!p.ownedHeroes.Contains(GameConfig.StarterHero)) p.ownedHeroes.Add(GameConfig.StarterHero);
                    else p.gems += 150;
                    break;
                default:
                    Debug.LogWarning("[IAP] Unknown product " + productId);
                    return;
            }
            Save.Commit();
            AudioManager.Instance?.Play("reward");
        }

        /// <summary>Баримтаас тогтвортой түлхүүр (давхар олголтоос сэргийлнэ).</summary>
        static string ReceiptKey(string receipt)
        {
            if (string.IsNullOrEmpty(receipt)) return null;
            unchecked
            {
                ulong h = 14695981039346656037UL;
                foreach (char ch in receipt)
                {
                    h ^= ch;
                    h *= 1099511628211UL;
                }
                return "r" + h.ToString("x16");
            }
        }

        public static bool IsNonConsumable(string id) => id == GameConfig.IapNoAds || id == GameConfig.IapStarterPack;

        public bool Owns(string id) =>
            (id == GameConfig.IapNoAds && Save.P.noAds) || (id == GameConfig.IapStarterPack && Save.P.starterPack);

#if RR_IAP
        StoreController store;

        static List<ProductDefinition> Catalog() => new List<ProductDefinition>
        {
            new ProductDefinition(GameConfig.IapGemsSmall, ProductType.Consumable),
            new ProductDefinition(GameConfig.IapGemsMedium, ProductType.Consumable),
            new ProductDefinition(GameConfig.IapGemsLarge, ProductType.Consumable),
            new ProductDefinition(GameConfig.IapNoAds, ProductType.NonConsumable),
            new ProductDefinition(GameConfig.IapStarterPack, ProductType.NonConsumable),
        };

        async void Start()
        {
            try
            {
                store = UnityIAPServices.StoreController();

                // Бүх event-ийг Connect()-оос ӨМНӨ бүртгэнэ
                store.OnPurchasePending += OnPurchasePending;
                store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                store.OnPurchaseFailed += failed =>
                {
                    Debug.LogWarning($"[IAP] Purchase failed: {failed.FailureReason} {failed.Details}");
                    OnMessage?.Invoke("Purchase cancelled");
                };
                store.OnPurchaseDeferred += deferred => OnMessage?.Invoke("Purchase pending approval");
                store.OnStoreConnected += OnStoreConnected;
                store.OnStoreDisconnected += failure =>
                {
                    IsReady = false;
                    Status = "Store unavailable";
                    Debug.LogWarning("[IAP] Store disconnected: " + failure.Message);
                    OnProductsReady?.Invoke();
                };
                store.OnProductsFetched += products =>
                {
                    IsReady = true;
                    Status = "";
                    OnProductsReady?.Invoke();
                };
                store.OnProductsFetchFailed += failure =>
                {
                    Status = "Store unavailable";
                    Debug.LogWarning("[IAP] Product fetch failed: " + failure.FailureReason);
                    OnProductsReady?.Invoke();
                };
                store.OnPurchasesFetched += OnPurchasesFetched;
                store.OnPurchasesFetchFailed += failure => Debug.LogWarning("[IAP] Purchases fetch failed: " + failure.Message);

                await store.Connect();
            }
            catch (Exception e)
            {
                Status = "Store unavailable";
                Debug.LogWarning("[IAP] Init error: " + e.Message);
                OnProductsReady?.Invoke();
            }
        }

        void OnStoreConnected()
        {
            store.FetchProducts(Catalog());
            store.FetchPurchases();
        }

        void OnPurchasePending(PendingOrder order)
        {
            string key = ReceiptKey(order.Info?.Receipt);
            bool alreadyGranted = Save.IsTransactionProcessed(key);

            foreach (var item in order.CartOrdered.Items())
            {
                var id = item.Product.definition.id;
                // Non-consumable-ийг дахин дуудах нь хохиролгүй (Grant давхар олгохгүй); consumable-ийг давхар олгохгүй
                if (!alreadyGranted || IsNonConsumable(id)) Grant(id);
            }

            Save.MarkTransaction(key);
            Save.Flush();   // Хадгалсны ДАРАА баталгаажуулна
            store.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder _:
                    OnMessage?.Invoke("Thank you!");
                    OnProductsReady?.Invoke();
                    break;
                case FailedOrder failed:
                    Debug.LogWarning($"[IAP] Confirm failed: {failed.FailureReason} {failed.Details}");
                    break;
            }
        }

        void OnPurchasesFetched(Orders orders)
        {
            // Өмнө нь авсан No Ads / Starter Pack-ийг сэргээнэ (утас солих, дахин суулгах)
            foreach (var order in orders.ConfirmedOrders)
                foreach (var item in order.CartOrdered.Items())
                {
                    var id = item.Product.definition.id;
                    if (IsNonConsumable(id) && !Owns(id)) Grant(id);
                }
            OnProductsReady?.Invoke();
        }

        public string GetPrice(string productId)
        {
            if (store == null || !IsReady) return null;
            var p = store.GetProductById(productId);
            return p != null && p.availableToPurchase ? p.metadata.localizedPriceString : null;
        }

        public void Buy(string productId)
        {
            if (Owns(productId)) { OnMessage?.Invoke("Already owned"); return; }
            var p = store != null && IsReady ? store.GetProductById(productId) : null;
            if (p == null || !p.availableToPurchase) { OnMessage?.Invoke("Store unavailable"); return; }
            store.PurchaseProduct(p);
        }

        public void Restore()
        {
            if (store == null) { OnMessage?.Invoke("Store unavailable"); return; }
            store.RestoreTransactions((ok, error) =>
            {
                OnMessage?.Invoke(ok ? "Purchases restored" : "Restore failed");
                if (ok) store.FetchPurchases();
            });
        }
#else
        void Start()
        {
#if UNITY_EDITOR
            IsReady = true;
            Status = "";
            Debug.Log("[IAP] In App Purchasing package is not installed yet. Purchases are SIMULATED in the Editor.");
#else
            Status = "Store unavailable";
#endif
            OnProductsReady?.Invoke();
        }

        public string GetPrice(string productId) => null;

        public void Buy(string productId)
        {
            if (Owns(productId)) { OnMessage?.Invoke("Already owned"); return; }
#if UNITY_EDITOR
            Grant(productId);
            OnMessage?.Invoke("Test purchase (Editor)");
            OnProductsReady?.Invoke();
#else
            OnMessage?.Invoke("Store unavailable");
#endif
        }

        public void Restore() => OnMessage?.Invoke("Store unavailable");
        static string KeepKey() => ReceiptKey(null);
#endif
    }
}
