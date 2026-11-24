using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuneRivals
{
    /// <summary>
    /// PixelCanvas → Texture2D/Sprite (Point filter). Бүх зураг нэг удаа үүсч кэшлэгдэнэ.
    /// Өөрийн зургаар солих бол Resources/RuneRivals/Art/&lt;нэр&gt;.png (жишээ нь hero_archer.png) тавина.
    /// 1 зургийн пиксел = PixelScale UI нэгж (1080 өргөн дэлгэц = 216 пиксел).
    /// </summary>
    public static class ArtCache
    {
        public const int PixelScale = 5;
        public const float Ppu = 100f / PixelScale;

        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> flashes = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Vector2Int> sizes = new Dictionary<string, Vector2Int>();
        static readonly Dictionary<string, Color32> mainColors = new Dictionary<string, Color32>();
        static Sprite white;

        public static Texture2D ToTexture(PixelCanvas c, string name, bool repeat = false)
        {
            var tex = new Texture2D(c.w, c.h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                name = name
            };
            var px = new Color32[c.w * c.h];
            for (int y = 0; y < c.h; y++)
            {
                int src = (c.h - 1 - y) * c.w, dst = y * c.w;
                for (int x = 0; x < c.w; x++)
                {
                    var p = c.px[src + x];
                    px[dst + x] = new Color32(p.r, p.g, p.b, p.a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        static Sprite MakeSprite(Texture2D tex, Vector4 border)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), Ppu, 0,
                SpriteMeshType.FullRect, border);
        }

        static Texture2D Override(string name)
        {
            var t = Resources.Load<Texture2D>("RuneRivals/Art/" + name);
            if (t != null)
            {
                t.filterMode = FilterMode.Point;
                t.wrapMode = TextureWrapMode.Clamp;
            }
            return t;
        }

        /// <summary>SpriteLib-ийн дүрс (1px хүрээтэй). Байхгүй бол null.</summary>
        public static Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (sprites.TryGetValue(name, out var s)) return s;
            var over = Override(name);
            if (over != null)
            {
                s = MakeSprite(over, Vector4.zero);
                sizes[name] = new Vector2Int(over.width, over.height);
            }
            else
            {
                var c = SpriteLib.Get(name);
                if (c == null) { sprites[name] = null; return null; }
                s = MakeSprite(ToTexture(c, name), Vector4.zero);
                sizes[name] = new Vector2Int(c.w, c.h);
            }
            sprites[name] = s;
            return s;
        }

        /// <summary>Цохилтын цагаан анивчил (зөвхөн кодоор зурсан дүрст).</summary>
        public static Sprite Flash(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (flashes.TryGetValue(name, out var s)) return s;
            s = null;
            if (Override(name) == null)
            {
                var c = SpriteLib.Get(name);
                if (c != null) s = MakeSprite(ToTexture(c.Silhouette(Palette.White), name + "_flash"), Vector4.zero);
            }
            flashes[name] = s;
            return s;
        }

        /// <summary>Зургийн пикселийн хэмжээ (хүрээтэй).</summary>
        public static Vector2Int PixelSize(string name)
        {
            if (!sizes.ContainsKey(name)) Get(name);
            return sizes.TryGetValue(name, out var v) ? v : new Vector2Int(8, 8);
        }

        /// <summary>Дүрсийн гол өнгө (үхэх үеийн тоосонцорт).</summary>
        public static Color MainColor(string name)
        {
            if (mainColors.TryGetValue(name, out var c)) return c;
            var raw = SpriteLib.Raw(name);
            var counts = new Dictionary<int, int>();
            int best = 0;
            Px bestPx = Palette.Slate2;
            if (raw != null)
                foreach (var p in raw.px)
                {
                    if (p.a == 0 || p.Equals(Palette.Ink) || p.Equals(Palette.White)) continue;
                    int k = (p.r << 16) | (p.g << 8) | p.b;
                    counts.TryGetValue(k, out int n);
                    counts[k] = ++n;
                    if (n > best) { best = n; bestPx = p; }
                }
            c = new Color32(bestPx.r, bestPx.g, bestPx.b, 255);
            mainColors[name] = c;
            return c;
        }

        // ---------------- Кодоор зурсан том зураг ----------------

        static readonly Dictionary<string, Sprite> procs = new Dictionary<string, Sprite>();

        public static Sprite Proc(string key, Func<PixelCanvas> make, bool repeat = false)
        {
            if (procs.TryGetValue(key, out var s)) return s;
            var over = Override(key);
            if (over != null)
            {
                if (repeat) over.wrapMode = TextureWrapMode.Repeat;
                s = MakeSprite(over, Vector4.zero);
            }
            else s = MakeSprite(ToTexture(make(), key, repeat), Vector4.zero);
            procs[key] = s;
            return s;
        }

        // ---------------- 9-slice хүрээ ----------------

        static readonly Dictionary<string, Sprite> frames = new Dictionary<string, Sprite>();

        static NineSlice MakeFrame(string name)
        {
            switch (name)
            {
                case "panel": return ProcArt.Panel();
                case "inset": return ProcArt.Inset();
                case "chip": return ProcArt.Chip();
                case "bar_back": return ProcArt.BarBack();
                case "bar_fill": return ProcArt.BarFill();
                case "btn_green": return ProcArt.Button(Palette.Green);
                case "btn_blue": return ProcArt.Button(Palette.Blue);
                case "btn_red": return ProcArt.Button(Palette.Red);
                case "btn_gold": return ProcArt.Button(Palette.Gold);
                case "btn_gray": return ProcArt.Button(Palette.Slate1);
                case "btn_purple": return ProcArt.Button(Palette.Purple);
                case "btn_orange": return ProcArt.Button(Palette.Orange);
                case "card_common": return ProcArt.Card(Palette.RarityCommon);
                case "card_rare": return ProcArt.Card(Palette.RarityRare);
                case "card_epic": return ProcArt.Card(Palette.RarityEpic);
                default: return ProcArt.Panel();
            }
        }

        public static Sprite Frame(string name)
        {
            if (frames.TryGetValue(name, out var s)) return s;
            var n = MakeFrame(name);
            s = MakeSprite(ToTexture(n.c, "frame_" + name), new Vector4(n.l, n.b, n.r, n.t));
            frames[name] = s;
            return s;
        }

        public static string CardFrame(Rarity r) =>
            r == Rarity.Epic ? "card_epic" : r == Rarity.Rare ? "card_rare" : "card_common";

        /// <summary>4x4 цагаан (өнгөөр будах тэгш өнцөгт).</summary>
        public static Sprite White
        {
            get
            {
                if (white != null) return white;
                var c = new PixelCanvas(4, 4);
                c.Fill(new Px(255, 255, 255));
                white = MakeSprite(ToTexture(c, "white"), Vector4.zero);
                return white;
            }
        }

        public static Sprite Circle => Proc("circle16", () => ProcArt.Circle(16));
        public static Sprite Glow => Proc("glow64", () => ProcArt.Glow(64));
        public static Sprite Rays => Proc("rays96", () => ProcArt.Rays(96));
        public static Sprite Shadow => Proc("shadow", ProcArt.Shadow);
        public static Sprite Ring => Proc("ring24", () =>
        {
            var c = new PixelCanvas(24, 24);
            c.Ring(12f, 12f, 12f, 2f, new Px(255, 255, 255));
            return c;
        });

        public static Sprite Emblem(int tier) => Proc("emblem" + tier, () => ProcArt.Emblem(tier));
        public static Sprite Lane(int theme, bool player) =>
            Proc("lane" + theme + (player ? "a" : "f"), () => ProcArt.Lane(theme, player ? Palette.Ally : Palette.Foe, player ? 100 + theme : 200 + theme));
        public static Sprite Home => Proc("home", () => ProcArt.Home());
        public static Sprite MenuPattern => Proc("menu_pattern", ProcArt.MenuPattern, true);
        public static Sprite GemPile(int n) => Proc("gems" + n, () => ProcArt.GemPile(n));
        public static Sprite NoAds => Proc("noads", ProcArt.NoAds);

        public static Color ToColor(Px p) => new Color32(p.r, p.g, p.b, p.a);
    }
}
