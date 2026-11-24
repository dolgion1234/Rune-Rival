using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>Нэг дэлгэц (нүүр, баатрууд, тулаан...).</summary>
    public abstract class UiScreen
    {
        public RectTransform root;
        protected UIManager ui;

        public virtual bool ShowTopBar => true;
        public virtual bool ShowNav => true;
        public virtual int NavIndex => -1;

        public void Create(UIManager owner, Transform parent, string name)
        {
            ui = owner;
            root = UIKit.Rect(name, parent).Fill();
            Build();
            root.gameObject.SetActive(false);
        }

        protected abstract void Build();
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        public virtual void Tick(float dt) { }
        /// <summary>true = буцах товчийг өөрөө шийдсэн.</summary>
        public virtual bool Back() => false;

        /// <summary>Дээд/доод самбарын хоорондох агуулгын талбай.</summary>
        protected RectTransform Content(string name = "Content") =>
            UIKit.Rect(name, root).Stretch(0, UIManager.NavHeight, 0, UIManager.TopHeight);

        protected Text Title(Transform parent, string text, float fromTop = 20f)
        {
            var t = UIKit.Label(parent, text, 96, UIKit.TextCol, true);
            t.rectTransform.TopStrip(100, fromTop);
            return t;
        }
    }

    /// <summary>Попап цонх.</summary>
    public sealed class Popup
    {
        public RectTransform root, panel, body;
        public Text title;
        public Action onClose;
        public bool closable = true;
        UIManager ui;

        public Popup(UIManager ui, Transform parent, string titleText, float w, float h)
        {
            this.ui = ui;
            root = UIKit.Rect("Popup", parent).Fill();
            var blocker = UIKit.Blocker(root, UIKit.Shade);
            var hit = blocker.gameObject.AddComponent<UiButton>();
            hit.pressScale = 1f;
            hit.sound = null;
            hit.haptic = false;
            hit.onClick = () => { if (closable) Close(); };
            var frame = UIKit.Frame(root, "panel", "Panel", true);
            panel = frame.rectTransform.Place(0.5f, 0.5f, 0, 0, w, h);
            if (!string.IsNullOrEmpty(titleText))
            {
                title = UIKit.Label(panel, titleText, 84, UIKit.Gold, true);
                title.rectTransform.TopStrip(110, 18);
            }
            body = UIKit.Rect("Body", panel).Stretch(40, 40, 40, string.IsNullOrEmpty(titleText) ? 40 : 140);
            var close = UIKit.IconBtn(panel, "ui_back", "btn_red", 0.8f, () => Close(), "Close");
            close.icon.SetPix("ui_plus", 1f);
            close.icon.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            close.icon.color = Color.white;
            close.rectTransform().Place(1f, 1f, 30, 30, 110, 110);
            closeButton = close;
            ui.StartCoroutine(UIKit.Pop(panel, 0.7f, 0.28f));
            AudioManager.Instance?.Play("whoosh", 0.5f);
        }

        public UiButton closeButton;

        public void HideCloseButton() => closeButton.gameObject.SetActive(false);

        public void Close()
        {
            if (root == null) return;
            ui.RemovePopup(this);
            UnityEngine.Object.Destroy(root.gameObject);
            root = null;
            onClose?.Invoke();
        }

        public bool IsOpen => root != null;
    }

    public static class RectExt
    {
        public static RectTransform rectTransform(this Component c) => (RectTransform)c.transform;
    }

    /// <summary>
    /// Бүх UI-г бүтээж удирдана: дэлгэцүүд, дээд самбар (мөнгө), доод цэс, попап, мэдэгдэл, туршилтын зар.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public const float TopHeight = 150f;
        public const float NavHeight = 215f;

        public Canvas canvas;
        public RectTransform canvasRect, bgLayer, safe, screenLayer, hudLayer, popupLayer, toastLayer, overlayLayer;

        public HomeScreen home;
        public HeroesScreen heroes;
        public CardsScreen cards;
        public LeagueScreen league;
        public ShopScreen shop;
        public BattleScreen battle;

        readonly List<UiScreen> screens = new List<UiScreen>();
        readonly List<Popup> popups = new List<Popup>();
        UiScreen current;

        // Дээд самбар
        RectTransform topBar;
        Text goldText, gemText, trophyText, nameText;
        Image topEmblem;
        int shownGold = -1, shownGems = -1;

        // Доод цэс
        RectTransform nav;
        readonly List<NavTab> tabs = new List<NavTab>();

        // Дэвсгэр
        Image menuBg;

        sealed class NavTab
        {
            public UiButton button;
            public Image icon, glow;
            public Text label;
            public UiScreen screen;
        }

        public bool ModalOpen => popups.Count > 0 || adOverlay != null;

        void Awake() => Instance = this;

        public void Build()
        {
            var go = new GameObject("UI Canvas", typeof(RectTransform));
            go.layer = 5;
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;
            go.AddComponent<CanvasAspect>();
            go.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(go);
            canvasRect = (RectTransform)go.transform;

            bgLayer = UIKit.Rect("Background", canvasRect).Fill();
            menuBg = UIKit.Img(bgLayer, ArtCache.MenuPattern, Color.white, "MenuPattern");
            menuBg.type = Image.Type.Tiled;
            menuBg.pixelsPerUnitMultiplier = 1f;
            menuBg.rectTransform.Fill();

            safe = UIKit.Rect("SafeArea", canvasRect);
            safe.gameObject.AddComponent<SafeArea>();
            screenLayer = UIKit.Rect("Screens", safe).Fill();
            hudLayer = UIKit.Rect("Hud", safe).Fill();
            popupLayer = UIKit.Rect("Popups", safe).Fill();
            toastLayer = UIKit.Rect("Toasts", safe).Fill();
            overlayLayer = UIKit.Rect("Overlay", canvasRect).Fill();

            BuildTopBar();
            BuildNav();

            home = Add(new HomeScreen(), "Home");
            heroes = Add(new HeroesScreen(), "Heroes");
            cards = Add(new CardsScreen(), "Cards");
            league = Add(new LeagueScreen(), "League");
            shop = Add(new ShopScreen(), "Shop");
            battle = Add(new BattleScreen(), "Battle");

            tabs[0].screen = shop;
            tabs[1].screen = heroes;
            tabs[2].screen = home;
            tabs[3].screen = cards;
            tabs[4].screen = league;

            Save.Changed += RefreshTopBar;
            if (IAPManager.Instance != null) IAPManager.Instance.OnMessage += msg => Toast(msg);
            RefreshTopBar();
        }

        T Add<T>(T s, string name) where T : UiScreen
        {
            s.Create(this, screenLayer, name);
            screens.Add(s);
            return s;
        }

        void OnDestroy() => Save.Changed -= RefreshTopBar;

        // ================= Дэлгэц солих =================

        public void ShowHome() => Show(home);

        public void Show(UiScreen s)
        {
            if (s == current) return;
            CloseAllPopups();
            if (current != null)
            {
                current.OnHide();
                current.root.gameObject.SetActive(false);
            }
            current = s;
            s.root.gameObject.SetActive(true);
            topBar.gameObject.SetActive(s.ShowTopBar);
            nav.gameObject.SetActive(s.ShowNav);
            menuBg.gameObject.SetActive(s != home);
            for (int i = 0; i < tabs.Count; i++) SetTab(tabs[i], tabs[i].screen == s);
            s.OnShow();
            var g = s.root.Group();
            g.alpha = 0f;
            StartCoroutine(UIKit.Fade(g, 1f, 0.15f));
            RefreshTopBar();
        }

        public UiScreen Current => current;

        void Update()
        {
            current?.Tick(Time.deltaTime);
            AnimateTopBar();
        }

        public bool HandleBack()
        {
            if (adOverlay != null) return true;
            if (popups.Count > 0)
            {
                var top = popups[popups.Count - 1];
                if (top.closable) top.Close();
                return true;
            }
            if (current != null && current.Back()) return true;
            if (current != home)
            {
                Show(home);
                return true;
            }
            Confirm("Quit?", "Leave Rune Rivals?", "Quit", Application.Quit);
            return true;
        }

        public void OnAppPaused()
        {
            if (current == battle) battle.OnAppPaused();
        }

        // ================= Дээд самбар =================

        void BuildTopBar()
        {
            topBar = UIKit.Rect("TopBar", hudLayer).TopStrip(TopHeight);
            var shade = UIKit.Block(topBar, new Color(0.06f, 0.04f, 0.12f, 0.55f), "Shade");
            shade.rectTransform.Fill();

            // Тоглогч: лигийн тэмдэг + нэр + цом (дарахад Лиг)
            var player = UIKit.HitArea(topBar, () => Show(league), "Player");
            player.rectTransform().Place(0f, 0.5f, 16, 0, 430, 130);
            topEmblem = UIKit.Pix(player.transform, "ui_trophy", 1f, "Emblem");
            topEmblem.sprite = ArtCache.Emblem(0);
            topEmblem.rectTransform.Place(0f, 0.5f, 0, 0, 28 * 4, 32 * 4);
            nameText = UIKit.Label(player.transform, "", 44, UIKit.TextCol, false, TextAnchor.LowerLeft, "Name");
            nameText.rectTransform.Place(0f, 0.5f, 125, 6, 300, 60);
            var trophyRow = UIKit.Rect("Trophies", player.transform).Place(0f, 0.5f, 125, -50, 300, 56);
            trophyText = UIKit.IconValue(trophyRow, "ui_trophy", "0", 52, UIKit.Gold, 0.9f, out _);

            // Зоос, эрдэнэ
            var gems = Chip(topBar, "ui_gem", new Vector2(-16, 0), out gemText, () => Show(shop));
            var gold = Chip(topBar, "ui_coin", new Vector2(-16 - 280, 0), out goldText, () => Show(shop));
            gemText.color = UIKit.GemCol;
            goldText.color = UIKit.Gold;
        }

        RectTransform Chip(Transform parent, string icon, Vector2 pos, out Text value, Action onClick)
        {
            var b = UIKit.Btn(parent, null, "chip", 0, onClick, "Chip_" + icon);
            b.pressScale = 0.95f;
            var rt = b.rectTransform().Place(1f, 0.5f, pos.x, pos.y, 262, 96);
            var ic = UIKit.Pix(rt, icon, 1.2f, "Icon");
            ic.rectTransform.Place(0f, 0.5f, -14, 2, ic.rectTransform.sizeDelta.x, ic.rectTransform.sizeDelta.y);
            value = UIKit.Label(rt, "0", 56, UIKit.TextCol, true, TextAnchor.MiddleRight, "Value");
            value.rectTransform.Stretch(60, 4, 64, 0);
            var plus = UIKit.Pix(rt, "ui_plus", 1.1f, "Plus");
            plus.rectTransform.Place(1f, 0.5f, -14, 2, 40, 40);
            return rt;
        }

        public void RefreshTopBar()
        {
            if (topBar == null) return;
            var p = Save.P;
            nameText.text = p.playerName;
            trophyText.text = p.trophies.ToString("N0");
            topEmblem.sprite = ArtCache.Emblem(League.TierOf(p.trophies));
            if (shownGold < 0) { shownGold = p.gold; shownGems = p.gems; }
            UpdateNavBadges();
        }

        void AnimateTopBar()
        {
            var p = Save.P;
            if (shownGold != p.gold)
            {
                int step = Mathf.Max(1, Mathf.Abs(p.gold - shownGold) / 12);
                shownGold = shownGold < p.gold ? Mathf.Min(p.gold, shownGold + step) : Mathf.Max(p.gold, shownGold - step);
            }
            if (shownGems != p.gems)
            {
                int step = Mathf.Max(1, Mathf.Abs(p.gems - shownGems) / 12);
                shownGems = shownGems < p.gems ? Mathf.Min(p.gems, shownGems + step) : Mathf.Max(p.gems, shownGems - step);
            }
            if (goldText != null) goldText.text = UIKit.Num(shownGold);
            if (gemText != null) gemText.text = UIKit.Num(shownGems);
        }

        // ================= Доод цэс =================

        readonly string[] tabIcons = { "ui_cart", "ui_helmet", "ui_swords", "ui_cards", "ui_trophy" };
        readonly string[] tabNames = { "Shop", "Heroes", "Battle", "Cards", "League" };
        Image heroesBadge;

        void BuildNav()
        {
            nav = UIKit.Rect("Nav", hudLayer).BottomStrip(NavHeight);
            var bg = UIKit.Frame(nav, "panel", "Bg");
            bg.rectTransform.Stretch(-12, -40, -12, 0);
            float w = 1f / tabIcons.Length;
            for (int i = 0; i < tabIcons.Length; i++)
            {
                int idx = i;
                var tab = new NavTab();
                var cell = UIKit.Rect("Tab" + i, nav);
                cell.anchorMin = new Vector2(w * i, 0);
                cell.anchorMax = new Vector2(w * (i + 1), 1);
                cell.offsetMin = new Vector2(4, 14);
                cell.offsetMax = new Vector2(-4, -12);
                tab.glow = UIKit.Frame(cell, "btn_blue", "Active");
                tab.glow.rectTransform.Stretch(0, 0, 0, 0);
                tab.button = UIKit.HitArea(cell, () => OnTab(idx), "Hit");
                tab.button.rectTransform().Fill();
                tab.icon = UIKit.Pix(cell, tabIcons[i], 1.5f, "Icon");
                tab.icon.rectTransform.anchoredPosition = new Vector2(0, 18);
                tab.label = UIKit.Label(cell, tabNames[i], 40, UIKit.TextCol, true, TextAnchor.LowerCenter, "Label");
                tab.label.rectTransform.BottomStrip(50, 10);
                tabs.Add(tab);
            }
            heroesBadge = UIKit.Pix(tabs[1].icon.transform.parent, "ui_plus", 1f, "Badge");
            heroesBadge.rectTransform.Place(1f, 1f, -14, -8, 44, 44);
        }

        void SetTab(NavTab t, bool active)
        {
            t.glow.gameObject.SetActive(active);
            t.icon.rectTransform.localScale = Vector3.one * (active ? 1.25f : 1f);
            t.icon.rectTransform.anchoredPosition = new Vector2(0, active ? 26 : 18);
            t.icon.color = active ? Color.white : new Color(0.75f, 0.72f, 0.85f, 1f);
            t.label.gameObject.SetActive(active);
        }

        void OnTab(int i)
        {
            var s = tabs[i].screen;
            if (s == current) return;
            Show(s);
            StartCoroutine(UIKit.Punch(tabs[i].icon.transform, 0.2f, 0.25f));
        }

        void UpdateNavBadges()
        {
            if (heroesBadge == null) return;
            // Алт хүрэлцэх ахиулалт байвал Heroes дээр "+" тэмдэг
            var p = Save.P;
            int heroCost = Progression.HeroCost(Save.HeroLevel(p.selectedHero));
            int troopCost = Progression.TroopCost(p.troopLevel);
            bool can = (heroCost > 0 && p.gold >= heroCost) || (troopCost > 0 && p.gold >= troopCost);
            heroesBadge.gameObject.SetActive(can);
        }

        // ================= Попап =================

        public Popup OpenPopup(string title, float w, float h)
        {
            var p = new Popup(this, popupLayer, title, w, h);
            popups.Add(p);
            return p;
        }

        public void RemovePopup(Popup p) => popups.Remove(p);

        public void CloseAllPopups()
        {
            for (int i = popups.Count - 1; i >= 0; i--) popups[i].Close();
        }

        public Popup Confirm(string title, string message, string yes, Action onYes, string no = "Cancel")
        {
            var p = OpenPopup(title, 860, 640);
            var msg = UIKit.Para(p.body, message, 50, UIKit.TextCol, TextAnchor.UpperCenter);
            msg.rectTransform.Stretch(0, 150, 0, 0);
            var noBtn = UIKit.Btn(p.body, no, "btn_gray", 60, () => p.Close(), "No");
            noBtn.rectTransform().Place(0f, 0f, 0, 0, 360, 130);
            var yesBtn = UIKit.Btn(p.body, yes, "btn_green", 60, () => { p.Close(); onYes?.Invoke(); }, "Yes");
            yesBtn.rectTransform().Place(1f, 0f, 0, 0, 360, 130);
            return p;
        }

        /// <summary>Шагналын попап (лиг ахих, дэлгүүр).</summary>
        public Popup RewardPopup(string title, string sprite, Sprite customSprite, string line, int gold, int gems, Action onClose = null)
        {
            var p = OpenPopup(title, 860, 900);
            p.onClose = onClose;
            var rays = UIKit.Img(p.body, ArtCache.Rays, new Color(1f, 0.9f, 0.5f, 0.6f), "Rays");
            rays.rectTransform.Place(0.5f, 1f, 0, 40, 620, 620);
            rays.gameObject.AddComponent<Spin>().speed = 20f;
            Image icon;
            if (customSprite != null)
            {
                icon = UIKit.Img(p.body, customSprite, Color.white, "Icon");
                icon.rectTransform.Place(0.5f, 1f, 0, -20, 280, 320);
                icon.preserveAspect = true;
            }
            else
            {
                icon = UIKit.Pix(p.body, sprite, 3f, "Icon");
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0.5f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(0, -60);
            }
            StartCoroutine(UIKit.Pop(icon.transform, 0.3f, 0.5f));
            var t = UIKit.Para(p.body, line, 46, UIKit.TextCol, TextAnchor.MiddleCenter);
            t.rectTransform.Place(0.5f, 0f, 0, 270, 760, 130);
            var row = UIKit.Rect("Rewards", p.body).Place(0.5f, 0f, 0, 170, 720, 80);
            if (gold > 0)
            {
                var g = UIKit.IconValue(row, "ui_coin", "+" + gold.ToString("N0"), 64, UIKit.Gold, 1.3f, out _);
                ((RectTransform)g.transform.parent).At(gems > 0 ? 0.25f : 0.5f, 0.5f, 40, 0, 300, 80);
            }
            if (gems > 0)
            {
                var g = UIKit.IconValue(row, "ui_gem", "+" + gems.ToString("N0"), 64, UIKit.GemCol, 1.3f, out _);
                ((RectTransform)g.transform.parent).At(gold > 0 ? 0.75f : 0.5f, 0.5f, 40, 0, 300, 80);
            }
            var ok = UIKit.Btn(p.body, "Awesome!", "btn_green", 64, () => p.Close(), "Ok");
            ok.rectTransform().Place(0.5f, 0f, 0, 0, 480, 140);
            AudioManager.Instance?.Play("levelup");
            Haptics.Medium();
            return p;
        }

        // ================= Мэдэгдэл =================

        public void Toast(string message, Color? color = null)
        {
            if (string.IsNullOrEmpty(message)) return;
            var box = UIKit.Frame(toastLayer, "chip", "Toast");
            var rt = box.rectTransform.Place(0.5f, 1f, 0, -TopHeight - 20, 900, 110);
            var t = UIKit.Label(rt, message, 46, color ?? UIKit.TextCol, false);
            t.rectTransform.Stretch(20, 6, 20, 0);
            float width = Mathf.Clamp(t.preferredWidth + 90, 360, 1000);
            rt.sizeDelta = new Vector2(width, 110);
            StartCoroutine(ToastLife(rt));
        }

        IEnumerator ToastLife(RectTransform rt)
        {
            var g = rt.Group();
            g.blocksRaycasts = false;
            g.alpha = 0f;
            yield return UIKit.Fade(g, 1f, 0.15f);
            float t = 0f;
            while (t < 1.6f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return UIKit.Fade(g, 0f, 0.3f);
            if (rt != null) Destroy(rt.gameObject);
        }

        // ================= Туршилтын зар =================

        RectTransform adOverlay;

        /// <summary>Зарын оронд 3 секундын дэлгэц (туршилтын горим).</summary>
        public void ShowTestAd(bool rewarded, string placement, Action<bool> done)
        {
            if (adOverlay != null) { done?.Invoke(false); return; }
            StartCoroutine(TestAdRoutine(rewarded, done));
        }

        IEnumerator TestAdRoutine(bool rewarded, Action<bool> done)
        {
            adOverlay = UIKit.Rect("TestAd", overlayLayer).Fill();
            UIKit.Blocker(adOverlay, new Color(0.04f, 0.03f, 0.08f, 0.96f));
            var box = UIKit.Frame(adOverlay, "panel", "Box").rectTransform.Place(0.5f, 0.5f, 0, 0, 880, 760);
            var icon = UIKit.Pix(box, "ui_ad", 3f, "Icon");
            icon.rectTransform.Place(0.5f, 1f, 0, -70, icon.rectTransform.sizeDelta.x, icon.rectTransform.sizeDelta.y);
            var title = UIKit.Label(box, rewarded ? "REWARD VIDEO" : "AD BREAK", 80, UIKit.Gold, true);
            title.rectTransform.Place(0.5f, 0.5f, 0, 0, 800, 100);
            var sub = UIKit.Para(box, "Test mode: real ads are added later (see the guide).", 40, UIKit.Dim, TextAnchor.UpperCenter);
            sub.rectTransform.Place(0.5f, 0.5f, 0, -110, 760, 120);
            var count = UIKit.Label(box, "3", 72, UIKit.TextCol, true);
            count.rectTransform.Place(0.5f, 0f, 0, 60, 300, 100);
            StartCoroutine(UIKit.Pop(box, 0.8f, 0.25f));
            float t = 3f;
            while (t > 0f)
            {
                count.text = Mathf.CeilToInt(t).ToString();
                t -= Time.unscaledDeltaTime;
                yield return null;
            }
            Destroy(adOverlay.gameObject);
            adOverlay = null;
            done?.Invoke(true);
        }

        /// <summary>"Зар үзэх" товчтой шагнал: амжилттай бол onReward.</summary>
        public void WatchAdFor(string placement, Action onReward)
        {
            if (AdsManager.Instance == null || !AdsManager.Instance.RewardedReady)
            {
                Toast("No video available right now");
                return;
            }
            AdsManager.Instance.ShowRewarded(placement, ok =>
            {
                if (ok) onReward?.Invoke();
                else Toast("Video not completed");
            });
        }

        // ================= Тохиргоо =================

        public void OpenSettings()
        {
            var p = OpenPopup("Settings", 900, 1320);
            var body = p.body;
            float y = 0f;
            Toggle(body, "Sound", () => Save.P.soundOn, v => { Save.P.soundOn = v; Save.Commit(); }, ref y, "ui_sound");
            Toggle(body, "Music", () => Save.P.musicOn, v => { Save.P.musicOn = v; Save.Commit(); AudioManager.Instance?.RefreshMusic(); }, ref y, "ui_music");
            Toggle(body, "Vibration", () => Save.P.vibrationOn, v => { Save.P.vibrationOn = v; Save.Commit(); if (v) Haptics.Medium(); }, ref y, "ui_vibe");

            // Нэр солих
            var nameRow = UIKit.Rect("Name", body).TopStrip(130, y + 10);
            var lbl = UIKit.Label(nameRow, "Name", 50, UIKit.Dim, true, TextAnchor.MiddleLeft);
            lbl.rectTransform.Place(0f, 0.5f, 0, 0, 200, 100);
            var fieldBg = UIKit.Frame(nameRow, "inset", "Field", true);
            fieldBg.rectTransform.Place(1f, 0.5f, 0, 0, 560, 110);
            var input = fieldBg.gameObject.AddComponent<InputField>();
            var txt = UIKit.Label(fieldBg.transform, Save.P.playerName, 48, UIKit.TextCol, false, TextAnchor.MiddleLeft, "Text", false);
            txt.rectTransform.Stretch(24, 4, 24, 4);
            txt.supportRichText = false;
            input.textComponent = txt;
            input.text = Save.P.playerName;
            input.characterLimit = 14;
            input.lineType = InputField.LineType.SingleLine;
            input.onEndEdit.AddListener(v =>
            {
                v = (v ?? "").Trim();
                if (v.Length >= 2)
                {
                    Save.P.playerName = v;
                    Save.Commit();
                }
                else input.text = Save.P.playerName;
            });
            y += 150;

            var restore = UIKit.Btn(body, "Restore Purchases", "btn_blue", 52, () => IAPManager.Instance?.Restore(), "Restore");
            restore.rectTransform().TopStrip(120, y + 20);
            restore.rectTransform().offsetMin = new Vector2(60, restore.rectTransform().offsetMin.y);
            restore.rectTransform().offsetMax = new Vector2(-60, restore.rectTransform().offsetMax.y);
            y += 150;
            var privacy = UIKit.Btn(body, "Privacy Policy", "btn_gray", 52, () => Application.OpenURL(GameConfig.PrivacyPolicyUrl), "Privacy");
            privacy.rectTransform().TopStrip(120, y + 20);
            privacy.rectTransform().offsetMin = new Vector2(60, privacy.rectTransform().offsetMin.y);
            privacy.rectTransform().offsetMax = new Vector2(-60, privacy.rectTransform().offsetMax.y);
            y += 150;
            var tut = UIKit.Btn(body, "Replay Tutorial", "btn_gray", 52, () =>
            {
                Save.P.tutorialStep = 0;
                Save.Commit();
                Toast("Tips will show in your next battle");
            }, "Tutorial");
            tut.rectTransform().TopStrip(120, y + 20);
            tut.rectTransform().offsetMin = new Vector2(60, tut.rectTransform().offsetMin.y);
            tut.rectTransform().offsetMax = new Vector2(-60, tut.rectTransform().offsetMax.y);

            var ver = UIKit.Label(body, GameConfig.ProductName + " v" + Application.version + "\nFonts: Jersey 10, Pixelify Sans (OFL)", 34, UIKit.Dim);
            ver.rectTransform.BottomStrip(90, 0);
        }

        void Toggle(Transform parent, string label, Func<bool> get, Action<bool> set, ref float y, string icon)
        {
            var row = UIKit.Rect(label, parent).TopStrip(120, y);
            var ic = UIKit.Pix(row, icon, 1.4f, "Icon");
            ic.rectTransform.Place(0f, 0.5f, 0, 0, ic.rectTransform.sizeDelta.x, ic.rectTransform.sizeDelta.y);
            var t = UIKit.Label(row, label, 56, UIKit.TextCol, true, TextAnchor.MiddleLeft);
            t.rectTransform.Place(0f, 0.5f, 110, 0, 400, 100);
            UiButton b = null;
            b = UIKit.Btn(row, "", get() ? "btn_green" : "btn_gray", 52, () =>
            {
                bool v = !get();
                set(v);
                b.frame.SetFrame(v ? "btn_green" : "btn_gray");
                b.SetLabel(v ? "ON" : "OFF");
            }, "Switch");
            b.SetLabel(get() ? "ON" : "OFF");
            b.rectTransform().Place(1f, 0.5f, 0, 0, 240, 110);
            y += 130;
        }
    }
}
