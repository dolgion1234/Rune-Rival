using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>Лиг: одоогийн шат, статистик, Bronze → Mythic шатлал (шагнал, нээгдэх зүйлс).</summary>
    public sealed class LeagueScreen : UiScreen
    {
        public override int NavIndex => 4;

        sealed class Row
        {
            public int tier;
            public Image frame, emblem, status, highlight;
            public Text name, need, unlocks;
            public RectTransform root;
        }

        readonly List<Row> rows = new List<Row>();
        Image emblem;
        Text tierName, trophies, nextText, best;
        UiBar bar;
        Text wins, losses, rate, streak;
        ScrollRect scroll;

        protected override void Build()
        {
            var c = Content();
            var content = UIKit.Scroll(c, out scroll, "Scroll");
            ((RectTransform)content.parent).Fill();

            // ---- Одоогийн шат ----
            var panel = UIKit.Frame(content, "panel", "Current").rectTransform;
            panel.TopStrip(560, 20);
            panel.offsetMin = new Vector2(30, panel.offsetMin.y);
            panel.offsetMax = new Vector2(-30, panel.offsetMax.y);

            var rays = UIKit.Img(panel, ArtCache.Rays, new Color(1f, 0.9f, 0.6f, 0.22f), "Rays");
            rays.rectTransform.At(0f, 1f, 230, -250, 520, 520);
            rays.gameObject.AddComponent<Spin>().speed = 10f;
            emblem = UIKit.Img(panel, ArtCache.Emblem(0), Color.white, "Emblem");
            emblem.rectTransform.At(0f, 1f, 230, -250, 28 * 8f, 32 * 8f);
            var bob = emblem.gameObject.AddComponent<Bob>();
            bob.amplitude = 6f;
            bob.speed = 1.8f;

            tierName = UIKit.Label(panel, "", 92, UIKit.TextCol, true, TextAnchor.MiddleLeft, "Tier");
            tierName.rectTransform.Place(0f, 1f, 440, -60, 560, 100);
            var sub = UIKit.Label(panel, "LEAGUE", 46, UIKit.Dim, true, TextAnchor.MiddleLeft, "Sub");
            sub.rectTransform.Place(0f, 1f, 444, -156, 400, 56);
            var trophyRow = UIKit.Rect("Trophies", panel).Place(0f, 1f, 440, -232, 500, 90);
            trophies = UIKit.IconValue(trophyRow, "ui_trophy", "0", 80, UIKit.Gold, 1.6f, out _);
            best = UIKit.Label(panel, "", 38, UIKit.Dim, false, TextAnchor.MiddleLeft, "Best");
            best.rectTransform.Place(0f, 1f, 444, -330, 520, 50);

            bar = UIKit.Bar(panel, UIKit.Gold, "Bar");
            bar.rectTransform().Place(0.5f, 0f, 0, 110, 900, 56);
            nextText = UIKit.Label(panel, "", 40, UIKit.TextCol, false, TextAnchor.MiddleCenter, "Next");
            nextText.rectTransform.Place(0.5f, 0f, 0, 40, 900, 60);

            // ---- Статистик ----
            var stats = UIKit.Rect("Stats", content).TopStrip(200, 600);
            wins = StatBox(stats, 0, "WINS", UIKit.GoodCol);
            losses = StatBox(stats, 1, "LOSSES", UIKit.BadCol);
            rate = StatBox(stats, 2, "WIN RATE", UIKit.TextCol);
            streak = StatBox(stats, 3, "BEST STREAK", UIKit.Gold);

            // ---- Шатлал ----
            UIKit.Header(content, "ALL LEAGUES", 830);
            float y = 930;
            for (int t = League.Count - 1; t >= 0; t--)
            {
                rows.Add(BuildRow(content, t, y));
                y += 250;
            }
            var foot = UIKit.Para(content, "Win battles to earn trophies. You never drop below a league once you reach it.",
                36, UIKit.Dim, TextAnchor.UpperCenter, "Footer");
            foot.rectTransform.TopStrip(110, y + 10);
            foot.rectTransform.offsetMin = new Vector2(60, foot.rectTransform.offsetMin.y);
            foot.rectTransform.offsetMax = new Vector2(-60, foot.rectTransform.offsetMax.y);
            content.sizeDelta = new Vector2(0, y + 140);
        }

        Text StatBox(Transform parent, int i, string label, Color color)
        {
            var box = UIKit.Frame(parent, "inset", "Stat" + i).rectTransform;
            box.anchorMin = new Vector2(i * 0.25f, 0);
            box.anchorMax = new Vector2((i + 1) * 0.25f, 1);
            box.offsetMin = new Vector2(i == 0 ? 30 : 8, 10);
            box.offsetMax = new Vector2(i == 3 ? -30 : -8, -10);
            var v = UIKit.Label(box, "0", 76, color, true, TextAnchor.MiddleCenter, "Value");
            v.rectTransform.Stretch(0, 56, 0, 10);
            var l = UIKit.Label(box, label, 30, UIKit.Dim, true, TextAnchor.MiddleCenter, "Label");
            l.rectTransform.BottomStrip(50, 18);
            return v;
        }

        Row BuildRow(RectTransform parent, int t, float y)
        {
            var r = new Row { tier = t };
            var holder = UIKit.Rect("League" + t, parent).TopStrip(232, y);
            holder.offsetMin = new Vector2(40, holder.offsetMin.y);
            holder.offsetMax = new Vector2(-40, holder.offsetMax.y);
            r.highlight = UIKit.Frame(holder, "btn_gold", "Highlight");
            r.highlight.rectTransform.Stretch(-12, -12, -12, -12);
            r.frame = UIKit.Frame(holder, "panel", "Panel");
            r.root = r.frame.rectTransform.Fill();

            r.emblem = UIKit.Img(r.root, ArtCache.Emblem(t), Color.white, "Emblem");
            r.emblem.rectTransform.At(0f, 0.5f, 110, 0, 28 * 5f, 32 * 5f);

            var tierCol = ArtCache.ToColor(Palette.Tier(t));
            r.name = UIKit.Label(r.root, League.Names[t], 64, tierCol, true, TextAnchor.MiddleLeft, "Name");
            r.name.rectTransform.Place(0f, 1f, 210, -22, 420, 74);
            var needRow = UIKit.Rect("Need", r.root).Place(0f, 1f, 212, -98, 360, 56);
            r.need = UIKit.IconValue(needRow, "ui_trophy", League.Thresholds[t].ToString("N0") + "+", 44, UIKit.TextCol, 0.75f, out _);

            var parts = new List<string>();
            int newCards = League.NewCardsAt(t);
            if (newCards > 0 && t > 0) parts.Add("+" + newCards + " cards");
            if (t > 0 && League.StarterSlots[t] > League.StarterSlots[t - 1]) parts.Add("+1 starter slot");
            if (t == 0) parts.Add(newCards + " cards");
            r.unlocks = UIKit.Label(r.root, string.Join("   ", parts.ToArray()), 36, UIKit.Gold, false, TextAnchor.MiddleLeft, "Unlocks");
            r.unlocks.rectTransform.Place(0f, 0f, 212, 22, 480, 50);

            // Шагнал (баруун тал)
            if (League.RewardGems[t] > 0 || League.RewardGold[t] > 0)
            {
                var rl = UIKit.Label(r.root, "REWARD", 30, UIKit.Dim, true, TextAnchor.MiddleCenter, "RewardLabel");
                rl.rectTransform.Place(1f, 1f, -40, -22, 240, 40);
                var gemRow = UIKit.Rect("Gems", r.root).Place(1f, 1f, -40, -70, 240, 60);
                UIKit.IconValue(gemRow, "ui_gem", League.RewardGems[t].ToString("N0"), 46, UIKit.GemCol, 0.85f, out _);
                var goldRow = UIKit.Rect("Gold", r.root).Place(1f, 1f, -40, -136, 240, 60);
                UIKit.IconValue(goldRow, "ui_coin", League.RewardGold[t].ToString("N0"), 46, UIKit.Gold, 0.85f, out _);
            }

            r.status = UIKit.Pix(r.root, "ui_check", 1.3f, "Status");
            r.status.rectTransform.Place(1f, 1f, 18, 18, 80, 80);
            return r;
        }

        public override void OnShow()
        {
            Refresh();
            Save.Changed += Refresh;
            scroll.verticalNormalizedPosition = 1f;
        }

        public override void OnHide() => Save.Changed -= Refresh;

        void Refresh()
        {
            var p = Save.P;
            int tier = League.TierOf(p.trophies);
            emblem.sprite = ArtCache.Emblem(tier);
            tierName.text = League.Names[tier];
            tierName.color = ArtCache.ToColor(Palette.Tier(tier));
            trophies.text = p.trophies.ToString("N0");
            best.text = "Best: " + Mathf.Max(p.bestTrophies, p.trophies).ToString("N0") + " trophies";
            int next = League.NextThreshold(p.trophies);
            bar.Set(League.Progress(p.trophies), true);
            nextText.text = next > 0
                ? (next - p.trophies).ToString("N0") + " more trophies to " + League.Names[tier + 1]
                : "You reached the top league!";

            wins.text = UIKit.Num(p.wins);
            losses.text = UIKit.Num(p.losses);
            int total = p.wins + p.losses;
            rate.text = total > 0 ? Mathf.RoundToInt(100f * p.wins / total) + "%" : "-";
            streak.text = p.bestStreak.ToString();

            foreach (var r in rows)
            {
                bool reached = r.tier <= tier;
                bool current = r.tier == tier;
                r.highlight.gameObject.SetActive(current);
                r.frame.color = reached ? Color.white : new Color(0.7f, 0.7f, 0.8f, 1f);
                r.emblem.color = reached ? Color.white : new Color(0.4f, 0.4f, 0.5f, 1f);
                r.status.gameObject.SetActive(true);
                r.status.SetPix(reached ? "ui_check" : "ui_lock", 1.3f);
                r.status.gameObject.SetActive(!current);
                r.name.text = League.Names[r.tier] + (current ? "  (you)" : "");
            }
        }
    }
}
