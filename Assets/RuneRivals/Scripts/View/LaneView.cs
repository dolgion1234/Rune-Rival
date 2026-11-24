using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>
    /// Нэг талын тулааны талбайг зурна: арын зураг, баатар/цэрэг/дайсан, сум, эффект, тоо.
    /// Тулааны логик (Lane) Unity-гүй; энд зөвхөн харагдац. Байрлалыг 2 алхмын хооронд гөлгөр шилжүүлнэ.
    /// </summary>
    public sealed class LaneView
    {
        // Талбайн координат → арын зургийн пиксел
        const float LaneUnits = 110f;
        const float RoadTop = 56f, RoadSpan = 26f;     // y: 0..1 → пиксел 56..82 (дээрээс)
        const float FlyLift = 13f;

        public readonly Side side;
        public readonly bool isPlayer;
        public RectTransform root, shaker, art, unitLayer, fxLayer, textLayer;
        Image artImg, skyFill, portal, flashOverlay;
        float scale = ArtCache.PixelScale;   // UI нэгж / арын зургийн пиксел
        float artW = 1080, artH = 470;

        readonly Dictionary<Unit, UnitView> views = new Dictionary<Unit, UnitView>();
        readonly List<UnitView> dying = new List<UnitView>();
        readonly List<UnitView> sortBuf = new List<UnitView>();
        readonly HashSet<Unit> seen = new HashSet<Unit>();
        readonly Dictionary<Projectile, ShotView> shots = new Dictionary<Projectile, ShotView>();
        readonly HashSet<Projectile> seenShots = new HashSet<Projectile>();
        readonly List<Projectile> removeShots = new List<Projectile>();
        readonly List<Image> trapViews = new List<Image>();
        readonly List<Image> incomingIcons = new List<Image>();
        RectTransform incomingRow;
        Text incomingLabel;

        readonly Fx fx;
        float shake, flash;
        float time;
        int textBudget;

        sealed class ShotView
        {
            public Image img;
            public float startX, totalDist, lastAngle;
            public bool arc, spin;
            public float trail;
        }

        public LaneView(Transform parent, Side side, bool isPlayer, int theme)
        {
            this.side = side;
            this.isPlayer = isPlayer;
            root = UIKit.Rect(isPlayer ? "PlayerLane" : "RivalLane", parent);
            root.gameObject.AddComponent<RectMask2D>();
            // Тусдаа canvas: нэгжүүд хөдлөхөд бусад UI дахин зурагдахгүй
            root.gameObject.AddComponent<Canvas>();

            shaker = UIKit.Rect("Shaker", root).Fill();
            skyFill = UIKit.Block(shaker, Color.black, "Sky");
            skyFill.rectTransform.Fill();
            artImg = UIKit.Img(shaker, ArtCache.Lane(theme, isPlayer), Color.white, "Art");
            art = artImg.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0f);
            art.pivot = new Vector2(0.5f, 0f);

            // Тэнгэрийн дээд пикселийн өнгөөр дүүргэнэ
            var laneCanvas = ProcArt.Lane(theme, isPlayer ? Palette.Ally : Palette.Foe, 0);
            skyFill.color = ArtCache.ToColor(laneCanvas.Get(0, 0));

            portal = UIKit.Pix(art, "ui_portal", 1f, "Portal");
            unitLayer = UIKit.Rect("Units", art).Fill();
            fxLayer = UIKit.Rect("Fx", art).Fill();
            textLayer = UIKit.Rect("Texts", art).Fill();
            fx = new Fx(fxLayer, textLayer);

            incomingRow = UIKit.Rect("Incoming", art);
            incomingLabel = UIKit.Outlined(incomingRow, "INCOMING", 34, ArtCache.ToColor(Palette.Magenta), "Label");
            incomingLabel.rectTransform.Place(1f, 1f, 0, 0, 300, 44);
            incomingLabel.alignment = TextAnchor.MiddleRight;
            incomingRow.gameObject.SetActive(false);

            flashOverlay = UIKit.Block(root, new Color(1, 1, 1, 0), "Flash");
            flashOverlay.rectTransform.Fill();
        }

        // ================= Байрлал =================

        public void Layout(float width, float height)
        {
            artW = width;
            artH = width * ProcArt.LaneH / ProcArt.LaneW;
            scale = artW / ProcArt.LaneW;
            art.sizeDelta = new Vector2(artW, artH);
            art.anchoredPosition = Vector2.zero;
            portal.rectTransform.sizeDelta = new Vector2(16, 15) * Mathf.Round(scale);
            portal.rectTransform.anchorMin = portal.rectTransform.anchorMax = Vector2.zero;
            portal.rectTransform.pivot = new Vector2(0.5f, 0f);
            portal.rectTransform.anchoredPosition = new Vector2(208f * scale, (ProcArt.LaneH - 74f) * scale);
            incomingRow.anchorMin = incomingRow.anchorMax = Vector2.zero;
            incomingRow.pivot = new Vector2(1f, 1f);
            incomingRow.sizeDelta = new Vector2(400, 110);
            incomingRow.anchoredPosition = new Vector2(artW - 14, (ProcArt.LaneH - 26f) * scale);
            foreach (var v in views.Values) v.Rescale(Mathf.Round(scale));
        }

        /// <summary>Талбайн (x, y) → art доторх UI байрлал (доод зүүн булангаас).</summary>
        public Vector2 ToLocal(float x, float y, float lift = 0f)
        {
            float px = x / LaneUnits * ProcArt.LaneW;
            float py = RoadTop + Mathf.Clamp01(y) * RoadSpan;   // дээрээс
            return new Vector2(px * scale, (ProcArt.LaneH - py + lift) * scale);
        }

        public float Scale => scale;

        // ================= Кадр бүр =================

        public void Sync(float alpha, float dt, bool fighting)
        {
            time += dt;
            textBudget = 3;
            var lane = side.lane;

            // Нэгжүүд
            seen.Clear();
            for (int i = 0; i < lane.units.Count; i++)
            {
                var u = lane.units[i];
                if (!views.TryGetValue(u, out var v))
                {
                    v = new UnitView(this, u, Mathf.Round(scale));
                    views[u] = v;
                }
                seen.Add(u);
            }

            sortBuf.Clear();
            foreach (var kv in views)
            {
                var v = kv.Value;
                if (!seen.Contains(kv.Key) || (!kv.Key.alive && !kv.Key.IsHero))
                {
                    if (!v.dying) { v.dying = true; dying.Add(v); }
                }
                sortBuf.Add(v);
            }

            foreach (var v in sortBuf) v.Update(alpha, dt, fighting, time);

            // Үхэж дууссан дүрсийг устгана
            for (int i = dying.Count - 1; i >= 0; i--)
            {
                var v = dying[i];
                if (v.deathT >= UnitView.DeathTime)
                {
                    views.Remove(v.u);
                    v.Destroy();
                    dying.RemoveAt(i);
                }
            }

            // Гүн: доод талын (ойрын) дүрс урд харагдана
            sortBuf.Sort((a, b) => b.sortY.CompareTo(a.sortY));
            for (int i = 0; i < sortBuf.Count; i++)
                if (sortBuf[i].rt != null) sortBuf[i].rt.SetSiblingIndex(i);

            SyncShots(alpha, dt);
            SyncTraps();
            SyncIncoming();

            // Портал лугшина
            float ps = 1f + Mathf.Sin(time * 4f) * 0.06f;
            portal.rectTransform.localScale = new Vector3(ps, ps, 1);

            fx.Update(dt);

            // Чичиргээ, гэрэл
            if (shake > 0f)
            {
                shake = Mathf.Max(0f, shake - dt * 3f);
                float a = shake * shake * 22f;
                shaker.anchoredPosition = new Vector2(Random.Range(-a, a), Random.Range(-a, a));
            }
            else shaker.anchoredPosition = Vector2.zero;
            if (flash > 0f)
            {
                flash = Mathf.Max(0f, flash - dt * 3f);
                flashOverlay.color = new Color(1f, 1f, 0.9f, flash * 0.5f);
            }
        }

        void SyncShots(float alpha, float dt)
        {
            var list = side.lane.shots;
            seenShots.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (!p.alive) continue;
                seenShots.Add(p);
                if (!shots.TryGetValue(p, out var sv))
                {
                    sv = CreateShot(p);
                    shots[p] = sv;
                }
                float x = Mathf.Lerp(p.prevX, p.x, alpha), y = Mathf.Lerp(p.prevY, p.y, alpha);
                float lift = p.fromEnemy && p.owner != null && p.owner.Flying ? FlyLift : 7f;
                if (sv.arc && sv.totalDist > 0.5f)
                {
                    float k = Mathf.Clamp01(Mathf.Abs(x - sv.startX) / sv.totalDist);
                    lift += Mathf.Sin(k * Mathf.PI) * Mathf.Min(22f, sv.totalDist * 0.35f);
                }
                var pos = ToLocal(x, y, lift);
                var rt = sv.img.rectTransform;
                var prev = rt.anchoredPosition;
                rt.anchoredPosition = pos;
                var d = pos - prev;
                if (sv.spin) rt.Rotate(0, 0, -720f * dt);
                else if (d.sqrMagnitude > 0.5f)
                {
                    sv.lastAngle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    rt.localEulerAngles = new Vector3(0, 0, sv.lastAngle);
                }
                sv.trail -= dt;
                if (sv.trail <= 0f && (p.kind == Shot.Fireball || p.kind == Shot.Frost || p.kind == Shot.Ember || p.kind == Shot.Magic))
                {
                    sv.trail = 0.04f;
                    Color c = p.kind == Shot.Frost ? UIKit.GemCol : p.kind == Shot.Magic ? new Color(0.7f, 1f, 0.4f) : new Color(1f, 0.6f, 0.2f);
                    fx.Particle(pos, new Vector2(Random.Range(-20f, 20f), Random.Range(10f, 40f)), c, scale * 1.2f, 0.3f, 0f);
                }
            }
            removeShots.Clear();
            foreach (var kv in shots) if (!seenShots.Contains(kv.Key)) removeShots.Add(kv.Key);
            foreach (var p in removeShots)
            {
                Object.Destroy(shots[p].img.gameObject);
                shots.Remove(p);
            }
        }

        ShotView CreateShot(Projectile p)
        {
            string sprite;
            switch (p.kind)
            {
                case Shot.Arrow: sprite = "arrow"; break;
                case Shot.BoneArrow: sprite = "bonearrow"; break;
                case Shot.Fireball: sprite = "fireball"; break;
                case Shot.Frost: sprite = "frostbolt"; break;
                case Shot.Magic: sprite = "magic"; break;
                case Shot.Cannonball: sprite = "cannonball"; break;
                case Shot.Ember: sprite = "ember"; break;
                default: sprite = "arrow"; break;
            }
            var img = UIKit.Pix(fxLayer, sprite, Mathf.Round(scale) / ArtCache.PixelScale, "Shot");
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero;
            img.rectTransform.anchoredPosition = ToLocal(p.x, p.y, 7f);
            img.transform.SetAsFirstSibling();
            var sv = new ShotView { img = img, startX = p.x };
            sv.totalDist = p.target != null ? Mathf.Abs(p.target.x - p.x) : 0f;
            sv.arc = p.kind == Shot.Cannonball || p.kind == Shot.Fireball || p.kind == Shot.BoneArrow || p.kind == Shot.Ember;
            sv.spin = p.kind == Shot.Cannonball || p.kind == Shot.Magic;
            return sv;
        }

        void SyncTraps()
        {
            int n = 0;
            foreach (var t in side.lane.traps)
            {
                if (!t.alive) continue;
                if (n >= trapViews.Count)
                {
                    var img = UIKit.Pix(unitLayer, "icon_trap", Mathf.Round(scale) * 0.7f / ArtCache.PixelScale, "Trap");
                    img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero;
                    img.rectTransform.pivot = new Vector2(0.5f, 0.2f);
                    trapViews.Add(img);
                }
                var v = trapViews[n++];
                v.gameObject.SetActive(true);
                v.rectTransform.anchoredPosition = ToLocal(t.x, t.y);
                v.transform.SetAsFirstSibling();
            }
            for (int i = n; i < trapViews.Count; i++) trapViews[i].gameObject.SetActive(false);
        }

        void SyncIncoming()
        {
            var inc = side.incoming;
            incomingRow.gameObject.SetActive(inc.Count > 0);
            if (inc.Count == 0) return;
            // Төрөл бүрийг нэг дүрсээр
            int shown = 0;
            string last = null;
            for (int i = 0; i < inc.Count && shown < 6; i++)
            {
                if (inc[i].sprite == last) continue;
                last = inc[i].sprite;
                if (shown >= incomingIcons.Count)
                {
                    var img = UIKit.Img(incomingRow, null, Color.white, "Icon");
                    img.preserveAspect = true;
                    incomingIcons.Add(img);
                }
                var ic = incomingIcons[shown];
                ic.gameObject.SetActive(true);
                ic.sprite = ArtCache.Get(last);
                var px = ArtCache.PixelSize(last);
                float k = Mathf.Min(4f, 56f / Mathf.Max(px.x, px.y)) * scale / ArtCache.PixelScale;
                ic.rectTransform.Place(1f, 0f, -shown * 70f, 0, px.x * k, px.y * k);
                shown++;
            }
            for (int i = shown; i < incomingIcons.Count; i++) incomingIcons[i].gameObject.SetActive(false);
            incomingLabel.text = "INCOMING x" + inc.Count;
            float pulse = 0.75f + 0.25f * Mathf.Sin(time * 8f);
            incomingLabel.color = new Color(1f, 0.5f * pulse + 0.3f, 0.95f, 1f);
        }

        // ================= Үйл явдал =================

        UnitView ViewOf(Unit u) => u != null && views.TryGetValue(u, out var v) ? v : null;

        public void OnEvent(BattleEvent e)
        {
            var audio = AudioManager.Instance;
            float vol = isPlayer ? 1f : 0.35f;
            switch (e.type)
            {
                case Ev.Spawn:
                {
                    var pos = ToLocal(e.x, e.y);
                    if (e.unit != null && e.unit.enemy)
                    {
                        if (e.unit.sent)
                        {
                            fx.Burst(pos + new Vector2(0, 30), new Color(0.85f, 0.4f, 1f), 10, scale * 1.4f, 160f);
                            if (isPlayer) audio?.Play("warn", 0.5f, 1f, 0.5f);
                        }
                        else fx.Burst(pos + new Vector2(0, 20), new Color(0.6f, 0.4f, 0.9f), 4, scale, 90f);
                    }
                    else if (e.unit != null)
                    {
                        fx.Beam(pos, new Color(1f, 0.95f, 0.6f), scale);
                        fx.Burst(pos + new Vector2(0, 20), new Color(1f, 0.92f, 0.5f), 8, scale * 1.2f, 140f);
                        if (isPlayer) audio?.Play("spawn", 0.7f);
                    }
                    break;
                }
                case Ev.Attack:
                    if (isPlayer) audio?.Play("hit", 0.45f, Random.Range(0.9f, 1.15f), 0.06f);
                    break;
                case Ev.Shoot:
                    if (isPlayer)
                    {
                        if (e.shot == Shot.Cannonball) { audio?.Play("boom", 0.6f, 1f, 0.15f); Shake(0.25f); }
                        else if (e.shot == Shot.Fireball) audio?.Play("fire", 0.5f, Random.Range(0.9f, 1.1f), 0.1f);
                        else if (e.shot == Shot.Frost) audio?.Play("frost", 0.45f, Random.Range(0.95f, 1.1f), 0.1f);
                        else audio?.Play("shoot", 0.35f, Random.Range(0.9f, 1.2f), 0.06f);
                    }
                    break;
                case Ev.Hit:
                {
                    var v = ViewOf(e.unit);
                    v?.Hit();
                    var pos = ToLocal(e.x, e.y, (e.unit != null && e.unit.Flying ? FlyLift : 0f) + 8f);
                    Color spark = e.unit != null && e.unit.enemy ? new Color(1f, 0.95f, 0.7f) : new Color(1f, 0.45f, 0.4f);
                    fx.Burst(pos, spark, e.crit ? 7 : 3, scale * 0.9f, e.crit ? 200f : 120f);
                    bool hero = e.unit != null && e.unit.IsHero;
                    if (e.crit) ShowText(pos + new Vector2(0, 40), Mathf.RoundToInt(e.value) + "!", UIKit.Gold, 56, true);
                    else if (hero) ShowText(pos + new Vector2(0, 40), "-" + Mathf.RoundToInt(e.value), UIKit.BadCol, 44, true);
                    else if (isPlayer && e.unit != null && e.unit.enemy) ShowText(pos + new Vector2(0, 34), Mathf.RoundToInt(e.value).ToString(), Color.white, 34, false);
                    if (hero && isPlayer)
                    {
                        audio?.Play("hurt", 0.6f, 1f, 0.12f);
                        if (e.value > side.lane.hero.maxHp * 0.06f) { Shake(0.35f); Haptics.Light(); }
                    }
                    break;
                }
                case Ev.Dodge:
                    ShowText(ToLocal(e.x, e.y, 30f), "MISS", UIKit.Dim, 40, true);
                    break;
                case Ev.Heal:
                    if (e.value >= 3f)
                    {
                        var pos = ToLocal(e.x, e.y, 26f);
                        ShowText(pos, "+" + Mathf.RoundToInt(e.value), UIKit.GoodCol, 40, e.unit != null && e.unit.IsHero);
                        fx.Burst(pos, new Color(0.5f, 1f, 0.5f), 4, scale, 60f, -60f);
                        if (isPlayer && e.unit != null && e.unit.IsHero) audio?.Play("heal", 0.5f, 1f, 0.3f);
                    }
                    break;
                case Ev.Death:
                {
                    var u = e.unit;
                    if (u == null) break;
                    var pos = ToLocal(e.x, e.y, (u.Flying ? FlyLift : 0f) + 10f);
                    var col = ArtCache.MainColor(u.def.sprite);
                    bool big = u.def.role == UnitRole.MonsterBoss || u.def.role == UnitRole.MonsterElite || u.IsHero || u.def.role == UnitRole.Boss;
                    fx.Burst(pos, col, big ? 22 : 10, scale * (big ? 1.6f : 1.2f), big ? 260f : 170f);
                    fx.Burst(pos, Color.white, 4, scale, 120f);
                    if (isPlayer || big)
                        audio?.Play(u.enemy ? "squish" : "death", vol * (big ? 1f : 0.6f), Random.Range(0.9f, 1.1f), 0.05f);
                    if (big) Shake(isPlayer ? 0.5f : 0.25f);
                    break;
                }
                case Ev.TrapSnap:
                    ShowText(ToLocal(e.x, e.y, 30f), "STUN!", UIKit.Gold, 44, true);
                    fx.Burst(ToLocal(e.x, e.y, 6f), new Color(0.8f, 0.85f, 0.95f), 8, scale, 150f);
                    if (isPlayer) audio?.Play("trap", 0.8f);
                    break;
                case Ev.Slow:
                    fx.Burst(ToLocal(e.x, e.y, 14f), UIKit.GemCol, 5, scale, 80f);
                    break;
                case Ev.Smite:
                {
                    var pos = ToLocal(e.x, e.y);
                    fx.Bolt(pos, artH, scale);
                    ShowText(pos + new Vector2(0, 120), "-" + Mathf.RoundToInt(e.value), UIKit.Gold, 60, true);
                    fx.Burst(pos + new Vector2(0, 20), new Color(0.8f, 0.95f, 1f), 16, scale * 1.3f, 240f);
                    flash = 1f;
                    Shake(0.6f);
                    audio?.Play("smite", vol);
                    if (isPlayer) Haptics.Medium();
                    break;
                }
                case Ev.Poison:
                {
                    var pos = ToLocal(e.x, e.y, 30f);
                    ShowText(pos, "-" + Mathf.RoundToInt(e.value), new Color(0.6f, 1f, 0.3f), 50, true);
                    fx.Burst(pos, new Color(0.5f, 0.9f, 0.2f), 10, scale * 1.2f, 90f, -50f);
                    if (isPlayer) audio?.Play("hurt", 0.6f);
                    break;
                }
                case Ev.Promote:
                {
                    var pos = ToLocal(e.x, e.y);
                    fx.Beam(pos, UIKit.Gold, scale * 1.4f);
                    ShowText(pos + new Vector2(0, 110), "PROMOTED!", UIKit.Gold, 46, true);
                    if (isPlayer) audio?.Play("levelup", 0.7f);
                    break;
                }
                case Ev.Energy:
                    if (e.value > 0 && e.text == null)
                        ShowText(ToLocal(e.x, e.y, 40f), "+" + Mathf.RoundToInt(e.value) + " Energy", UIKit.Gold, 42, true);
                    break;
                case Ev.Text:
                    if (e.text == "blast")
                    {
                        var pos = ToLocal(e.x, e.y, 6f);
                        float r = e.value / LaneUnits * ProcArt.LaneW * scale;
                        Color c = e.shot == Shot.Cannonball ? new Color(1f, 0.85f, 0.5f) : e.shot == Shot.Frost ? UIKit.GemCol : new Color(1f, 0.55f, 0.15f);
                        fx.Ring(pos, r * 2.2f, c);
                        fx.Burst(pos, c, 8, scale * 1.3f, 180f);
                    }
                    break;
            }
        }

        void ShowText(Vector2 pos, string text, Color color, int size, bool important)
        {
            if (!important)
            {
                if (textBudget <= 0) return;
                textBudget--;
            }
            fx.Text(pos, text, color, Mathf.RoundToInt(size * scale / ArtCache.PixelScale), important);
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);
        public void Flash(float amount) => flash = Mathf.Max(flash, amount);

        /// <summary>Картын эффект: бүх манай нэгж дээр гялбаа (сайжруулалт авахад).</summary>
        public void BoostAll(Color c, string text)
        {
            int n = 0;
            foreach (var u in side.lane.units)
            {
                if (!u.alive || u.enemy) continue;
                var pos = ToLocal(u.x, u.y, 14f);
                fx.Burst(pos, c, 5, scale, 90f, -80f);
                n++;
            }
            var hero = side.lane.hero;
            if (!string.IsNullOrEmpty(text) && hero != null)
                ShowText(ToLocal(hero.x + 14f, 0.15f, 30f), text, c, 46, true);
        }

        /// <summary>Энэ талбайн дэлгэцийн байрлал (UI доторх) — карт нисгэхэд.</summary>
        public Vector3 WorldCenter => art.TransformPoint(new Vector3(artW * 0.5f, artH * 0.4f, 0));
        public Vector3 HeroWorld
        {
            get
            {
                var h = side.lane.hero;
                var local = ToLocal(h.x, h.y, 12f);
                return art.TransformPoint(new Vector3(local.x, local.y, 0));
            }
        }

        public void Clear()
        {
            foreach (var v in views.Values) v.Destroy();
            views.Clear();
            dying.Clear();
            foreach (var s in shots.Values) Object.Destroy(s.img.gameObject);
            shots.Clear();
            fx.Clear();
        }

        // =================================================================
        // Нэг нэгжийн дүрс
        // =================================================================

        sealed class UnitView
        {
            public const float DeathTime = 0.45f;
            public readonly Unit u;
            public RectTransform rt, bodyRt, hpRoot, hpFill;
            Image body, shadow, hpFillImg, stun;
            Sprite normal, white;
            readonly LaneView lane;
            public float deathT, sortY;
            public bool dying;
            float flashT, spawnT, hpShow, phase, k;
            Vector2Int px;

            public UnitView(LaneView lane, Unit u, float k)
            {
                this.lane = lane;
                this.u = u;
                phase = (u.id * 1.37f) % 6.28f;
                normal = ArtCache.Get(u.def.sprite);
                white = ArtCache.Flash(u.def.sprite);
                px = ArtCache.PixelSize(u.def.sprite);

                rt = UIKit.Rect(u.def.id, lane.unitLayer);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0f);

                shadow = UIKit.Img(rt, ArtCache.Shadow, Color.white, "Shadow");
                shadow.rectTransform.anchorMin = shadow.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                shadow.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                body = UIKit.Img(rt, normal, Color.white, "Body");
                bodyRt = body.rectTransform;
                bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 0f);
                bodyRt.pivot = new Vector2(0.5f, 0f);

                if (u.sent)
                {
                    var aura = UIKit.Img(rt, ArtCache.Glow, new Color(0.8f, 0.3f, 1f, 0.55f), "Aura");
                    aura.rectTransform.anchorMin = aura.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    aura.rectTransform.sizeDelta = new Vector2(px.x, px.y * 0.5f) * k * 1.3f;
                    aura.transform.SetSiblingIndex(1);
                }

                // HP мөр
                hpRoot = UIKit.Rect("Hp", rt);
                hpRoot.anchorMin = hpRoot.anchorMax = new Vector2(0.5f, 0f);
                hpRoot.pivot = new Vector2(0.5f, 0f);
                var back = UIKit.Block(hpRoot, UIKit.Ink, "Back");
                back.rectTransform.Fill();
                hpFillImg = UIKit.Block(hpRoot, Color.white, "Fill");
                hpFill = hpFillImg.rectTransform;
                hpFillImg.color = u.enemy ? (u.sent ? new Color(0.85f, 0.4f, 1f) : UIKit.BadCol) : u.IsHero ? UIKit.AllyCol : UIKit.GoodCol;

                stun = UIKit.Img(rt, ArtCache.Get("ui_star"), Color.white, "Stun");
                stun.rectTransform.anchorMin = stun.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                stun.gameObject.SetActive(false);

                Rescale(k);
                spawnT = u.IsHero ? 1f : 0f;
            }

            public void Rescale(float baseK)
            {
                k = Mathf.Max(1f, Mathf.Round(baseK * u.def.scale));
                var size = new Vector2(px.x, px.y) * k;
                rt.sizeDelta = size;
                bodyRt.sizeDelta = size;
                shadow.rectTransform.sizeDelta = new Vector2(Mathf.Max(10, px.x - 2), 4) * k;
                bool big = u.IsHero || u.def.role == UnitRole.MonsterBoss || u.def.role == UnitRole.Boss || u.def.role == UnitRole.MonsterElite;
                float barW = Mathf.Clamp(px.x * k * 0.8f, 8 * k, 22 * k);
                hpRoot.sizeDelta = new Vector2(barW, (big ? 2.4f : 1.6f) * k);
                stun.rectTransform.sizeDelta = new Vector2(11, 9) * k * 0.5f;
            }

            public void Hit()
            {
                flashT = 0.09f;
                hpShow = 2.5f;
            }

            public void Update(float alpha, float dt, bool fighting, float time)
            {
                if (rt == null) return;
                float x = Mathf.Lerp(u.prevX, u.x, alpha), y = Mathf.Lerp(u.prevY, u.y, alpha);
                float lift = u.Flying ? FlyLift + Mathf.Sin(time * 6f + phase) * 2f : 0f;
                var pos = lane.ToLocal(x, y);
                sortY = pos.y;
                float bob = 0f, lunge = 0f, tilt = 0f;

                if (dying)
                {
                    deathT += dt;
                    float d = Mathf.Clamp01(deathT / DeathTime);
                    body.sprite = d < 0.25f && white != null ? white : normal;
                    body.color = new Color(1, 1, 1, 1f - d);
                    shadow.color = new Color(1, 1, 1, 1f - d);
                    bodyRt.localScale = new Vector3(1f + d * 0.3f, 1f - d * 0.5f, 1f);
                    hpRoot.gameObject.SetActive(false);
                    stun.gameObject.SetActive(false);
                    rt.anchoredPosition = pos + new Vector2(0, lift * lane.scale);
                    return;
                }

                if (!u.alive && u.IsHero)
                {
                    // Унасан баатар
                    bodyRt.localEulerAngles = new Vector3(0, 0, 90f);
                    body.color = new Color(0.55f, 0.55f, 0.6f, 1f);
                    hpRoot.gameObject.SetActive(false);
                    rt.anchoredPosition = pos;
                    return;
                }

                if (fighting && u.moving)
                {
                    bob = Mathf.Abs(Mathf.Sin(time * 11f + phase)) * 1.5f;
                    tilt = Mathf.Sin(time * 11f + phase) * 5f;
                }
                else
                {
                    bob = Mathf.Sin(time * 3f + phase) > 0.6f ? 1f : 0f;
                }

                if (u.attackAnim < 0.2f)
                {
                    float a = Mathf.Sin(u.attackAnim / 0.2f * Mathf.PI);
                    float dir = u.enemy ? -1f : 1f;
                    lunge = u.def.ranged ? -a * 1.5f * dir : a * 3.5f * dir;
                    tilt += -a * 8f * dir;
                }

                rt.anchoredPosition = pos;
                bodyRt.anchoredPosition = new Vector2(Mathf.Round(lunge) * k, (Mathf.Round(bob) + lift) * k);
                bodyRt.localEulerAngles = new Vector3(0, 0, tilt);
                shadow.rectTransform.anchoredPosition = new Vector2(0, k);
                float shadowScale = u.Flying ? 0.7f : 1f;
                shadow.rectTransform.localScale = new Vector3(shadowScale, shadowScale, 1f);

                // Гарч ирэх анимаци
                if (spawnT < 1f)
                {
                    spawnT = Mathf.Min(1f, spawnT + dt * 4f);
                    float s = BackOut(spawnT);
                    bodyRt.localScale = new Vector3(s, s, 1f);
                }
                else bodyRt.localScale = Vector3.one;

                // Анивчил, удаашрал
                if (flashT > 0f) flashT -= dt;
                bool fl = flashT > 0f;
                body.sprite = fl && white != null ? white : normal;
                body.color = fl && white == null ? new Color(1f, 0.5f, 0.5f) : u.slow > 0f ? new Color(0.65f, 0.88f, 1f) : Color.white;

                // HP
                hpShow -= dt;
                bool show = u.IsHero || u.def.role == UnitRole.MonsterBoss || (u.hp < u.maxHp - 0.5f && hpShow > -3f) || hpShow > 0f;
                hpRoot.gameObject.SetActive(show && u.alive);
                if (show)
                {
                    hpRoot.anchoredPosition = new Vector2(0, (px.y + 2 + lift) * k);
                    hpFill.anchorMin = Vector2.zero;
                    hpFill.anchorMax = new Vector2(Mathf.Clamp01(u.HpFrac), 1f);
                    hpFill.offsetMin = new Vector2(k * 0.4f, k * 0.4f);
                    hpFill.offsetMax = new Vector2(-k * 0.4f, -k * 0.4f);
                }

                // Гацсан
                bool stunned = u.stun > 0f;
                stun.gameObject.SetActive(stunned);
                if (stunned)
                {
                    stun.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(time * 8f) * 3f * k, (px.y + 4 + lift) * k);
                    stun.rectTransform.localEulerAngles = new Vector3(0, 0, time * 200f);
                }
            }

            static float BackOut(float t)
            {
                const float s = 1.70158f;
                t -= 1f;
                return 1f + (s + 1f) * t * t * t + s * t * t;
            }

            public void Destroy()
            {
                if (rt != null) Object.Destroy(rt.gameObject);
                rt = null;
            }
        }
    }
}
