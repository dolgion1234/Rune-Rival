using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>Картууд: эхлэх картын слотууд (Armory) + 30 картын цуглуулга (лигээр нээгдэнэ).</summary>
    public sealed class CardsScreen : UiScreen
    {
        public override int NavIndex => 3;
        const int MaxSlots = 3;

        sealed class Slot
        {
            public RectTransform holder;
            public CardWidget card;
            public UiButton empty;
            public Image plus, lockImg;
            public Text lockText;
        }

        readonly Slot[] slots = new Slot[MaxSlots];
        readonly List<CardWidget> collection = new List<CardWidget>();
        readonly Text[] tierHeaders = new Text[4];
        Text countText;
        RectTransform content;

        protected override void Build()
        {
            var c = Content();
            content = UIKit.Scroll(c, out _, "Scroll");
            ((RectTransform)content.parent).Fill();
            BuildStarters();
            float y = BuildCollection(600);
            content.sizeDelta = new Vector2(0, y + 30);
        }

        // ---------------- Эхлэх картууд ----------------

        void BuildStarters()
        {
            var panel = UIKit.Frame(content, "panel", "Starters").rectTransform;
            panel.TopStrip(550, 20);
            panel.offsetMin = new Vector2(30, panel.offsetMin.y);
            panel.offsetMax = new Vector2(-30, panel.offsetMax.y);
            var title = UIKit.Label(panel, "STARTING CARDS", 62, UIKit.Gold, true, TextAnchor.MiddleLeft, "Title");
            title.rectTransform.Place(0f, 1f, 40, -26, 800, 72);
            var sub = UIKit.Label(panel, "Played for free when every battle begins", 38, UIKit.Dim, false, TextAnchor.MiddleLeft, "Sub");
            sub.rectTransform.Place(0f, 1f, 42, -100, 900, 50);

            for (int i = 0; i < MaxSlots; i++)
            {
                int idx = i;
                var s = new Slot();
                s.holder = UIKit.Rect("Slot" + i, panel).At(0.5f, 0f, (i - 1) * 320, 200, 240, 330);
                s.empty = UIKit.Btn(s.holder, null, "inset", 0, () => OnSlot(idx), "Empty");
                s.empty.rectTransform().Fill();
                s.plus = UIKit.Pix(s.empty.transform, "ui_plus", 2.4f, "Plus");
                s.plus.gameObject.AddComponent<Pulse>().amount = 0.08f;
                s.lockImg = UIKit.Pix(s.empty.transform, "ui_lock", 2f, "Lock");
                s.lockImg.rectTransform.anchoredPosition = new Vector2(0, 40);
                s.lockText = UIKit.Label(s.empty.transform, "", 36, UIKit.Dim, true, TextAnchor.MiddleCenter, "LockText");
                s.lockText.rectTransform.At(0.5f, 0.5f, 0, -60, 230, 90);
                s.card = new CardWidget(s.holder, 240, 330, true, () => OnSlot(idx));
                s.card.root.anchoredPosition = Vector2.zero;
                slots[i] = s;
            }
        }

        // ---------------- Цуглуулга ----------------

        float BuildCollection(float y)
        {
            // Хамгийн урт текстээр бүтээнэ (арын хайрцаг багтах)
            countText = UIKit.Header(content, "COLLECTION 30/30", y);
            y += 100;
            const int cols = 4;
            const float cw = 236, ch = 320, gapX = 16, gapY = 26;
            for (int tier = 0; tier <= 3; tier++)
            {
                var list = new List<CardDef>();
                foreach (var c in Cards.All) if (c.unlockTier == tier) list.Add(c);

                var head = UIKit.Label(content, "", 46, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Tier" + tier);
                head.rectTransform.TopStrip(60, y);
                head.rectTransform.offsetMin = new Vector2(52, head.rectTransform.offsetMin.y);
                tierHeaders[tier] = head;
                y += 74;

                for (int i = 0; i < list.Count; i++)
                {
                    int r = i / cols, col = i % cols;
                    var card = list[i];
                    var w = new CardWidget(content, cw, ch, true, () => ShowInfo(card));
                    w.root.anchorMin = w.root.anchorMax = new Vector2(0.5f, 1f);
                    w.root.pivot = new Vector2(0.5f, 1f);
                    w.root.anchoredPosition = new Vector2((col - (cols - 1) / 2f) * (cw + gapX), -(y + r * (ch + gapY)));
                    w.Set(card, card.cost);
                    if (card.starter)
                    {
                        var star = UIKit.Pix(w.root, "ui_star", 0.9f, "StarterMark");
                        star.rectTransform.Place(1f, 1f, 6, 6, star.rectTransform.sizeDelta.x, star.rectTransform.sizeDelta.y);
                    }
                    collection.Add(w);
                }
                y += ((list.Count + cols - 1) / cols) * (ch + gapY) + 24;
            }

            var legend = UIKit.Rect("Legend", content).TopStrip(60, y);
            var ls = UIKit.Pix(legend, "ui_star", 0.9f, "Star");
            ls.rectTransform.At(0.5f, 0.5f, -330, 0, ls.rectTransform.sizeDelta.x, ls.rectTransform.sizeDelta.y);
            var lt = UIKit.Label(legend, "= can be a starting card", 38, UIKit.Dim, false, TextAnchor.MiddleLeft, "Text");
            lt.rectTransform.At(0.5f, 0.5f, 40, 0, 620, 60);
            return y + 70;
        }

        // ---------------- Шинэчлэх ----------------

        public override void OnShow()
        {
            Refresh();
            Save.Changed += Refresh;
        }

        public override void OnHide() => Save.Changed -= Refresh;

        static int CurrentTier => League.TierOf(Save.P.trophies);
        static int CardTierNow => League.CardTier(CurrentTier);

        static int SlotTier(int slot)
        {
            for (int t = 0; t < League.Count; t++)
                if (League.StarterSlots[t] > slot) return t;
            return League.Count - 1;
        }

        static CardDef StarterAt(int slot)
        {
            var list = Save.P.starters;
            if (slot >= list.Count) return null;
            var c = Cards.Get(list[slot]);
            return c != null && c.starter && c.unlockTier <= CardTierNow ? c : null;
        }

        void Refresh()
        {
            int tier = CurrentTier, ct = CardTierNow;
            countText.text = "COLLECTION " + Cards.CountUnlockedAt(ct) + "/" + Cards.All.Count;

            for (int t = 0; t < tierHeaders.Length; t++)
            {
                bool open = t <= ct;
                tierHeaders[t].text = League.Names[t].ToUpper() + " LEAGUE" + (open ? "" : "  - LOCKED");
                tierHeaders[t].color = open ? ArtCache.ToColor(Palette.Tier(t)) : UIKit.Dim;
            }
            foreach (var w in collection)
            {
                bool locked = w.card.unlockTier > ct;
                w.SetLocked(locked, locked ? League.Names[w.card.unlockTier] : null);
            }

            int open2 = League.StarterSlots[tier];
            for (int i = 0; i < MaxSlots; i++)
            {
                var s = slots[i];
                bool unlocked = i < open2;
                var card = unlocked ? StarterAt(i) : null;
                s.card.Set(card, card != null ? card.cost : 0);
                s.empty.gameObject.SetActive(card == null);
                s.plus.gameObject.SetActive(unlocked);
                s.lockImg.gameObject.SetActive(!unlocked);
                s.lockText.gameObject.SetActive(!unlocked);
                if (!unlocked) s.lockText.text = League.Names[SlotTier(i)] + "\nLeague";
            }
        }

        // ---------------- Слот ----------------

        void OnSlot(int slot)
        {
            if (slot >= League.StarterSlots[CurrentTier])
            {
                ui.Toast("Unlocks in " + League.Names[SlotTier(slot)] + " League");
                return;
            }
            OpenPicker(slot);
        }

        static int SlotOf(string id)
        {
            var list = Save.P.starters;
            for (int i = 0; i < list.Count; i++) if (list[i] == id) return i;
            return -1;
        }

        static void SetSlot(int slot, string id)
        {
            var list = Save.P.starters;
            while (list.Count <= slot) list.Add("");
            if (!string.IsNullOrEmpty(id))
            {
                int other = list.IndexOf(id);
                if (other >= 0 && other != slot) list[other] = list[slot];   // солилцоно
            }
            list[slot] = id ?? "";
            while (list.Count > 0 && string.IsNullOrEmpty(list[list.Count - 1])) list.RemoveAt(list.Count - 1);
            Save.Commit();
        }

        void OpenPicker(int slot)
        {
            var popup = ui.OpenPopup("Starting Card " + (slot + 1), 1000, 1580);
            var body = popup.body;
            var view = UIKit.Scroll(body, out _, "List");
            ((RectTransform)view.parent).Stretch(-20, 170, -20, 0);
            int ct = CardTierNow;
            var current = StarterAt(slot);

            const int cols = 3;
            const float cw = 276, ch = 370, gap = 22;
            int n = 0;
            foreach (var c in Cards.All)
            {
                if (!c.starter) continue;
                var card = c;
                int r = n / cols, col = n % cols;
                var w = new CardWidget(view, cw, ch, true, () => Pick(popup, slot, card));
                w.root.anchorMin = w.root.anchorMax = new Vector2(0.5f, 1f);
                w.root.pivot = new Vector2(0.5f, 1f);
                w.root.anchoredPosition = new Vector2((col - 1) * (cw + gap), -(24 + r * (ch + gap)));
                w.Set(card, card.cost);
                if (card.unlockTier > ct) w.SetLocked(true, League.Names[card.unlockTier]);
                else if (card == current) w.SetGlow(0.9f);
                else
                {
                    int other = SlotOf(card.id);
                    if (other >= 0 && other < League.StarterSlots[CurrentTier])
                    {
                        var tag = UIKit.Label(w.root, "SLOT " + (other + 1), 34, UIKit.Gold, true, TextAnchor.MiddleCenter, "InSlot");
                        tag.rectTransform.Place(0.5f, 1f, 0, -6, 200, 44);
                    }
                }
                n++;
            }
            view.sizeDelta = new Vector2(0, 40 + ((n + cols - 1) / cols) * (ch + gap));

            var clear = UIKit.Btn(body, "Remove", "btn_red", 54, () =>
            {
                SetSlot(slot, null);
                popup.Close();
            }, "Clear");
            clear.rectTransform().Place(0.5f, 0f, 0, 0, 420, 140);
            clear.gameObject.SetActive(current != null);
            if (current == null)
            {
                var hint = UIKit.Label(body, "Tap a card to place it in this slot", 40, UIKit.Dim, false);
                hint.rectTransform.BottomStrip(120, 10);
            }
        }

        void Pick(Popup popup, int slot, CardDef card)
        {
            if (card.unlockTier > CardTierNow)
            {
                ui.Toast("Unlocks in " + League.Names[card.unlockTier] + " League");
                return;
            }
            SetSlot(slot, card.id);
            popup.Close();
            AudioManager.Instance?.Play("buy");
            Haptics.Light();
            var s = slots[slot];
            if (s.card.root.gameObject.activeSelf) ui.StartCoroutine(UIKit.Pop(s.card.root, 0.6f, 0.35f));
        }

        // ---------------- Картын мэдээлэл ----------------

        void ShowInfo(CardDef c)
        {
            AudioManager.Instance?.Play("click", 0.7f);
            var popup = ui.OpenPopup(null, 900, 1380);
            var body = popup.body;
            var w = new CardWidget(body, 440, 610, false, null);
            w.root.anchorMin = w.root.anchorMax = new Vector2(0.5f, 1f);
            w.root.pivot = new Vector2(0.5f, 1f);
            w.root.anchoredPosition = new Vector2(0, -20);
            w.Set(c, c.cost);
            int ct = CardTierNow;
            bool locked = c.unlockTier > ct;
            if (locked) w.SetLocked(true, League.Names[c.unlockTier]);

            string rarityHex = ColorUtility.ToHtmlStringRGB(ArtCache.ToColor(Palette.Rarity(c.rarity)));
            string kindHex = ColorUtility.ToHtmlStringRGB(ArtCache.ToColor(Palette.Kind(c.kind)));
            var lines = "<color=#" + rarityHex + ">" + c.rarity.ToString().ToUpper() + "</color>   <color=#" + kindHex + ">" +
                        CardWidget.KindName(c.kind) + "</color>\n";
            lines += c.cost == 0 ? "Free to pick" : "Costs " + c.cost + " energy";
            lines += c.maxStacks > 0 ? "  -  stacks up to " + c.maxStacks + "x" : "  -  stacks without limit";
            if (c.starter) lines += "\n<color=#FFD24A>Can be a starting card</color>";
            if (locked) lines += "\n<color=#FF7A7A>Unlocks in " + League.Names[c.unlockTier] + " League</color>";
            var info = UIKit.Para(body, lines, 42, UIKit.TextCol, TextAnchor.UpperCenter, "Info");
            info.rectTransform.Place(0.5f, 1f, 0, -660, 800, 260);

            UiButton btn;
            if (!locked && c.starter && SlotOf(c.id) < 0)
            {
                btn = UIKit.Btn(body, "Set as starter", "btn_green", 54, () =>
                {
                    QuickAssign(c);
                    popup.Close();
                }, "Starter");
            }
            else btn = UIKit.Btn(body, "OK", "btn_blue", 58, () => popup.Close(), "Ok");
            btn.rectTransform().Place(0.5f, 0f, 0, 0, 480, 140);
        }

        void QuickAssign(CardDef c)
        {
            int open = League.StarterSlots[CurrentTier];
            int target = -1;
            for (int i = 0; i < open; i++)
                if (StarterAt(i) == null) { target = i; break; }
            if (target < 0) target = open - 1;
            SetSlot(target, c.id);
            AudioManager.Instance?.Play("buy");
            ui.Toast(c.name + " is now starting card " + (target + 1), UIKit.Gold);
        }
    }
}
