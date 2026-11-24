using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>Нүүр: сонгосон баатар руны тавцан дээр, лигийн ахиц, BATTLE товч, санал.</summary>
    public sealed class HomeScreen : UiScreen
    {
        public override int NavIndex => 2;

        RectTransform backdrop;
        Image hero, emblem;
        Text heroName, heroSub, leagueName, leagueValue, streakText, freeCount;
        UiBar leagueBar;
        UiButton starterBtn, noAdsBtn, freeBtn;
        float idleT;

        protected override void Build()
        {
            // Арын зураг: бүтэн дэлгэцийг хамарна (safe area-аас гадна ч)
            backdrop = UIKit.Rect("HomeBackdrop", ui.bgLayer).Fill();
            var holder = UIKit.Img(backdrop, ArtCache.Home, Color.white, "Art");
            // Доороос нь тулгана: таблет мэт өргөн дэлгэцэнд тэнгэрийн хэсэг тасарна, тавцан BATTLE товчноос дээр үлдэнэ
            holder.rectTransform.pivot = new Vector2(0.5f, 0f);
            var fitter = holder.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 216f / 384f;
            backdrop.gameObject.SetActive(false);

            // Баатар тавцан дээр (тавцангийн төв = зургийн доороос 23.5%)
            hero = UIKit.Img(holder.transform, null, Color.white, "Hero");
            hero.rectTransform.anchorMin = hero.rectTransform.anchorMax = new Vector2(0.5f, 0.237f);
            hero.rectTransform.pivot = new Vector2(0.5f, 0f);
            var heroHit = hero.gameObject.AddComponent<UiButton>();
            hero.raycastTarget = true;
            heroHit.sound = "hit";
            heroHit.pressScale = 0.9f;
            heroHit.onClick = () => ui.Show(ui.heroes);
            var bob = hero.gameObject.AddComponent<Bob>();
            bob.amplitude = 5f;
            bob.speed = 2.4f;

            var c = Content();

            // Лигийн самбар
            var league = UIKit.Btn(c, null, "panel", 0, () => ui.Show(ui.league), "League");
            league.pressScale = 0.97f;
            league.rectTransform().TopStrip(170, 24);
            league.rectTransform().offsetMin = new Vector2(150, league.rectTransform().offsetMin.y);
            league.rectTransform().offsetMax = new Vector2(-186, league.rectTransform().offsetMax.y);
            emblem = UIKit.Img(league.transform, ArtCache.Emblem(0), Color.white, "Emblem");
            emblem.rectTransform.Place(0f, 0.5f, 18, 0, 28 * 4.2f, 32 * 4.2f);
            leagueName = UIKit.Label(league.transform, "Bronze League", 56, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Name");
            leagueName.rectTransform.Place(0f, 1f, 150, -20, 500, 64);
            leagueBar = UIKit.Bar(league.transform, UIKit.Gold, "Bar");
            leagueBar.rectTransform().Place(0f, 0f, 150, 30, 590, 50);
            leagueValue = UIKit.Label(leagueBar.transform, "", 34, UIKit.TextCol, true);
            leagueValue.rectTransform.Fill();

            // Тохиргоо (зүүн дээд)
            var settings = UIKit.IconBtn(c, "ui_gear", "btn_gray", 1.5f, () => ui.OpenSettings(), "Settings");
            settings.rectTransform().Place(0f, 1f, 18, -36, 116, 116);

            // Баруун талын санал
            starterBtn = Offer(c, "Starter", "STARTER", 0, () => ui.Show(ui.shop));
            var pyro = UIKit.Pix(starterBtn.transform, "hero_pyro", 0.9f, "Icon");
            pyro.rectTransform.anchoredPosition = new Vector2(0, 14);
            noAdsBtn = Offer(c, "NoAds", "NO ADS", 1, () => ui.Show(ui.shop));
            var na = UIKit.Img(noAdsBtn.transform, ArtCache.NoAds, Color.white, "Icon");
            na.rectTransform.sizeDelta = ArtCache.NoAds.rect.size * 6f;
            na.rectTransform.anchoredPosition = new Vector2(0, 14);
            freeBtn = Offer(c, "Free", "FREE", 2, ClaimFree);
            var chest = UIKit.Pix(freeBtn.transform, "ui_chest", 1f, "Icon");
            chest.rectTransform.anchoredPosition = new Vector2(0, 14);
            freeCount = UIKit.Label(freeBtn.transform, "", 30, UIKit.TextCol, true, TextAnchor.UpperRight, "Count");
            freeCount.rectTransform.Place(1f, 1f, -10, -6, 80, 40);

            // Баатрын нэр (арын зурагтай хамт хөдөлнө)
            heroName = UIKit.Label(holder.transform, "", 64, UIKit.TextCol, true, TextAnchor.MiddleCenter, "HeroName");
            heroSub = UIKit.Label(holder.transform, "", 38, UIKit.Gold, true, TextAnchor.MiddleCenter, "HeroSub");
            foreach (var t in new[] { heroName, heroSub })
            {
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.237f);
                t.rectTransform.pivot = new Vector2(0.5f, 0f);
            }

            // Цуврал ялалт
            streakText = UIKit.Label(c, "", 44, new Color(1f, 0.6f, 0.3f), true);
            streakText.rectTransform.TopStrip(60, 206);

            // BATTLE товч (лугшина)
            var pulse = UIKit.Rect("BattlePulse", c).Place(0.5f, 0f, 0, 50, 620, 170);
            pulse.gameObject.AddComponent<Pulse>().amount = 0.03f;
            var battle = UIKit.Btn(pulse, null, "btn_gold", 0, StartBattle, "Battle");
            battle.rectTransform().Fill();
            battle.sound = "horn";
            var swords = UIKit.Pix(battle.transform, "ui_swords", 1.5f, "Icon");
            swords.rectTransform.anchoredPosition = new Vector2(-200, 6);
            var bt = UIKit.Label(battle.transform, "BATTLE", 100, UIKit.TextCol, true);
            bt.rectTransform.Stretch(90, 26, 0, 0);
            var sub = UIKit.Label(battle.transform, "RANKED", 32, new Color(0.35f, 0.2f, 0.05f), true, TextAnchor.LowerCenter, "Sub", false);
            sub.rectTransform.BottomStrip(40, 14);
            sub.rectTransform.offsetMin = new Vector2(90, sub.rectTransform.offsetMin.y);
        }

        UiButton Offer(Transform parent, string name, string label, int index, System.Action onClick)
        {
            var b = UIKit.Btn(parent, null, "btn_purple", 0, onClick, name);
            b.rectTransform().Place(1f, 1f, -18, -36 - index * 170, 150, 156);
            var t = UIKit.Label(b.transform, label, 30, UIKit.TextCol, true, TextAnchor.LowerCenter, "Label");
            t.rectTransform.BottomStrip(40, 14);
            var bob = b.gameObject.AddComponent<Bob>();
            bob.amplitude = 4f;
            bob.speed = 2f;
            bob.phase = index;
            return b;
        }

        public override void OnShow()
        {
            backdrop.gameObject.SetActive(true);
            Refresh();
            Save.Changed += Refresh;
            AudioManager.Instance?.PlayMusic(false);
        }

        public override void OnHide()
        {
            backdrop.gameObject.SetActive(false);
            Save.Changed -= Refresh;
        }

        void Refresh()
        {
            var p = Save.P;
            var h = Save.SelectedHero;
            hero.sprite = ArtCache.Get(h.unit.sprite);
            var px = ArtCache.PixelSize(h.unit.sprite);
            hero.rectTransform.sizeDelta = new Vector2(px.x, px.y) * ArtCache.PixelScale * 2.4f;
            hero.rectTransform.anchoredPosition = Vector2.zero;
            hero.GetComponent<Bob>().Rebase();

            // Нэр: баатрын толгой дээр
            float hh = hero.rectTransform.sizeDelta.y;
            heroSub.rectTransform.anchoredPosition = new Vector2(0, hh + 22f);
            heroSub.rectTransform.sizeDelta = new Vector2(800, 50);
            heroName.rectTransform.anchoredPosition = new Vector2(0, hh + 74f);
            heroName.rectTransform.sizeDelta = new Vector2(800, 80);
            heroName.text = h.name;
            heroSub.text = h.title + "  -  Lv " + Save.HeroLevel(h.id);

            int tier = League.TierOf(p.trophies);
            emblem.sprite = ArtCache.Emblem(tier);
            leagueName.text = League.Names[tier] + " League";
            leagueName.color = ArtCache.ToColor(Palette.Tier(tier));
            int next = League.NextThreshold(p.trophies);
            leagueBar.Set(League.Progress(p.trophies), true);
            leagueValue.text = next > 0 ? p.trophies.ToString("N0") + " / " + next.ToString("N0") : p.trophies.ToString("N0") + "  MAX";

            starterBtn.gameObject.SetActive(!p.starterPack);
            noAdsBtn.gameObject.SetActive(!p.noAds);
            int idx = 0;
            foreach (var b in new[] { starterBtn, noAdsBtn, freeBtn })
            {
                if (!b.gameObject.activeSelf) continue;
                b.rectTransform().anchoredPosition = new Vector2(-18, -36 - idx * 170);
                b.GetComponent<Bob>().Rebase();
                idx++;
            }
            int left = Save.FreeRewardsLeft;
            freeCount.text = left + "/" + GameConfig.FreeRewardsPerDay;
            freeBtn.Interactable = left > 0;

            streakText.text = p.winStreak >= 2 ? p.winStreak + " WIN STREAK!" : "";
        }

        public override void Tick(float dt)
        {
            // Баатар хааяа "дайрах" хөдөлгөөн хийнэ
            idleT += dt;
            if (idleT > 4f)
            {
                idleT = 0f;
                ui.StartCoroutine(UIKit.Punch(hero.transform, 0.08f, 0.3f));
            }
        }

        void ClaimFree()
        {
            if (Save.FreeRewardsLeft <= 0) { ui.Toast("Come back tomorrow!"); return; }
            ui.WatchAdFor("free_chest", () =>
            {
                Save.UseFreeReward();
                bool gems = Random.value < 0.4f;
                if (gems) Save.AddGems(GameConfig.FreeRewardGems);
                else Save.AddGold(GameConfig.FreeRewardGold);
                ui.RewardPopup("FREE GIFT", "ui_chest", null, "Thanks for watching!", gems ? 0 : GameConfig.FreeRewardGold, gems ? GameConfig.FreeRewardGems : 0);
            });
        }

        void StartBattle()
        {
            Haptics.Medium();
            ui.Show(ui.battle);
        }
    }
}
