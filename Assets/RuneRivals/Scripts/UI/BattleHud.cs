using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>Тулааны дэлгэцийн UI хэсэг: байрлал, HUD, картын самбар, баннер.</summary>
    public sealed partial class BattleScreen
    {
        const float RivalHudH = 140f, MidH = 92f, PlayerHudH = 128f, CardPanelMin = 640f;
        const float CardW = 316f, CardH = 440f;

        sealed class HeroHud
        {
            public RectTransform root;
            public Image portrait;
            public Text name, sub, hpText, ready;
            public UiBar hp;
            public Image frame;
        }

        RectTransform laneHolder, mid, cardPanel, cardsRow, buttonsRow, overlay, bannerLayer, fightCover;
        HeroHud rivalHud, playerHud;
        Text waveText, timerText, phaseText, energyText, incomeText, fightCoverText, fightCoverSub;
        Image timerIcon, energyOrb;
        UiBar timerBar, waveBar;
        readonly CardWidget[] offerCards = new CardWidget[3];
        readonly RectTransform[] offerSlots = new RectTransform[3];
        UiButton refreshBtn, getAllBtn, fightBtn, pauseBtn;
        Text refreshCost;
        RectTransform tickerRoot;
        Vector2 lastSize;
        int shownEnergy = -1;

        // ---- Тулааны үеийн доод самбар: 2 тоглогчийн сонгосон картууд ----
        const int PickCols = 6, PickRows = 2;

        sealed class PickSlot
        {
            public RectTransform root;
            public Image rim, fill, band, icon;
            public Text stack, more;
            public CardDef card;
            public int count;
        }

        sealed class PickBoard
        {
            public RectTransform root;
            public Image portrait;
            public Text title, count;
            public readonly List<PickSlot> slots = new List<PickSlot>();
            public int shown;
        }

        RectTransform waveStrip;
        PickBoard myPicks, rivalPicks;
        readonly List<CardDef> pickOrder = new List<CardDef>();

        // ================= Бүтэц =================

        protected override void Build()
        {
            var bg = UIKit.Block(root, new Color(0.06f, 0.04f, 0.12f, 0.6f), "Dim");
            bg.rectTransform.Stretch(-200, -200, -200, -200);

            laneHolder = UIKit.Rect("Lanes", root).Fill();

            rivalHud = MakeHud(root, false);
            rivalHud.root.TopStrip(RivalHudH, 0);

            mid = UIKit.Rect("Mid", root);
            var midBg = UIKit.Frame(mid, "chip", "Bg");
            midBg.rectTransform.Stretch(6, 4, 6, 4);
            waveText = UIKit.Label(mid, "WAVE 1", 64, UIKit.TextCol, true);
            waveText.rectTransform.Place(0.5f, 0.5f, 0, 2, 420, 84);
            phaseText = UIKit.Label(mid, "", 40, UIKit.Dim, true, TextAnchor.MiddleLeft);
            phaseText.rectTransform.Place(0f, 0.5f, 34, 2, 300, 70);
            var timerBox = UIKit.Rect("Timer", mid).Place(1f, 0.5f, -26, 0, 200, 80);
            timerIcon = UIKit.Pix(timerBox, "ui_timer", 1.3f, "Icon");
            timerIcon.rectTransform.Place(0f, 0.5f, 0, 0, timerIcon.rectTransform.sizeDelta.x, timerIcon.rectTransform.sizeDelta.y);
            timerText = UIKit.Label(timerBox, "15", 60, UIKit.TextCol, true, TextAnchor.MiddleRight);
            timerText.rectTransform.Stretch(70, 0, 0, 0);

            playerHud = MakeHud(root, true);

            BuildCardPanel();

            tickerRoot = UIKit.Rect("Ticker", root);
            bannerLayer = UIKit.Rect("Banners", root).Fill();
            overlay = UIKit.Rect("Overlay", root).Fill();

            pauseBtn = UIKit.IconBtn(rivalHud.root, "ui_pause", "btn_gray", 1.4f, OpenPause, "Pause");
            pauseBtn.rectTransform().Place(1f, 0.5f, -16, 0, 112, 112);
        }

        HeroHud MakeHud(Transform parent, bool player)
        {
            var h = new HeroHud();
            h.root = UIKit.Rect(player ? "PlayerHud" : "RivalHud", parent);
            var bg = UIKit.Block(h.root, new Color(0.08f, 0.06f, 0.16f, 0.85f), "Bg");
            bg.rectTransform.Fill();
            var line = UIKit.Block(h.root, player ? UIKit.AllyCol : UIKit.FoeCol, "Line");
            if (player) line.rectTransform.TopStrip(6, 0); else line.rectTransform.BottomStrip(6, 0);

            h.frame = UIKit.Frame(h.root, "inset", "Portrait");
            h.frame.rectTransform.Place(0f, 0.5f, 16, 0, 112, 112);
            h.portrait = UIKit.Img(h.frame.transform, null, Color.white, "Hero");
            h.portrait.preserveAspect = true;
            h.portrait.rectTransform.Stretch(10, 10, 10, 10);

            h.name = UIKit.Label(h.root, "", 46, player ? UIKit.AllyCol : UIKit.FoeCol, true, TextAnchor.MiddleLeft, "Name");
            h.name.rectTransform.Place(0f, 1f, 144, -10, 420, 56);
            h.sub = UIKit.Label(h.root, "", 34, UIKit.Dim, false, TextAnchor.MiddleLeft, "Sub");
            h.sub.rectTransform.Place(0f, 0f, 144, 14, 420, 46);

            h.hp = UIKit.Bar(h.root, player ? UIKit.GoodCol : UIKit.BadCol, "Hp");
            h.hp.rectTransform().Place(1f, 0.5f, player ? -24 : -144, -6, 400, 58);
            h.hpText = UIKit.Label(h.hp.transform, "", 34, UIKit.TextCol, true);
            h.hpText.rectTransform.Fill();
            var hpIcon = UIKit.Pix(h.hp.transform, "icon_heart", 1f, "Icon");
            hpIcon.rectTransform.Place(0f, 0.5f, -28, 0, 60, 50);

            h.ready = UIKit.Label(h.root, "READY", 36, UIKit.GoodCol, true, TextAnchor.MiddleRight, "Ready");
            h.ready.rectTransform.Place(1f, 1f, player ? -24 : -144, -2, 300, 46);
            h.ready.gameObject.SetActive(false);
            return h;
        }

        void BuildCardPanel()
        {
            cardPanel = UIKit.Rect("CardPanel", root);
            var frame = UIKit.Frame(cardPanel, "panel", "Frame");
            frame.rectTransform.Stretch(-8, -30, -8, 0);

            // Энерги
            var energyBox = UIKit.Rect("Energy", cardPanel).Place(0f, 1f, 22, -14, 360, 110);
            energyOrb = UIKit.Img(energyBox, ArtCache.Circle, UIKit.Gold, "Orb");
            energyOrb.rectTransform.Place(0f, 0.5f, 0, 0, 104, 104);
            var ring = UIKit.Img(energyOrb.transform, ArtCache.Ring, UIKit.Ink, "Ring");
            ring.rectTransform.Fill();
            var bolt = UIKit.Pix(energyOrb.transform, "ui_energy", 1.5f, "Bolt");
            bolt.color = new Color(1, 1, 1, 0.35f);
            energyText = UIKit.Label(energyOrb.transform, "3", 74, UIKit.Ink, true, TextAnchor.MiddleCenter, "Value", false);
            energyText.rectTransform.Stretch(0, 4, 0, 0);
            var elabel = UIKit.Label(energyBox, "ENERGY", 40, UIKit.Gold, true, TextAnchor.LowerLeft, "Label");
            elabel.rectTransform.Place(0f, 0.5f, 120, 26, 240, 50);
            incomeText = UIKit.Label(energyBox, "+3 next turn", 32, UIKit.Dim, false, TextAnchor.UpperLeft, "Income");
            incomeText.rectTransform.Place(0f, 0.5f, 120, -24, 260, 44);

            // Хугацааны мөр
            timerBar = UIKit.Bar(cardPanel, UIKit.Gold, "TimerBar");
            timerBar.rectTransform().Place(1f, 1f, -24, -46, 520, 46);

            // Картууд
            cardsRow = UIKit.Rect("Cards", cardPanel);
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var slot = UIKit.Rect("Slot" + i, cardsRow).At(0.5f, 0.5f, (i - 1) * (CardW + 26), 0, CardW, CardH);
                offerSlots[i] = slot;
                var cw = new CardWidget(slot, CardW, CardH, false, () => OnCardTap(idx));
                cw.root.At(0.5f, 0.5f, 0, 0, CardW, CardH);
                offerCards[i] = cw;
            }

            // Товчнууд
            buttonsRow = UIKit.Rect("Buttons", cardPanel);
            refreshBtn = UIKit.Btn(buttonsRow, null, "btn_blue", 0, OnRefresh, "Refresh");
            refreshBtn.rectTransform().Place(0f, 0.5f, 24, 0, 230, 124);
            var ri = UIKit.Pix(refreshBtn.transform, "ui_refresh", 1.4f, "Icon");
            ri.rectTransform.anchoredPosition = new Vector2(-38, 4);
            refreshCost = UIKit.Label(refreshBtn.transform, "1", 56, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Cost");
            refreshCost.rectTransform.Place(0.5f, 0.5f, 20, 4, 90, 80);
            var rbolt = UIKit.Pix(refreshBtn.transform, "ui_energy", 1.1f, "Bolt");
            rbolt.rectTransform.anchoredPosition = new Vector2(76, 4);

            getAllBtn = UIKit.Btn(buttonsRow, null, "btn_purple", 0, OnGetAll, "GetAll");
            getAllBtn.rectTransform().Place(0.5f, 0.5f, -90, 0, 310, 124);
            var gi = UIKit.Pix(getAllBtn.transform, "ui_ad", 1.2f, "Icon");
            gi.rectTransform.anchoredPosition = new Vector2(-104, 4);
            var gt = UIKit.Label(getAllBtn.transform, "GET ALL", 50, UIKit.TextCol, true, TextAnchor.MiddleCenter, "Text");
            gt.rectTransform.Stretch(70, 10, 10, 0);
            getAllBtn.label = gt;

            fightBtn = UIKit.Btn(buttonsRow, "FIGHT!", "btn_green", 66, OnFight, "Fight");
            fightBtn.rectTransform().Place(1f, 0.5f, -24, 0, 330, 124);
            fightBtn.sound = "buy";

            // Тулааны үед картын самбарыг хучна: долгионы мөр + 2 тоглогчийн сонгосон картууд
            fightCover = UIKit.Rect("FightCover", cardPanel);
            var dim = UIKit.Block(fightCover, new Color(0.07f, 0.05f, 0.14f, 0.94f), "Dim", true);
            dim.rectTransform.Fill();

            waveStrip = UIKit.Rect("WaveStrip", fightCover);
            var stripBg = UIKit.Frame(waveStrip, "chip", "Bg");
            stripBg.rectTransform.Fill();
            var swords = UIKit.Pix(waveStrip, "ui_swords", 1f, "Swords");
            swords.rectTransform.At(0f, 0.5f, 62, 2, swords.rectTransform.sizeDelta.x, swords.rectTransform.sizeDelta.y);
            swords.gameObject.AddComponent<Pulse>().amount = 0.06f;
            fightCoverText = UIKit.Label(waveStrip, "WAVE 1", 58, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Wave");
            fightCoverText.rectTransform.Place(0f, 0.5f, 116, 2, 330, 80);
            waveBar = UIKit.Bar(waveStrip, UIKit.FoeCol, "WaveBar");
            waveBar.rectTransform().Place(1f, 0.5f, -22, 0, 500, 52);
            fightCoverSub = UIKit.Label(waveBar.transform, "", 30, UIKit.TextCol, true, TextAnchor.MiddleCenter, "Foes");
            fightCoverSub.rectTransform.Stretch(8, 4, 8, 0);

            myPicks = MakePickBoard(fightCover, true);
            rivalPicks = MakePickBoard(fightCover, false);
            fightCover.gameObject.SetActive(false);
        }

        PickBoard MakePickBoard(Transform parent, bool mine)
        {
            var b = new PickBoard();
            b.root = UIKit.Rect(mine ? "MyPicks" : "RivalPicks", parent);
            var bg = UIKit.Frame(b.root, "inset", "Bg");
            bg.rectTransform.Fill();
            var line = UIKit.Block(b.root, mine ? UIKit.AllyCol : UIKit.FoeCol, "Line");
            line.rectTransform.TopStrip(5, 0);
            line.rectTransform.offsetMin = new Vector2(20, line.rectTransform.offsetMin.y);
            line.rectTransform.offsetMax = new Vector2(-20, line.rectTransform.offsetMax.y);

            b.portrait = UIKit.Img(b.root, null, Color.white, "Hero");
            b.portrait.preserveAspect = true;
            b.title = UIKit.Label(b.root, "", 40, mine ? UIKit.AllyCol : UIKit.FoeCol, true, TextAnchor.MiddleLeft, "Name");
            b.count = UIKit.Label(b.root, "", 30, UIKit.Dim, false, TextAnchor.MiddleRight, "Count");

            for (int i = 0; i < PickCols * PickRows; i++)
            {
                var s = new PickSlot();
                s.rim = UIKit.Block(b.root, Color.white, "Slot" + i);
                s.root = s.rim.rectTransform;
                s.fill = UIKit.Block(s.root, Color.white, "Fill");
                s.fill.rectTransform.Stretch(5, 5, 5, 5);
                s.band = UIKit.Block(s.root, Color.white, "Kind");
                s.band.rectTransform.TopStrip(5, 5);
                s.band.rectTransform.offsetMin = new Vector2(5, s.band.rectTransform.offsetMin.y);
                s.band.rectTransform.offsetMax = new Vector2(-5, s.band.rectTransform.offsetMax.y);
                s.icon = UIKit.Img(s.root, null, Color.white, "Icon");
                s.icon.preserveAspect = true;
                s.stack = UIKit.Outlined(s.root, "", 34, UIKit.Gold, "Stack");
                s.stack.alignment = TextAnchor.LowerRight;
                s.stack.rectTransform.Stretch(0, -2, 2, 0);
                s.more = UIKit.Outlined(s.root, "", 40, UIKit.Gold, "More");
                s.more.rectTransform.Fill();
                SetPickSlot(s, null, 0);
                b.slots.Add(s);
            }
            return b;
        }

        static readonly Color SlotEmptyRim = new Color(0.24f, 0.2f, 0.36f, 1f);
        static readonly Color SlotEmptyFill = new Color(0.08f, 0.06f, 0.15f, 1f);
        static readonly Color SlotFill = new Color(0.14f, 0.11f, 0.24f, 1f);

        void SetPickSlot(PickSlot s, CardDef c, int count)
        {
            s.card = c;
            s.count = count;
            bool on = c != null;
            s.icon.gameObject.SetActive(on);
            s.band.gameObject.SetActive(on);
            s.stack.gameObject.SetActive(on && count > 1);
            s.more.gameObject.SetActive(false);
            if (!on)
            {
                s.rim.color = SlotEmptyRim;
                s.fill.color = SlotEmptyFill;
                return;
            }
            s.rim.color = ArtCache.ToColor(Palette.Rarity(c.rarity));
            s.fill.color = SlotFill;
            s.band.color = ArtCache.ToColor(Palette.Kind(c.kind));
            s.icon.sprite = ArtCache.Get(c.art);
            s.stack.text = "x" + count;
            FitPickIcon(s);
        }

        void SetPickMore(PickSlot s, int extra)
        {
            s.card = null;
            s.count = -extra;
            s.icon.gameObject.SetActive(false);
            s.band.gameObject.SetActive(false);
            s.stack.gameObject.SetActive(false);
            s.more.gameObject.SetActive(true);
            s.more.text = "+" + extra;
            s.rim.color = UIKit.Gold;
            s.fill.color = SlotFill;
        }

        /// <summary>Картын зургийг слотод бүхэл тоогоор томруулж багтаана (пиксел тод).</summary>
        static void FitPickIcon(PickSlot s)
        {
            if (s.card == null) return;
            var size = s.root.sizeDelta;
            var px = ArtCache.PixelSize(s.card.art);
            if (px.x <= 0 || px.y <= 0 || size.x <= 0) return;
            float k = Mathf.Floor(Mathf.Min((size.x - 18f) / px.x, (size.y - 34f) / px.y));
            k = Mathf.Clamp(k, 2f, 6f);
            s.icon.rectTransform.At(0.5f, 0.5f, 0, 4, px.x * k, px.y * k);
        }

        void ResetPickBoards()
        {
            ResetPickBoard(myPicks, Save.P.playerName, match.Player.hero.unit.sprite);
            ResetPickBoard(rivalPicks, opp.name, match.Rival.hero.unit.sprite);
        }

        void ResetPickBoard(PickBoard b, string name, string heroSprite)
        {
            b.title.text = name;
            b.portrait.sprite = ArtCache.Get(heroSprite);
            b.count.text = "";
            b.shown = 0;
            foreach (var s in b.slots) SetPickSlot(s, null, 0);
        }

        /// <summary>Шинээр сонгосон картуудыг слотод нэмнэ. Ижил картыг нэг слотод "x2" гэж бүлэглэнэ.</summary>
        void UpdatePickBoard(PickBoard b, Side side)
        {
            int total = side.picks.Count;
            if (b.shown == total) return;
            b.shown = total;

            pickOrder.Clear();
            foreach (var c in side.picks)
                if (!pickOrder.Contains(c)) pickOrder.Add(c);

            int n = b.slots.Count;
            bool overflow = pickOrder.Count > n;
            for (int i = 0; i < n; i++)
            {
                var slot = b.slots[i];
                if (overflow && i == n - 1)
                {
                    int extra = pickOrder.Count - (n - 1);
                    if (slot.count != -extra) SetPickMore(slot, extra);
                    continue;
                }
                var c = i < pickOrder.Count ? pickOrder[i] : null;
                int count = c != null ? side.Stacks(c.id) : 0;
                if (slot.card == c && slot.count == count) continue;
                SetPickSlot(slot, c, count);
                if (c != null) StartCo(UIKit.Pop(slot.root, 0.4f, 0.3f));
            }
            b.count.text = total + (total == 1 ? " card" : " cards");
        }

        /// <summary>Тулааны үеийн самбарын байрлал: долгионы мөр (дээр), 2 тоглогчийн карт (доор, зүүн = та).</summary>
        void LayoutFightCover(float w, float h)
        {
            const float side = 24f, gap = 16f, stripH = 92f, pad = 20f, headH = 70f, slotGap = 8f;
            float colW = Mathf.Floor((w - side * 2f - gap) / 2f);
            float slotW = Mathf.Min(96f, Mathf.Floor((colW - pad * 2f - (PickCols - 1) * slotGap) / PickCols));
            float slotH = Mathf.Round(slotW * 1.25f);
            float boardH = pad + headH + PickRows * slotH + (PickRows - 1) * slotGap + pad;
            float blockH = stripH + 14f + boardH;
            float top = Mathf.Max(14f, Mathf.Floor((h - blockH) * 0.5f));

            waveStrip.anchorMin = new Vector2(0f, 1f);
            waveStrip.anchorMax = new Vector2(1f, 1f);
            waveStrip.pivot = new Vector2(0.5f, 1f);
            waveStrip.offsetMin = new Vector2(side, -top - stripH);
            waveStrip.offsetMax = new Vector2(-side, -top);

            float y = top + stripH + 14f;
            PlacePickBoard(myPicks, side, y, colW, boardH, slotW, slotH, slotGap, pad, headH);
            PlacePickBoard(rivalPicks, side + colW + gap, y, colW, boardH, slotW, slotH, slotGap, pad, headH);
        }

        static void PlacePickBoard(PickBoard b, float x, float y, float w, float h, float sw, float sh, float g, float pad, float headH)
        {
            b.root.Place(0f, 1f, x, -y, w, h);
            b.portrait.rectTransform.Place(0f, 1f, pad, -pad + 4, 64, 64);
            b.title.rectTransform.Place(0f, 1f, pad + 74, -pad + 2, w - pad * 2f - 74f - 130f, 60);
            b.count.rectTransform.Place(1f, 1f, -pad, -pad + 2, 130, 60);
            float gridW = PickCols * sw + (PickCols - 1) * g;
            float x0 = Mathf.Floor((w - gridW) * 0.5f);
            for (int i = 0; i < b.slots.Count; i++)
            {
                int col = i % PickCols, row = i / PickCols;
                var s = b.slots[i];
                s.root.Place(0f, 1f, x0 + col * (sw + g), -(pad + headH + row * (sh + g)), sw, sh);
                FitPickIcon(s);
            }
        }

        // ================= Байршил (дэлгэцийн өндрөөс хамаарна) =================

        void Layout()
        {
            Canvas.ForceUpdateCanvases();
            var size = root.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            lastSize = size;
            float H = size.y;
            float fixedH = RivalHudH + MidH + PlayerHudH;
            float laneH = Mathf.Clamp((H - fixedH - CardPanelMin) / 2f, 330f, 540f);
            float panelH = Mathf.Max(CardPanelMin - 60f, H - fixedH - laneH * 2f);

            float y = RivalHudH;
            var rl = rivalLane != null ? rivalLane.root : null;
            if (rl != null) { rl.TopStrip(laneH, y); rivalLane.Layout(size.x, laneH); }
            y += laneH;
            mid.TopStrip(MidH, y);
            y += MidH;
            if (playerLane != null) { playerLane.root.TopStrip(laneH, y); playerLane.Layout(size.x, laneH); }
            y += laneH;
            playerHud.root.TopStrip(PlayerHudH, y);
            cardPanel.BottomStrip(panelH, 0);

            // Картын самбарын дотор: энерги (дээр) / картууд / товчнууд (доор)
            float inner = panelH - 130f - 150f;
            float cardScale = Mathf.Clamp(inner / (CardH + 20f), 0.72f, 1.12f);
            // Урт утсанд 3 карт дэлгэцийн өргөнөөс хэтрэхгүй
            cardScale = Mathf.Min(cardScale, (size.x - 24f) / (CardW * 3f + 52f));
            cardsRow.anchorMin = new Vector2(0, 0);
            cardsRow.anchorMax = new Vector2(1, 1);
            cardsRow.offsetMin = new Vector2(0, 150f);
            cardsRow.offsetMax = new Vector2(0, -130f);
            cardsRow.localScale = Vector3.one;
            for (int i = 0; i < 3; i++)
            {
                offerSlots[i].localScale = new Vector3(cardScale, cardScale, 1f);
                offerSlots[i].anchoredPosition = new Vector2((i - 1) * (CardW + 26) * cardScale, 0);
            }
            buttonsRow.BottomStrip(150, 10);
            fightCover.Stretch(0, 0, 0, 0);
            LayoutFightCover(size.x, panelH);

            tickerRoot.Place(0f, 1f, 20, -(RivalHudH + laneH - 20f), 600, 300);
            tickerRoot.pivot = new Vector2(0f, 0f);
        }

        // ================= HUD шинэчлэх =================

        void SetupHuds()
        {
            var p = Save.P;
            playerHud.portrait.sprite = ArtCache.Get(match.Player.hero.unit.sprite);
            playerHud.name.text = p.playerName;
            playerHud.sub.text = "Lv " + match.Player.heroLevel + " " + match.Player.hero.name + "   " + p.trophies + " Trophies";
            rivalHud.portrait.sprite = ArtCache.Get(match.Rival.hero.unit.sprite);
            rivalHud.name.text = opp.name;
            rivalHud.sub.text = "Lv " + opp.heroLevel + " " + opp.hero.name + "   " + opp.trophies + " Trophies";
            shownEnergy = -1;
            ResetPickBoards();
        }

        void UpdateHud(float dt)
        {
            if (match == null) return;
            UpdateHeroHud(playerHud, match.Player);
            UpdateHeroHud(rivalHud, match.Rival);
            bool pick = match.phase == Phase.Pick;
            rivalHud.ready.gameObject.SetActive(pick && match.Rival.ready);
            playerHud.ready.gameObject.SetActive(false);

            waveText.text = (match.turn % 5 == 0 ? "BOSS " : "WAVE ") + match.turn;
            waveText.color = match.turn % 5 == 0 ? UIKit.BadCol : UIKit.TextCol;
            if (pick)
            {
                phaseText.text = "PICK";
                int secs = Mathf.CeilToInt(Mathf.Max(0f, match.timer));
                timerText.text = secs.ToString();
                timerText.color = secs <= 5 ? UIKit.BadCol : UIKit.TextCol;
                float total = match.turn == 1 ? Balance.FirstPickDuration : Balance.PickDuration;
                timerBar.Set(match.timer / total, true);
                timerBar.SetColor(secs <= 5 ? UIKit.BadCol : UIKit.Gold);
            }
            else
            {
                phaseText.text = "FIGHT";
                float left = Mathf.Max(0f, Balance.FightDuration - match.fightTime);
                timerText.text = Mathf.CeilToInt(left).ToString();
                timerText.color = UIKit.TextCol;
                waveBar.Set(match.fightTime / Balance.FightDuration, true);
                int monsters = match.Player.lane.MonsterCount + match.Player.lane.PendingCount;
                fightCoverSub.text = monsters > 0 ? monsters + " foes left in your lane" : "Lane clear! Next cards soon...";
            }

            // Энерги
            var me = match.Player;
            if (shownEnergy != me.energy)
            {
                if (shownEnergy >= 0) StartCo(UIKit.Punch(energyOrb.transform, 0.18f, 0.25f));
                shownEnergy = me.energy;
                energyText.text = me.energy.ToString();
            }
            int nextIncome = Balance.Income(match.turn + 1) + me.mods.goldMine + me.siphon - me.drain;
            incomeText.text = "+" + Mathf.Max(0, nextIncome) + " next turn";

            // Тулааны үед: долгионы нэр + 2 тоглогчийн сонгосон картууд (карт сонгох хэсгийг нууна)
            fightCover.gameObject.SetActive(!pick);
            cardsRow.gameObject.SetActive(pick);
            buttonsRow.gameObject.SetActive(pick);
            timerBar.gameObject.SetActive(pick);
            energyOrb.transform.parent.gameObject.SetActive(pick);
            if (!pick)
            {
                fightCoverText.text = waveText.text;
                fightCoverText.color = waveText.color;
                UpdatePickBoard(myPicks, me);
                UpdatePickBoard(rivalPicks, match.Rival);
            }

            // Картууд
            for (int i = 0; i < offerCards.Length; i++)
            {
                var cw = offerCards[i];
                if (i >= me.offer.Count) { cw.root.gameObject.SetActive(false); continue; }
                var o = me.offer[i];
                if (cw.card != o.card) cw.Set(o.card, o.price);
                cw.SetPrice(o.price);
                cw.SetSold(o.sold);
                // Ээлжид 1 карт: авсны дараа үлдсэнийг түгжинэ (GET ALL-оор авч болно)
                cw.SetLocked(!o.sold && !me.CanPick, "1 per wave");
                bool can = !o.sold && me.CanPick && me.energy >= o.price;
                cw.SetAffordable(can || o.sold);
                cw.SetGlow(can && pick && !me.ready ? 0.25f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f + i) : 0f);
            }

            bool anyLeft = false;
            foreach (var o in me.offer) if (!o.sold) anyLeft = true;
            refreshBtn.Interactable = pick && !me.ready && me.CanPick && me.energy >= Balance.RefreshCost;
            // GET ALL: 3 долгион тутамд. Бусад үед хэдэн долгионы дараа гарахыг харуулна
            bool getAllTurn = Balance.GetAllTurn(match.turn);
            getAllBtn.gameObject.SetActive(!(getAllTurn && me.usedGetAll));
            getAllBtn.Interactable = pick && !me.ready && me.CanGetAll && anyLeft && !adBusy;
            int wait = Balance.GetAllEvery - match.turn % Balance.GetAllEvery;
            getAllBtn.SetLabel(getAllTurn ? "GET ALL" : (wait == 1 ? "NEXT WAVE" : "IN " + wait + " WAVES"));
            fightBtn.Interactable = pick && !me.ready;
            fightBtn.SetLabel(me.ready ? "WAIT..." : "FIGHT!");
        }

        void UpdateHeroHud(HeroHud h, Side s)
        {
            var hero = s.lane.hero;
            if (hero == null) return;
            h.hp.Set(hero.HpFrac);
            h.hpText.text = Mathf.CeilToInt(Mathf.Max(0f, hero.hp)) + " / " + Mathf.CeilToInt(hero.maxHp);
            h.hp.SetColor(hero.HpFrac < 0.3f ? (Mathf.Sin(Time.unscaledTime * 10f) > 0 ? UIKit.BadCol : new Color(1f, 0.75f, 0.3f)) : (s.index == 0 ? UIKit.GoodCol : UIKit.BadCol));
        }

        // ================= Баннер, тикер =================

        void Banner(string text, Color color, int size = 150, float hold = 0.9f, string sub = null)
        {
            StartCo(BannerRoutine(text, color, size, hold, sub));
        }

        IEnumerator BannerRoutine(string text, Color color, int size, float hold, string sub)
        {
            var rt = UIKit.Rect("Banner", bannerLayer).At(0.5f, 0.5f, 0, 120, 1080, 300);
            var band = UIKit.Block(rt, new Color(0.05f, 0.03f, 0.1f, 0.7f), "Band");
            band.rectTransform.Stretch(-100, 40, -100, 40);
            var t = UIKit.Outlined(rt, text, size, color, "Text");
            t.rectTransform.Fill();
            t.rectTransform.offsetMin = new Vector2(0, sub != null ? 40 : 0);
            if (sub != null)
            {
                var s = UIKit.Label(rt, sub, 44, UIKit.TextCol, true);
                s.rectTransform.BottomStrip(70, 30);
            }
            var g = rt.Group();
            g.blocksRaycasts = false;
            yield return UIKit.Pop(rt, 1.6f, 0.25f);
            float t0 = 0f;
            while (t0 < hold) { t0 += Time.unscaledDeltaTime; yield return null; }
            yield return UIKit.Fade(g, 0f, 0.25f);
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        readonly List<RectTransform> tickers = new List<RectTransform>();

        /// <summary>Өрсөлдөгчийн сонгосон картыг зүүн доод буланд харуулна.</summary>
        void Ticker(CardDef c)
        {
            var box = UIKit.Frame(tickerRoot, "chip", "Pick");
            var rt = box.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(470, 76);
            var icon = UIKit.Img(rt, ArtCache.Get(c.art), Color.white, "Icon");
            icon.preserveAspect = true;
            icon.rectTransform.Place(0f, 0.5f, 10, 0, 64, 60);
            var t = UIKit.Label(rt, c.name, 38, c.kind == CardKind.Sabotage ? UIKit.FoeCol : UIKit.TextCol, true, TextAnchor.MiddleLeft, "Name");
            t.rectTransform.Stretch(84, 4, 10, 0);
            tickers.Insert(0, rt);
            for (int i = 0; i < tickers.Count; i++) if (tickers[i] != null) tickers[i].anchoredPosition = new Vector2(0, i * 84);
            while (tickers.Count > 3)
            {
                var old = tickers[tickers.Count - 1];
                tickers.RemoveAt(tickers.Count - 1);
                if (old != null) Object.Destroy(old.gameObject);
            }
            StartCo(UIKit.Pop(rt, 0.5f, 0.25f));
            StartCo(TickerLife(rt));
        }

        IEnumerator TickerLife(RectTransform rt)
        {
            float t = 0f;
            while (t < 3.2f) { t += Time.unscaledDeltaTime; yield return null; }
            if (rt == null) yield break;
            yield return UIKit.Fade(rt.Group(), 0f, 0.4f);
            tickers.Remove(rt);
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        void ClearTickers()
        {
            foreach (var t in tickers) if (t != null) Object.Destroy(t.gameObject);
            tickers.Clear();
        }

        /// <summary>Сонгосон картын дүрсийг нэг цэгээс нөгөө рүү нисгэнэ.</summary>
        IEnumerator FlyIcon(string sprite, Vector3 fromWorld, Vector3 toWorld, System.Action arrive)
        {
            var img = UIKit.Img(overlay, ArtCache.Get(sprite), Color.white, "Fly");
            img.preserveAspect = true;
            var px = ArtCache.PixelSize(sprite);
            img.rectTransform.sizeDelta = new Vector2(px.x, px.y) * 7f;
            var trail = UIKit.Img(img.transform, ArtCache.Glow, new Color(1f, 0.9f, 0.5f, 0.7f), "Glow");
            trail.rectTransform.Stretch(-40, -40, -40, -40);
            trail.transform.SetAsFirstSibling();
            float t = 0f, dur = 0.45f;
            Vector3 a = fromWorld, b = toWorld;
            Vector3 ctrl = (a + b) * 0.5f + (Vector3)(Vector2.up * 0.25f * Vector3.Distance(a, b));
            while (t < dur && img != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                k = k * k * (3f - 2f * k);
                var p = Vector3.Lerp(Vector3.Lerp(a, ctrl, k), Vector3.Lerp(ctrl, b, k), k);
                img.transform.position = p;
                float s = Mathf.Lerp(1f, 0.55f, k);
                img.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
            arrive?.Invoke();
        }

        // ================= Сургалт =================

        RectTransform tipRoot;

        bool TipActive => tipRoot != null;

        /// <summary>Заавар харуулна (дарахад алга болно). target = заах UI (null бол заахгүй).</summary>
        void ShowTip(string text, RectTransform target, bool above)
        {
            CloseTip();
            tipRoot = UIKit.Rect("Tip", overlay).Fill();
            var blocker = UIKit.Blocker(tipRoot, new Color(0.03f, 0.02f, 0.08f, 0.45f));
            var hit = blocker.gameObject.AddComponent<UiButton>();
            hit.pressScale = 1f;
            hit.onClick = CloseTip;

            Vector3 world = target != null ? target.TransformPoint(target.rect.center) : root.TransformPoint(Vector3.zero);
            Vector2 local = tipRoot.InverseTransformPoint(world);
            var box = UIKit.Frame(tipRoot, "panel", "Box");
            float bh = 300f;
            float by = target == null ? 0f : above ? local.y + target.rect.height * 0.5f + 60f + bh * 0.5f : local.y - target.rect.height * 0.5f - 60f - bh * 0.5f;
            box.rectTransform.At(0.5f, 0.5f, 0, by, 940, bh);
            var t = UIKit.Para(box.transform, text, 46, UIKit.TextCol, TextAnchor.MiddleCenter);
            t.rectTransform.Stretch(40, 60, 40, 30);
            var tap = UIKit.Label(box.transform, "Tap to continue", 32, UIKit.Dim);
            tap.rectTransform.BottomStrip(50, 18);
            if (target != null)
            {
                var arrow = UIKit.Pix(tipRoot, "ui_back", 1.6f, "Arrow");
                arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                arrow.rectTransform.anchoredPosition = new Vector2(local.x, above ? local.y + target.rect.height * 0.5f + 40f : local.y - target.rect.height * 0.5f - 40f);
                arrow.rectTransform.localEulerAngles = new Vector3(0, 0, above ? 90f : -90f);
                var bob = arrow.gameObject.AddComponent<Bob>();
                bob.amplitude = 12f;
                bob.speed = 6f;
                bob.Rebase();
            }
            StartCo(UIKit.Pop(box.transform, 0.7f, 0.3f));
            AudioManager.Instance?.Play("whoosh", 0.6f);
        }

        void CloseTip()
        {
            if (tipRoot != null) Object.Destroy(tipRoot.gameObject);
            tipRoot = null;
        }
    }
}
