using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>Баатрууд: үзэх, нээх, сонгох, түвшин ахиулах + цэргийн түвшин.</summary>
    public sealed class HeroesScreen : UiScreen
    {
        public override int NavIndex => 1;

        sealed class Cell
        {
            public HeroDef hero;
            public UiButton button;
            public Image art, highlight, check, lockIcon;
            public Text name, status;
        }

        readonly List<Cell> cells = new List<Cell>();
        HeroDef viewing;

        // Үзүүлэн
        Image heroArt, glow;
        Text heroName, heroTitle, levelText, ability;
        UiBar levelBar;
        Text hpText, atkText, styleText, hpNext, atkNext;
        Image styleIcon;
        UiButton upgradeBtn, actionBtn;
        Text upgradeCost, actionCost, actionLabel;
        Image upgradeCoin, actionIcon;

        // Арми
        Text armyLevel, armyBonus, armyCost;
        UiButton armyBtn;
        Image armyCoin;

        protected override void Build()
        {
            var c = Content();
            var content = UIKit.Scroll(c, out _, "Scroll");
            ((RectTransform)content.parent).Fill();
            content.sizeDelta = new Vector2(0, 1540);

            BuildShowcase(content);
            BuildRow(content);
            BuildArmy(content);
        }

        // ---------------- Үзүүлэн самбар ----------------

        void BuildShowcase(RectTransform parent)
        {
            var panel = UIKit.Frame(parent, "panel", "Showcase").rectTransform;
            panel.TopStrip(820, 20);
            panel.offsetMin = new Vector2(30, panel.offsetMin.y);
            panel.offsetMax = new Vector2(-30, panel.offsetMax.y);

            glow = UIKit.Img(panel, ArtCache.Glow, new Color(1f, 0.85f, 0.4f, 0.55f), "Glow");
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0f, 1f);
            glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            glow.rectTransform.anchoredPosition = new Vector2(240, -300);
            glow.rectTransform.sizeDelta = new Vector2(460, 460);
            var rays = UIKit.Img(glow.transform, ArtCache.Rays, new Color(1f, 0.9f, 0.6f, 0.25f), "Rays");
            rays.rectTransform.Stretch(-40, -40, -40, -40);
            rays.gameObject.AddComponent<Spin>().speed = 12f;

            var pedestal = UIKit.Img(panel, ArtCache.Shadow, new Color(0, 0, 0, 0.5f), "Pedestal");
            pedestal.rectTransform.anchorMin = pedestal.rectTransform.anchorMax = new Vector2(0f, 1f);
            pedestal.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pedestal.rectTransform.anchoredPosition = new Vector2(240, -478);
            pedestal.rectTransform.sizeDelta = new Vector2(300, 60);

            heroArt = UIKit.Img(panel, null, Color.white, "Hero");
            heroArt.rectTransform.anchorMin = heroArt.rectTransform.anchorMax = new Vector2(0f, 1f);
            heroArt.rectTransform.pivot = new Vector2(0.5f, 0f);
            heroArt.rectTransform.anchoredPosition = new Vector2(240, -490);
            var bob = heroArt.gameObject.AddComponent<Bob>();
            bob.amplitude = 5f;
            bob.speed = 2.2f;

            heroName = UIKit.Label(panel, "", 92, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Name");
            heroName.rectTransform.Place(0f, 1f, 480, -36, 500, 96);
            heroTitle = UIKit.Label(panel, "", 46, UIKit.Gold, true, TextAnchor.MiddleLeft, "Title");
            heroTitle.rectTransform.Place(0f, 1f, 482, -128, 500, 56);

            levelText = UIKit.Label(panel, "", 46, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Level");
            levelText.rectTransform.Place(0f, 1f, 482, -196, 500, 56);
            levelBar = UIKit.Bar(panel, UIKit.AllyCol, "LevelBar");
            levelBar.rectTransform().Place(0f, 1f, 482, -256, 470, 40);

            float y = -320;
            hpText = StatRow(panel, "icon_heart", ref y, out hpNext, out _);
            atkText = StatRow(panel, "icon_sword", ref y, out atkNext, out _);
            styleText = StatRow(panel, "icon_volley", ref y, out _, out styleIcon);

            var abilityBox = UIKit.Frame(panel, "inset", "AbilityBox").rectTransform;
            abilityBox.Place(0.5f, 1f, 0, -530, 920, 130);
            ability = UIKit.Para(abilityBox, "", 40, new Color(0.92f, 0.9f, 1f), TextAnchor.MiddleCenter, "Ability");
            ability.rectTransform.Stretch(24, 10, 24, 10);

            // Товчнууд
            upgradeBtn = UIKit.Btn(panel, null, "btn_green", 0, OnUpgrade, "Upgrade");
            upgradeBtn.rectTransform().Place(0f, 0f, 40, 40, 450, 150);
            var ul = UIKit.Label(upgradeBtn.transform, "LEVEL UP", 52, UIKit.TextCol, true, TextAnchor.MiddleCenter, "Text");
            ul.rectTransform.Place(0.5f, 1f, 0, -14, 420, 64);
            upgradeBtn.label = ul;
            upgradeCost = UIKit.IconValue(upgradeBtn.transform, "ui_coin", "0", 46, UIKit.TextCol, 0.9f, out upgradeCoin);
            ((RectTransform)upgradeCost.transform.parent).At(0.5f, 0f, 0, 44, 200, 56);

            actionBtn = UIKit.Btn(panel, null, "btn_blue", 0, OnAction, "Action");
            actionBtn.rectTransform().Place(1f, 0f, -40, 40, 450, 150);
            actionLabel = UIKit.Label(actionBtn.transform, "", 56, UIKit.TextCol, true, TextAnchor.MiddleCenter, "Text");
            actionLabel.rectTransform.Stretch(10, 12, 10, 4);
            actionBtn.label = actionLabel;
            actionCost = UIKit.IconValue(actionBtn.transform, "ui_gem", "0", 46, UIKit.TextCol, 0.9f, out actionIcon);
            ((RectTransform)actionCost.transform.parent).At(0.5f, 0f, 0, 44, 200, 56);
        }

        Text StatRow(Transform panel, string icon, ref float y, out Text next, out Image iconImg)
        {
            var row = UIKit.Rect("Stat_" + icon, panel).Place(0f, 1f, 482, y, 480, 62);
            iconImg = UIKit.Pix(row, icon, 0.9f, "Icon");
            iconImg.rectTransform.Place(0f, 0.5f, 0, 0, 70, 70);
            var t = UIKit.Label(row, "", 46, UIKit.TextCol, false, TextAnchor.MiddleLeft, "Value");
            t.rectTransform.Place(0f, 0.5f, 86, 0, 260, 60);
            next = UIKit.Label(row, "", 40, UIKit.GoodCol, false, TextAnchor.MiddleLeft, "Next");
            next.rectTransform.Place(0f, 0.5f, 320, 0, 160, 60);
            y -= 68;
            return t;
        }

        // ---------------- Баатрын эгнээ ----------------

        void BuildRow(RectTransform parent)
        {
            var row = UIKit.Rect("Heroes", parent).TopStrip(310, 860);
            int n = Heroes.All.Length;
            for (int i = 0; i < n; i++)
            {
                var h = Heroes.All[i];
                var cell = new Cell { hero = h };
                float x = (i + 0.5f) / n;
                var holder = UIKit.Rect("Cell_" + h.id, row);
                holder.At(x, 0.5f, 0, 0, 236, 296);

                cell.highlight = UIKit.Frame(holder, "btn_gold", "Highlight");
                cell.highlight.rectTransform.Stretch(-10, -10, -10, -10);
                cell.button = UIKit.Btn(holder, null, "panel", 0, () => View(h), "Button");
                cell.button.rectTransform().Fill();
                cell.button.pressScale = 0.94f;

                cell.art = UIKit.Pix(cell.button.transform, h.unit.sprite, 1.75f, "Art");
                cell.art.rectTransform.anchorMin = cell.art.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                cell.art.rectTransform.pivot = new Vector2(0.5f, 1f);
                cell.art.rectTransform.anchoredPosition = new Vector2(0, -30);

                cell.name = UIKit.Label(cell.button.transform, h.name, 46, UIKit.TextCol, true, TextAnchor.MiddleCenter, "Name");
                cell.name.rectTransform.BottomStrip(56, 64);
                cell.status = UIKit.Label(cell.button.transform, "", 36, UIKit.Dim, true, TextAnchor.MiddleCenter, "Status");
                cell.status.rectTransform.BottomStrip(48, 22);

                cell.check = UIKit.Pix(holder, "ui_check", 1.2f, "Check");
                cell.check.rectTransform.Place(1f, 1f, 14, 14, 70, 70);
                cell.lockIcon = UIKit.Pix(cell.button.transform, "ui_lock", 1.2f, "Lock");
                cell.lockIcon.rectTransform.Place(0f, 1f, 16, -16, 60, 60);
                cells.Add(cell);
            }
        }

        // ---------------- Арми ----------------

        void BuildArmy(RectTransform parent)
        {
            var panel = UIKit.Frame(parent, "panel", "Army").rectTransform;
            panel.TopStrip(340, 1190);
            panel.offsetMin = new Vector2(30, panel.offsetMin.y);
            panel.offsetMax = new Vector2(-30, panel.offsetMax.y);

            var title = UIKit.Label(panel, "ARMY", 64, UIKit.Gold, true, TextAnchor.MiddleLeft, "Title");
            title.rectTransform.Place(0f, 1f, 40, -24, 300, 76);
            armyLevel = UIKit.Label(panel, "", 50, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Level");
            armyLevel.rectTransform.Place(0f, 1f, 210, -30, 300, 66);

            string[] troops = { "footman", "bowman", "guardian", "ranger", "champion" };
            for (int i = 0; i < troops.Length; i++)
            {
                var t = UIKit.Pix(panel, troops[i], 1.25f, troops[i]);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 0f);
                t.rectTransform.pivot = new Vector2(0.5f, 0f);
                t.rectTransform.anchoredPosition = new Vector2(90 + i * 106, 100);
                var bob = t.gameObject.AddComponent<Bob>();
                bob.amplitude = 3f;
                bob.speed = 2.5f;
                bob.phase = i * 0.9f;
            }

            armyBonus = UIKit.Label(panel, "", 38, UIKit.Dim, false, TextAnchor.MiddleLeft, "Bonus");
            armyBonus.rectTransform.Place(0f, 0f, 40, 34, 560, 56);

            armyBtn = UIKit.Btn(panel, null, "btn_green", 0, OnArmyUpgrade, "Upgrade");
            armyBtn.rectTransform().Place(1f, 0.5f, -36, -10, 380, 170);
            var al = UIKit.Label(armyBtn.transform, "TRAIN", 56, UIKit.TextCol, true, TextAnchor.MiddleCenter, "Text");
            al.rectTransform.Place(0.5f, 1f, 0, -16, 360, 66);
            armyBtn.label = al;
            armyCost = UIKit.IconValue(armyBtn.transform, "ui_coin", "0", 46, UIKit.TextCol, 0.9f, out armyCoin);
            ((RectTransform)armyCost.transform.parent).At(0.5f, 0f, 0, 50, 200, 56);
        }

        // ---------------- Шинэчлэх ----------------

        public override void OnShow()
        {
            viewing = Save.SelectedHero;
            Refresh();
            Save.Changed += Refresh;
        }

        public override void OnHide() => Save.Changed -= Refresh;

        void View(HeroDef h)
        {
            if (viewing == h) return;
            viewing = h;
            Refresh();
            ui.StartCoroutine(UIKit.Pop(heroArt.transform, 0.7f, 0.3f));
        }

        void Refresh()
        {
            var p = Save.P;
            var h = viewing ?? Save.SelectedHero;
            bool owned = Save.OwnsHero(h.id);
            bool selected = p.selectedHero == h.id;
            int level = Save.HeroLevel(h.id);
            int cost = Progression.HeroCost(level);

            heroArt.SetPix(h.unit.sprite, 3.4f);
            heroArt.color = owned ? Color.white : new Color(0.55f, 0.55f, 0.65f, 1f);
            heroArt.rectTransform.anchoredPosition = new Vector2(240, -490);
            heroArt.GetComponent<Bob>().Rebase();
            glow.color = owned ? new Color(1f, 0.85f, 0.4f, 0.55f) : new Color(0.6f, 0.6f, 0.8f, 0.3f);
            heroName.text = h.name;
            heroTitle.text = h.title;
            levelText.text = "LEVEL " + level + " / " + Progression.MaxLevel;
            levelBar.Set(level / (float)Progression.MaxLevel, true);
            ability.text = h.ability;

            float mul = Balance.LevelMul(level), next = Balance.LevelMul(level + 1);
            int hp = Mathf.RoundToInt(h.unit.hp * mul), atk = Mathf.RoundToInt(h.unit.atk * mul);
            hpText.text = "HP  " + hp.ToString("N0");
            atkText.text = "ATK  " + atk;
            bool canGrow = owned && cost > 0;
            hpNext.text = canGrow ? "+" + (Mathf.RoundToInt(h.unit.hp * next) - hp) : "";
            atkNext.text = canGrow ? "+" + (Mathf.RoundToInt(h.unit.atk * next) - atk) : "";
            styleText.text = h.unit.ranged ? "RANGED" : "MELEE";
            styleIcon.SetPix(h.unit.ranged ? "icon_volley" : "icon_shield", 0.9f);
            styleIcon.rectTransform.sizeDelta = new Vector2(70, 70);

            // Түвшин ахиулах
            upgradeBtn.gameObject.SetActive(owned);
            if (owned)
            {
                if (cost > 0)
                {
                    upgradeBtn.SetLabel("LEVEL UP");
                    upgradeCost.text = cost.ToString("N0");
                    UIKit.FitIconValue(upgradeCost);
                    upgradeCost.transform.parent.gameObject.SetActive(true);
                    upgradeBtn.Interactable = true;
                    upgradeCost.color = p.gold >= cost ? UIKit.TextCol : new Color(1f, 0.55f, 0.55f);
                }
                else
                {
                    upgradeBtn.SetLabel("MAX LEVEL");
                    upgradeCost.transform.parent.gameObject.SetActive(false);
                    upgradeBtn.Interactable = false;
                }
            }

            // Сонгох / нээх
            var actionRt = actionBtn.rectTransform();
            if (!owned)
            {
                actionRt.Place(0.5f, 0f, 0, 40, 600, 150);
                bool gems = h.gemCost > 0;
                actionBtn.frame.SetFrame(gems ? "btn_purple" : "btn_gold");
                actionLabel.rectTransform.Place(0.5f, 1f, 0, -14, 560, 64);
                actionLabel.text = "UNLOCK";
                actionCost.transform.parent.gameObject.SetActive(true);
                actionIcon.SetPix(gems ? "ui_gem" : "ui_coin", 0.9f);
                actionCost.text = (gems ? h.gemCost : h.goldCost).ToString("N0");
                UIKit.FitIconValue(actionCost);
                actionBtn.Interactable = true;
            }
            else
            {
                actionRt.Place(1f, 0f, -40, 40, 450, 150);
                actionLabel.rectTransform.Stretch(10, 12, 10, 4);
                actionCost.transform.parent.gameObject.SetActive(false);
                actionBtn.frame.SetFrame(selected ? "btn_gray" : "btn_blue");
                actionLabel.text = selected ? "SELECTED" : "SELECT";
                actionBtn.Interactable = true;
            }

            // Эгнээ
            foreach (var cell in cells)
            {
                bool own = Save.OwnsHero(cell.hero.id);
                bool sel = p.selectedHero == cell.hero.id;
                cell.highlight.gameObject.SetActive(cell.hero == h);
                cell.check.gameObject.SetActive(sel);
                cell.lockIcon.gameObject.SetActive(!own);
                cell.art.color = own ? Color.white : new Color(0.35f, 0.35f, 0.45f, 1f);
                if (own)
                {
                    cell.status.text = "LV " + Save.HeroLevel(cell.hero.id);
                    cell.status.color = UIKit.Dim;
                }
                else if (cell.hero.gemCost > 0)
                {
                    cell.status.text = cell.hero.gemCost + " GEMS";
                    cell.status.color = UIKit.GemCol;
                }
                else
                {
                    cell.status.text = cell.hero.goldCost.ToString("N0") + " GOLD";
                    cell.status.color = UIKit.Gold;
                }
            }

            // Арми
            int tl = p.troopLevel;
            int tcost = Progression.TroopCost(tl);
            armyLevel.text = "LV " + tl;
            int bonus = Mathf.RoundToInt((Balance.LevelMul(tl) - 1f) * 100f);
            armyBonus.text = tcost > 0
                ? "All troops +" + bonus + "% HP & ATK  (next: +" + Mathf.RoundToInt((Balance.LevelMul(tl + 1) - 1f) * 100f) + "%)"
                : "All troops +" + bonus + "% HP & ATK  (max)";
            if (tcost > 0)
            {
                armyBtn.SetLabel("TRAIN");
                armyCost.text = tcost.ToString("N0");
                UIKit.FitIconValue(armyCost);
                armyCost.transform.parent.gameObject.SetActive(true);
                armyBtn.Interactable = true;
                armyCost.color = p.gold >= tcost ? UIKit.TextCol : new Color(1f, 0.55f, 0.55f);
            }
            else
            {
                armyBtn.SetLabel("MAX");
                armyCost.transform.parent.gameObject.SetActive(false);
                armyBtn.Interactable = false;
            }
        }

        // ---------------- Үйлдэл ----------------

        void OnUpgrade()
        {
            var h = viewing;
            int level = Save.HeroLevel(h.id);
            int cost = Progression.HeroCost(level);
            if (cost <= 0) return;
            if (!Save.SpendGold(cost))
            {
                NotEnough(false);
                return;
            }
            Save.SetHeroLevel(h.id, level + 1);
            AudioManager.Instance?.Play("levelup");
            Haptics.Medium();
            ui.StartCoroutine(UIKit.Pop(heroArt.transform, 0.6f, 0.4f));
            ui.Toast(h.name + " reached level " + (level + 1) + "!", UIKit.Gold);
        }

        void OnAction()
        {
            var h = viewing;
            if (!Save.OwnsHero(h.id))
            {
                bool gems = h.gemCost > 0;
                int price = gems ? h.gemCost : h.goldCost;
                if ((gems ? Save.P.gems : Save.P.gold) < price)
                {
                    NotEnough(gems);
                    return;
                }
                ui.Confirm("Unlock " + h.name + "?", "Unlock " + h.name + " the " + h.title + " for " + price.ToString("N0") + (gems ? " gems?" : " gold?"), "Unlock", () =>
                {
                    bool ok = gems ? Save.SpendGems(price) : Save.SpendGold(price);
                    if (!ok) { NotEnough(gems); return; }
                    Save.P.selectedHero = h.id;
                    Save.UnlockHero(h.id);
                    ui.RewardPopup("NEW HERO!", h.unit.sprite, null, h.name + " joins your side!", 0, 0);
                });
                return;
            }
            if (Save.P.selectedHero == h.id)
            {
                ui.Show(ui.home);
                return;
            }
            Save.P.selectedHero = h.id;
            Save.Commit();
            AudioManager.Instance?.Play("buy");
            ui.StartCoroutine(UIKit.Punch(heroArt.transform, 0.2f, 0.3f));
        }

        void OnArmyUpgrade()
        {
            int tl = Save.P.troopLevel;
            int cost = Progression.TroopCost(tl);
            if (cost <= 0) return;
            if (!Save.SpendGold(cost))
            {
                NotEnough(false);
                return;
            }
            Save.P.troopLevel = tl + 1;
            Save.Commit();
            AudioManager.Instance?.Play("levelup");
            Haptics.Medium();
            ui.Toast("Army reached level " + (tl + 1) + "!", UIKit.Gold);
        }

        void NotEnough(bool gems)
        {
            ui.Confirm(gems ? "Not enough gems" : "Not enough gold",
                gems ? "Get more gems in the shop?" : "Win battles for gold, or trade gems for gold in the shop.",
                "Shop", () => ui.Show(ui.shop));
        }
    }
}
