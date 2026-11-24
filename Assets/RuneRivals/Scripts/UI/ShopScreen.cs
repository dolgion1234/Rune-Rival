using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>
    /// Дэлгүүр: Starter Pack, No Ads, эрдэнэ (жинхэнэ мөнгө), алт (эрдэнээр), өдрийн үнэгүй шагнал (зар).
    /// Үнийг Google Play-ээс авна; store холбогдоогүй үед GameConfig.FallbackPrice.
    /// </summary>
    public sealed class ShopScreen : UiScreen
    {
        public override int NavIndex => 0;

        sealed class Tile
        {
            public UiButton button;
            public Image priceFrame;
            public Text amount, price, sub;
        }

        RectTransform content;
        RectTransform starterSec, noAdsSec, gemsSec, goldSec, footerSec;
        readonly List<RectTransform> order = new List<RectTransform>();
        Text starterPrice, noAdsPrice, status;
        UiButton starterBuy, noAdsBuy;
        readonly Tile[] gemTiles = new Tile[4];
        readonly Tile[] goldTiles = new Tile[4];
        ScrollRect scroll;

        static readonly string[] GemIds = { null, GameConfig.IapGemsSmall, GameConfig.IapGemsMedium, GameConfig.IapGemsLarge };
        static readonly int[] GemAmounts = { GameConfig.FreeRewardGems, GameConfig.GemsSmall, GameConfig.GemsMedium, GameConfig.GemsLarge };

        protected override void Build()
        {
            var c = Content();
            content = UIKit.Scroll(c, out scroll, "Scroll");
            ((RectTransform)content.parent).Fill();

            BuildStarter();
            BuildNoAds();
            BuildGems();
            BuildGold();
            BuildFooter();
            order.AddRange(new[] { starterSec, noAdsSec, gemsSec, goldSec, footerSec });
        }

        // ---------------- Starter Pack ----------------

        void BuildStarter()
        {
            starterSec = UIKit.Rect("StarterPack", content).TopStrip(470, 0);
            var glow = UIKit.Frame(starterSec, "btn_gold", "Glow");
            glow.rectTransform.Stretch(18, -12, 18, -12);
            var panel = UIKit.Frame(starterSec, "panel", "Panel").rectTransform.Stretch(30, 0, 30, 0);

            var rays = UIKit.Img(panel, ArtCache.Rays, new Color(1f, 0.85f, 0.5f, 0.3f), "Rays");
            rays.rectTransform.At(0f, 0.5f, 190, 10, 420, 420);
            rays.gameObject.AddComponent<Spin>().speed = 14f;
            var hero = UIKit.Pix(panel, GameConfig.StarterHero == "pyro" ? "hero_pyro" : "hero_knight", 3f, "Hero");
            hero.rectTransform.At(0f, 0.5f, 190, 10, hero.rectTransform.sizeDelta.x, hero.rectTransform.sizeDelta.y);
            var bob = hero.gameObject.AddComponent<Bob>();
            bob.amplitude = 5f;

            var title = UIKit.Label(panel, "STARTER PACK", 76, UIKit.Gold, true, TextAnchor.MiddleLeft, "Title");
            title.rectTransform.Place(0f, 1f, 370, -28, 620, 84);
            var heroLine = UIKit.Label(panel, Heroes.Get(GameConfig.StarterHero).name + " the " + Heroes.Get(GameConfig.StarterHero).title,
                44, UIKit.TextCol, false, TextAnchor.MiddleLeft, "HeroLine");
            heroLine.rectTransform.Place(0f, 1f, 372, -118, 620, 56);
            var gems = UIKit.Rect("Gems", panel).Place(0f, 1f, 372, -184, 300, 64);
            UIKit.IconValue(gems, "ui_gem", "+" + GameConfig.StarterGems.ToString("N0"), 56, UIKit.GemCol, 1f, out _);
            var gold = UIKit.Rect("Gold", panel).Place(0f, 1f, 372, -258, 300, 64);
            UIKit.IconValue(gold, "ui_coin", "+" + GameConfig.StarterGold.ToString("N0"), 56, UIKit.Gold, 1f, out _);

            starterBuy = UIKit.Btn(panel, "", "btn_green", 60, () => Buy(GameConfig.IapStarterPack), "Buy");
            starterBuy.rectTransform().Place(1f, 0f, -40, 40, 330, 140);
            starterPrice = starterBuy.label;

            var tag = UIKit.Frame(panel, "btn_red", "Tag").rectTransform;
            tag.Place(1f, 1f, -30, -24, 250, 76);
            var tagText = UIKit.Label(tag, "ONLY ONCE", 36, UIKit.TextCol, true);
            tagText.rectTransform.Stretch(6, 10, 6, 2);
            tag.gameObject.AddComponent<Pulse>().amount = 0.05f;
        }

        // ---------------- No Ads ----------------

        void BuildNoAds()
        {
            noAdsSec = UIKit.Rect("NoAds", content).TopStrip(290, 0);
            var panel = UIKit.Frame(noAdsSec, "panel", "Panel").rectTransform.Stretch(30, 0, 30, 0);
            var icon = UIKit.Img(panel, ArtCache.NoAds, Color.white, "Icon");
            icon.rectTransform.At(0f, 0.5f, 150, 0, ArtCache.NoAds.rect.width * 8f, ArtCache.NoAds.rect.height * 8f);
            var title = UIKit.Label(panel, "NO ADS", 72, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Title");
            title.rectTransform.Place(0f, 1f, 290, -30, 400, 80);
            var desc = UIKit.Para(panel, "No more ads between battles. Reward videos stay optional.", 36, UIKit.Dim, TextAnchor.UpperLeft, "Desc");
            desc.rectTransform.Place(0f, 1f, 292, -118, 400, 140);
            noAdsBuy = UIKit.Btn(panel, "", "btn_green", 60, () => Buy(GameConfig.IapNoAds), "Buy");
            noAdsBuy.rectTransform().Place(1f, 0.5f, -40, 0, 300, 140);
            noAdsPrice = noAdsBuy.label;
        }

        // ---------------- Эрдэнэ ----------------

        void BuildGems()
        {
            gemsSec = UIKit.Rect("Gems", content).TopStrip(SectionHeight, 0);
            UIKit.Header(gemsSec, "GEMS", 0);
            string[] badges = { "FREE", null, "POPULAR", "BEST VALUE" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var t = MakeTile(gemsSec, i, badges[i], () => OnGemTile(idx));
                t.amount.text = GemAmounts[i].ToString("N0");
                t.amount.color = UIKit.GemCol;
                SetAmountIcon(t, "ui_gem");
                var art = UIKit.Img(t.button.transform, ArtCache.GemPile(i == 0 ? 1 : i == 1 ? 2 : i == 2 ? 4 : 6), Color.white, "Art");
                var sp = art.sprite;
                art.rectTransform.At(0.5f, 0.5f, 0, ArtY, sp.rect.width * 6f, sp.rect.height * 6f);
                art.transform.SetSiblingIndex(1);
                if (i == 0) FreeLook(t);
                gemTiles[i] = t;
            }
        }

        // ---------------- Алт ----------------

        void BuildGold()
        {
            goldSec = UIKit.Rect("Gold", content).TopStrip(SectionHeight, 0);
            UIKit.Header(goldSec, "GOLD", 0);
            string[] badges = { "FREE", null, null, "BEST VALUE" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var t = MakeTile(goldSec, i, badges[i], () => OnGoldTile(idx));
                t.amount.text = (i == 0 ? GameConfig.FreeRewardGold : GameConfig.GoldPackGold[i - 1]).ToString("N0");
                t.amount.color = UIKit.Gold;
                SetAmountIcon(t, "ui_coin");
                int coins = i == 0 ? 1 : i + 1;
                for (int k = 0; k < coins; k++)
                {
                    var coin = UIKit.Pix(t.button.transform, "ui_coin", 2.6f, "Coin" + k);
                    float spread = (k - (coins - 1) / 2f) * 46f;
                    coin.rectTransform.At(0.5f, 0.5f, spread, ArtY - 9f + (k % 2) * 18f, coin.rectTransform.sizeDelta.x, coin.rectTransform.sizeDelta.y);
                    coin.transform.SetSiblingIndex(1);
                }
                if (i == 0) FreeLook(t);
                else
                {
                    t.priceFrame.SetFrame("btn_purple");
                    t.price.text = GameConfig.GoldPackGems[i - 1].ToString("N0");
                    var gem = UIKit.Pix(t.priceFrame.transform, "ui_gem", 0.9f, "Gem");
                    gem.rectTransform.At(0f, 0.5f, 60, 4, gem.rectTransform.sizeDelta.x, gem.rectTransform.sizeDelta.y);
                    t.price.rectTransform.Stretch(80, 12, 10, 4);
                }
                goldTiles[i] = t;
            }
        }

        // Тайлын хэмжээ. Дотор нь дээрээс доош: дүн (тоо), зураг, жижиг бичиг, үнийн товч.
        // Зургийг дүн ба жижиг бичгийн хоорондох зайд төвлөрүүлсэн тул хоорондоо давхцахгүй.
        const float TileW = 476f, TileH = 460f, TileGap = 20f, TilesTop = 110f;
        const float SectionHeight = TilesTop + TileH * 2f + TileGap + 20f;
        const float ArtY = 42f;   // тайлын төвөөс дээш

        Tile MakeTile(RectTransform parent, int index, string badge, Action onClick)
        {
            var t = new Tile();
            var holder = UIKit.Rect("Tile" + index, parent);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f);
            holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(index % 2 == 0 ? -250 : 250, -(TilesTop + (index / 2) * (TileH + TileGap)));
            holder.sizeDelta = new Vector2(TileW, TileH);

            t.button = UIKit.Btn(holder, null, "panel", 0, onClick, "Button");
            t.button.rectTransform().Fill();
            t.button.pressScale = 0.96f;

            t.amount = UIKit.Label(t.button.transform, "", 66, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Amount");
            t.amount.rectTransform.At(0.5f, 1f, 0, -64, 300, 76);

            t.sub = UIKit.Label(t.button.transform, "", 32, UIKit.Dim, false, TextAnchor.MiddleCenter, "Sub");
            t.sub.rectTransform.At(0.5f, 0f, 0, 166, 440, 44);

            t.priceFrame = UIKit.Frame(t.button.transform, "btn_green", "Price");
            t.priceFrame.rectTransform.Place(0.5f, 0f, 0, 28, 380, 118);
            t.price = UIKit.Label(t.priceFrame.transform, "", 56, UIKit.TextCol, true, TextAnchor.MiddleCenter, "Text");
            t.price.rectTransform.Stretch(10, 12, 10, 4);

            if (!string.IsNullOrEmpty(badge))
            {
                var b = UIKit.Frame(holder, badge == "FREE" ? "btn_orange" : "btn_red", "Badge").rectTransform;
                b.Place(0f, 1f, -14, 18, badge.Length > 6 ? 250 : 190, 66);
                b.localEulerAngles = new Vector3(0, 0, 6);
                var bt = UIKit.Label(b, badge, 34, UIKit.TextCol, true);
                bt.rectTransform.Stretch(6, 10, 6, 2);
            }
            return t;
        }

        /// <summary>Дүнгийн өмнө дүрс тавьж, бүгдийг төвд байрлуулна.</summary>
        static void SetAmountIcon(Tile t, string icon)
        {
            var img = UIKit.Pix(t.amount.transform, icon, 1.1f, "Icon");
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            img.rectTransform.pivot = new Vector2(1f, 0.5f);
            img.rectTransform.anchoredPosition = new Vector2(-10, 2);
            CenterAmount(t);
        }

        static void CenterAmount(Tile t)
        {
            var icon = t.amount.transform.Find("Icon") as RectTransform;
            float iw = icon != null ? icon.sizeDelta.x + 10f : 0f;
            float w = t.amount.preferredWidth;
            t.amount.rectTransform.sizeDelta = new Vector2(w, 76);
            t.amount.rectTransform.anchoredPosition = new Vector2(iw * 0.5f, -64);
        }

        void FreeLook(Tile t)
        {
            t.priceFrame.SetFrame("btn_orange");
            var ad = UIKit.Pix(t.priceFrame.transform, "ui_ad", 1f, "Ad");
            ad.rectTransform.At(0f, 0.5f, 64, 4, ad.rectTransform.sizeDelta.x, ad.rectTransform.sizeDelta.y);
            t.price.text = "FREE";
            t.price.rectTransform.Stretch(90, 12, 10, 4);
        }

        // ---------------- Доод хэсэг ----------------

        void BuildFooter()
        {
            footerSec = UIKit.Rect("Footer", content).TopStrip(300, 0);
            var restore = UIKit.Btn(footerSec, "Restore Purchases", "btn_gray", 48, () => IAPManager.Instance?.Restore(), "Restore");
            restore.rectTransform().Place(0.5f, 1f, 0, -20, 560, 120);
            status = UIKit.Label(footerSec, "", 36, UIKit.Dim, false, TextAnchor.MiddleCenter, "Status");
            status.rectTransform.TopStrip(50, 160);
            var note = UIKit.Para(footerSec, "Payments are handled securely by Google Play.", 32, UIKit.Dim, TextAnchor.UpperCenter, "Note");
            note.rectTransform.TopStrip(60, 220);
        }

        // ---------------- Шинэчлэх ----------------

        public override void OnShow()
        {
            Refresh();
            Save.Changed += Refresh;
            if (IAPManager.Instance != null) IAPManager.Instance.OnProductsReady += Refresh;
            scroll.verticalNormalizedPosition = 1f;
        }

        public override void OnHide()
        {
            Save.Changed -= Refresh;
            if (IAPManager.Instance != null) IAPManager.Instance.OnProductsReady -= Refresh;
        }

        static string Price(string id)
        {
            var s = IAPManager.Instance != null ? IAPManager.Instance.GetPrice(id) : null;
            return string.IsNullOrEmpty(s) ? GameConfig.FallbackPrice(id) : s;
        }

        void Refresh()
        {
            var p = Save.P;
            starterSec.gameObject.SetActive(!p.starterPack);
            noAdsSec.gameObject.SetActive(!p.noAds);

            // Хэсгүүдийг дээрээс доош байрлуулна. Текст шинэчлэхээс ӨМНӨ хийнэ:
            // доорх ямар нэг мөр алдаа өгсөн ч хэсгүүд бие биен дээрээ давхцахгүй.
            float y = 30;
            foreach (var sec in order)
            {
                if (!sec.gameObject.activeSelf) continue;
                sec.anchoredPosition = new Vector2(0, -y);
                y += sec.sizeDelta.y + 30;
            }
            content.sizeDelta = new Vector2(0, y + 20);

            starterPrice.text = Price(GameConfig.IapStarterPack);
            noAdsPrice.text = Price(GameConfig.IapNoAds);

            int free = Save.FreeRewardsLeft;
            for (int i = 0; i < 4; i++)
            {
                var g = gemTiles[i];
                if (i == 0)
                {
                    g.sub.text = free > 0 ? free + " left today" : "Come back tomorrow";
                    g.button.Interactable = free > 0;
                }
                else
                {
                    g.price.text = Price(GemIds[i]);
                    g.sub.text = i == 3 ? "+20% bonus" : "";
                }

                var o = goldTiles[i];
                if (i == 0)
                {
                    o.sub.text = free > 0 ? free + " left today" : "Come back tomorrow";
                    o.button.Interactable = free > 0;
                }
                else
                {
                    bool afford = p.gems >= GameConfig.GoldPackGems[i - 1];
                    o.price.color = afford ? UIKit.TextCol : new Color(1f, 0.6f, 0.6f);
                }
            }

            var iap = IAPManager.Instance;
            status.text = iap != null && !string.IsNullOrEmpty(iap.Status) ? iap.Status : "";
        }

        // ---------------- Үйлдэл ----------------

        void Buy(string id)
        {
            if (IAPManager.Instance == null) { ui.Toast("Store unavailable"); return; }
            IAPManager.Instance.Buy(id);
        }

        void OnGemTile(int i)
        {
            if (i == 0)
            {
                if (Save.FreeRewardsLeft <= 0) { ui.Toast("Come back tomorrow!"); return; }
                ui.WatchAdFor("shop_free_gems", () =>
                {
                    Save.UseFreeReward();
                    Save.AddGems(GameConfig.FreeRewardGems);
                    ui.RewardPopup("FREE GEMS", "ui_gem", null, "Thanks for watching!", 0, GameConfig.FreeRewardGems);
                });
                return;
            }
            Buy(GemIds[i]);
        }

        void OnGoldTile(int i)
        {
            if (i == 0)
            {
                if (Save.FreeRewardsLeft <= 0) { ui.Toast("Come back tomorrow!"); return; }
                ui.WatchAdFor("shop_free_gold", () =>
                {
                    Save.UseFreeReward();
                    Save.AddGold(GameConfig.FreeRewardGold);
                    ui.RewardPopup("FREE GOLD", "ui_coin", null, "Thanks for watching!", GameConfig.FreeRewardGold, 0);
                });
                return;
            }
            int gems = GameConfig.GoldPackGems[i - 1], gold = GameConfig.GoldPackGold[i - 1];
            if (Save.P.gems < gems)
            {
                ui.Confirm("Not enough gems", "You need " + gems + " gems for this pack. Get more gems above?", "OK", () => scroll.verticalNormalizedPosition = 1f);
                return;
            }
            ui.Confirm("Buy gold?", "Trade " + gems + " gems for " + gold.ToString("N0") + " gold?", "Buy", () =>
            {
                if (!Save.SpendGems(gems)) return;
                Save.AddGold(gold);
                AudioManager.Instance?.Coin();
                ui.RewardPopup("GOLD!", "ui_coin", null, "Your treasury grows!", gold, 0);
            });
        }
    }
}
