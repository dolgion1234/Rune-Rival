using System;

namespace RuneRivals
{
    /// <summary>9-slice зураг: хүрээний өргөн (пикселээр) l, t, r, b.</summary>
    public sealed class NineSlice
    {
        public PixelCanvas c;
        public int l, t, r, b;
        public NineSlice(PixelCanvas c, int l, int t, int r, int b) { this.c = c; this.l = l; this.t = t; this.r = r; this.b = b; }
    }

    /// <summary>
    /// Кодоор зурагддаг арын зураг, UI хүрээ, лигийн тэмдэг (бүгд Palette-ийн өнгөөр).
    /// Хэмжээ нь "виртуал пиксел": 1080 өргөн дэлгэц = 216 пиксел (ArtCache.PixelScale = 5).
    /// </summary>
    public static class ProcArt
    {
        static Px H(uint rgb) => Px.Hex(rgb);

        // ---------------- Туслах ----------------

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xffffff) / 16777216f;
            }
        }

        /// <summary>1D гөлгөр noise (0..1).</summary>
        static float Noise1(float x, int seed)
        {
            int i = (int)Math.Floor(x);
            float f = x - i;
            f = f * f * (3f - 2f * f);
            return Hash(i, 0, seed) * (1f - f) + Hash(i + 1, 0, seed) * f;
        }

        /// <summary>2D гөлгөр noise (0..1).</summary>
        static float Noise2(float x, float y, int seed)
        {
            int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(ix, iy, seed), b = Hash(ix + 1, iy, seed);
            float c = Hash(ix, iy + 1, seed), d = Hash(ix + 1, iy + 1, seed);
            return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy;
        }

        static float Ridge(float x, int seed, float scale) =>
            Noise1(x / scale, seed) * 0.65f + Noise1(x / (scale * 0.4f), seed + 7) * 0.25f + Noise1(x / (scale * 0.15f), seed + 13) * 0.1f;

        /// <summary>Уулын нуруу: ridge-ээс доош дүүргэнэ, оройд 1px гэрэл.</summary>
        static void Mountains(PixelCanvas c, int baseY, int amp, float scale, int seed, Px fill, Px rim)
        {
            for (int x = 0; x < c.w; x++)
            {
                int top = baseY - (int)(Ridge(x, seed, scale) * amp);
                for (int y = Math.Max(0, top); y < c.h; y++) c.Set(x, y, fill);
                c.Set(x, top, rim);
            }
        }

        // ---------------- Нүүр дэлгэцийн арын зураг ----------------

        public static PixelCanvas Home(int w = 216, int h = 384)
        {
            var c = new PixelCanvas(w, h);
            int horizon = (int)(h * 0.60f);
            c.VGradient(0, 0, w, horizon + 2, new[]
            {
                H(0x120e2c), H(0x1c1641), H(0x2b1d58), H(0x46266c), H(0x6e2f76), H(0xa24371), H(0xd8665f), H(0xf3975a)
            });

            var rng = new Rng(4242);
            for (int i = 0; i < 110; i++)
            {
                int x = rng.Range(0, w), y = rng.Range(0, (int)(horizon * 0.55f));
                byte a = (byte)rng.Range(90, 256);
                c.Blend(x, y, Palette.White.A(a));
                if (rng.Chance(0.08f))
                {
                    c.Blend(x - 1, y, Palette.White.A(90));
                    c.Blend(x + 1, y, Palette.White.A(90));
                    c.Blend(x, y - 1, Palette.White.A(90));
                    c.Blend(x, y + 1, Palette.White.A(90));
                }
            }

            // Жаргаж буй нар (dither цагираг)
            float sx = w * 0.70f, sy = horizon - 16;
            for (int r = 30; r >= 16; r -= 7)
                for (int y = (int)(sy - r); y <= sy + r; y++)
                    for (int x = (int)(sx - r); x <= sx + r; x++)
                    {
                        float dx = x - sx, dy = y - sy;
                        if (dx * dx + dy * dy <= r * r && PixelCanvas.Dither(x, y) < 0.35f) c.Blend(x, y, H(0xffc56a).A(150));
                    }
            c.Disc(sx, sy, 13, H(0xffd77a));
            c.Disc(sx - 2, sy - 2, 9, H(0xffeaa0));

            Mountains(c, horizon - 6, 34, 60f, 11, H(0x5a2f6e), H(0x8d4a80));
            Mountains(c, horizon + 6, 26, 42f, 23, H(0x3b2558), H(0x5d3770));

            // Цайз (зүүн толгод)
            Castle(c, 26, horizon + 2, H(0x22183d), Palette.Gold);

            // Урд талын тэгш газар: тайз
            int ground = (int)(h * 0.70f);
            for (int x = 0; x < w; x++)
            {
                int top = ground - (int)(Ridge(x, 51, 30f) * 6);
                for (int y = top; y < h; y++)
                {
                    float t = (y - top) / (float)(h - top);
                    c.Set(x, y, PixelCanvas.DitherMix(H(0x2b2246), H(0x1a1430), t * 1.4f, x, y));
                }
                c.Set(x, top, H(0x4a3766));
            }

            // Руны тойрог тайз (баатар дээр нь зогсоно)
            float cx = w / 2f, cy = h * 0.765f;
            Ellipse(c, cx, cy, 52, 13, H(0x3b2f5c));
            Ellipse(c, cx, cy - 1, 50, 11, H(0x51427a));
            EllipseRing(c, cx, cy - 1, 44, 9, H(0x8eeaf2).A(170));
            EllipseRing(c, cx, cy - 1, 36, 7, H(0x8eeaf2).A(90));
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                int rx = (int)(cx + Math.Cos(a) * 40), ry = (int)(cy - 1 + Math.Sin(a) * 8);
                c.Set(rx, ry, Palette.Ice);
                c.Set(rx + 1, ry, Palette.White);
            }

            // Өвсний ширхэг, чулуу
            for (int i = 0; i < 60; i++)
            {
                int x = rng.Range(0, w), y = rng.Range(ground + 4, h);
                if (Math.Abs(x - cx) < 56 && Math.Abs(y - cy) < 16) continue;
                Tuft(c, x, y, H(0x3d3060));
            }
            return c;
        }

        static void Castle(PixelCanvas c, int x0, int baseY, Px col, Px light)
        {
            // Хана
            c.FillRect(x0, baseY - 20, 46, 22, col);
            for (int i = 0; i < 46; i += 4) c.FillRect(x0 + i, baseY - 23, 2, 3, col);
            // Цамхагууд
            Tower(c, x0 - 4, baseY, 10, 40, col);
            Tower(c, x0 + 18, baseY, 12, 52, col);
            Tower(c, x0 + 40, baseY, 10, 36, col);
            // Цонх
            Px[] lit = { light, light.Shade(0.8f) };
            c.FillRect(x0 + 22, baseY - 44, 2, 3, lit[0]);
            c.FillRect(x0 + 26, baseY - 44, 2, 3, lit[1]);
            c.FillRect(x0 + 23, baseY - 32, 3, 4, lit[0]);
            c.FillRect(x0 - 1, baseY - 30, 2, 3, lit[0]);
            c.FillRect(x0 + 43, baseY - 26, 2, 3, lit[1]);
            c.FillRect(x0 + 8, baseY - 12, 2, 2, lit[1]);
            c.FillRect(x0 + 33, baseY - 12, 2, 2, lit[0]);
            // Хаалга
            c.FillRect(x0 + 21, baseY - 8, 6, 8, Palette.Ink);
            // Туг
            c.VLine(x0 + 24, baseY - 64, baseY - 52, col);
            c.FillRect(x0 + 25, baseY - 64, 6, 4, Palette.Red);
            c.FillRect(x0 + 25, baseY - 61, 4, 1, Palette.RedDark);
        }

        static void Tower(PixelCanvas c, int x, int baseY, int tw, int th, Px col)
        {
            c.FillRect(x, baseY - th, tw, th, col);
            for (int i = 0; i < tw; i += 3) c.FillRect(x + i, baseY - th - 3, 2, 3, col);
        }

        static void Ellipse(PixelCanvas c, float cx, float cy, float rx, float ry, Px col)
        {
            for (int y = (int)(cy - ry); y <= cy + ry; y++)
                for (int x = (int)(cx - rx); x <= cx + rx; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) c.Blend(x, y, col);
                }
        }

        static void EllipseRing(PixelCanvas c, float cx, float cy, float rx, float ry, Px col)
        {
            int n = (int)(rx * 7);
            int lx = int.MinValue, ly = int.MinValue;
            for (int i = 0; i < n; i++)
            {
                double a = i * 2 * Math.PI / n;
                int x = (int)Math.Round(cx + Math.Cos(a) * rx), y = (int)Math.Round(cy + Math.Sin(a) * ry);
                if (x == lx && y == ly) continue;
                c.Blend(x, y, col);
                lx = x; ly = y;
            }
        }

        static void Tuft(PixelCanvas c, int x, int y, Px col)
        {
            c.Set(x, y, col);
            c.Set(x - 1, y - 1, col);
            c.Set(x + 1, y - 1, col);
            c.Set(x, y - 2, col);
        }

        // ---------------- Тулааны зам (lane) ----------------

        public const int LaneW = 216, LaneH = 94;
        public const int LaneThemes = 4;

        sealed class LaneTheme
        {
            public Px[] sky;
            public Px far, farRim, near, nearRim;
            public Px ground0, ground1, edge, road0, road1, tuft, deco0, deco1;
            public bool pines, lava;
        }

        static LaneTheme Theme(int t)
        {
            switch (((t % LaneThemes) + LaneThemes) % LaneThemes)
            {
                case 1: // Намрын ой
                    return new LaneTheme
                    {
                        sky = new[] { H(0xe0805a), H(0xf2a865) },
                        far = H(0xa0563f), farRim = H(0xc8734a), near = H(0x7a3a2e), nearRim = H(0xb8542f),
                        ground0 = H(0x8b7a3a), ground1 = H(0x7a6a32), edge = H(0xb39a4a),
                        road0 = H(0x6e5038), road1 = H(0x60442f), tuft = H(0x5e5028), deco0 = Palette.Orange, deco1 = Palette.Red
                    };
                case 2: // Цастай нуруу
                    return new LaneTheme
                    {
                        sky = new[] { H(0x9cc6e8), H(0xd6e8f2) },
                        far = H(0x8fa8c8), farRim = H(0xe6f0fa), near = H(0x2f5a5a), nearRim = H(0x3e7270),
                        ground0 = H(0xdfe8f2), ground1 = H(0xc8d6e6), edge = H(0xffffff),
                        road0 = H(0xa9b6cc), road1 = H(0x98a6bd), tuft = H(0xb0c0d6), deco0 = Palette.Ice, deco1 = Palette.White,
                        pines = true
                    };
                case 3: // Галт уул
                    return new LaneTheme
                    {
                        sky = new[] { H(0x3a1830), H(0x8a2a2a) },
                        far = H(0x4a2236), farRim = H(0x8a3a3a), near = H(0x2a1a2a), nearRim = H(0x5a2a30),
                        ground0 = H(0x3e3242), ground1 = H(0x332838), edge = H(0x5e4a5e),
                        road0 = H(0x4e3c48), road1 = H(0x45343f), tuft = H(0x2a2030), deco0 = Palette.Orange, deco1 = Palette.Gold,
                        lava = true
                    };
                default: // Нуга
                    return new LaneTheme
                    {
                        sky = new[] { H(0xf0a46a), H(0xf8cf8a) },
                        far = H(0x6f8a5a), farRim = H(0x93a86a), near = H(0x2f6a3e), nearRim = H(0x4a8a48),
                        ground0 = H(0x5e9e48), ground1 = H(0x528f40), edge = H(0x8fc85a),
                        road0 = H(0xa07c52), road1 = H(0x8e6c47), tuft = H(0x3f7a38), deco0 = Palette.Gold, deco1 = Palette.White
                    };
            }
        }

        /// <summary>Нэг тоглогчийн зам: дээд талд байгаль, доор газар + зам. flag = эзний өнгө.</summary>
        public static PixelCanvas Lane(int theme, Px flag, int seed)
        {
            var th = Theme(theme);
            int w = LaneW, h = LaneH;
            var c = new PixelCanvas(w, h);
            int skyH = 24, groundY = 24;
            c.VGradient(0, 0, w, skyH, th.sky);
            Mountains(c, 14, 10, 30f, seed + 1, th.far, th.farRim);
            if (th.pines) Pines(c, groundY, th.near, th.nearRim, seed);
            else Mountains(c, groundY - 1, 9, 9f, seed + 2, th.near, th.nearRim);

            // Газар (2 өнгийн noise dither)
            for (int y = groundY; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float n = Noise2(x / 9f, y / 5f, seed + 3);
                    c.Set(x, y, n > 0.35f + 0.3f * PixelCanvas.Dither(x, y) ? th.ground0 : th.ground1);
                }
            c.HLine(0, w - 1, groundY, th.edge);

            // Зам: доод хагаст, ирмэг нь долгионтой
            int roadTop = 50, roadBot = 84;
            for (int x = 0; x < w; x++)
            {
                int t0 = roadTop + (int)(Noise1(x / 11f, seed + 4) * 4) - 2;
                int t1 = roadBot + (int)(Noise1(x / 13f, seed + 5) * 4) - 2;
                for (int y = t0; y < t1 && y < h; y++)
                {
                    float n = Noise2(x / 6f, y / 3f, seed + 6);
                    c.Set(x, y, n > 0.5f ? th.road0 : th.road1);
                }
                c.Set(x, t0, th.road0.Shade(1.12f));
                c.Set(x, t1, th.road1.Shade(0.8f));
            }

            var rng = new Rng((uint)(seed * 977 + 5));
            // Хайрга замын дотор
            for (int i = 0; i < 30; i++)
            {
                int x = rng.Range(0, w), y = rng.Range(roadTop + 2, roadBot - 2);
                c.Set(x, y, th.road1.Shade(0.75f));
                c.Set(x + 1, y, th.road0.Shade(1.15f));
            }
            // Өвс, цэцэг замын гадна
            for (int i = 0; i < 70; i++)
            {
                int x = rng.Range(0, w);
                int y = rng.Chance(0.6f) ? rng.Range(groundY + 4, roadTop - 3) : rng.Range(roadBot + 4, h);
                if (rng.Chance(0.7f)) Tuft(c, x, y, th.tuft);
                else
                {
                    c.Set(x, y, rng.Chance(0.5f) ? th.deco0 : th.deco1);
                    c.Set(x, y + 1, th.tuft);
                }
            }
            if (th.lava)
            {
                for (int i = 0; i < 9; i++)
                {
                    int x = rng.Range(10, w - 10);
                    int y = rng.Chance(0.5f) ? rng.Range(groundY + 4, roadTop - 4) : rng.Range(roadBot + 3, h - 2);
                    int len = rng.Range(4, 10);
                    for (int k = 0; k < len; k++)
                    {
                        c.Set(x + k, y + (int)(Noise1(k / 2f, i) * 3), k % 3 == 0 ? Palette.Gold : Palette.Orange);
                    }
                }
            }

            // Баатрын хуаран: туг + дугуй чулуун тавцан (хавтан чулуу, руны цагираг)
            int bx = 8;
            StonePlatform(c, bx + 10, 69, 13, 7, flag, seed);
            c.VLine(bx + 1, 36, 64, Palette.LeatherDark);
            c.VLine(bx + 2, 36, 64, Palette.Leather);
            c.FillRect(bx + 3, 37, 9, 7, flag);
            c.FillRect(bx + 3, 44, 7, 2, flag.Shade(0.7f));
            c.FillRect(bx + 3, 46, 4, 1, flag.Shade(0.7f));
            c.Set(bx + 7, 40, Palette.Gold);
            c.Set(bx + 6, 40, Palette.Gold);
            c.Set(bx + 7, 39, Palette.Gold);
            c.Set(bx + 7, 41, Palette.Gold);
            c.Set(bx + 8, 40, Palette.Gold);
            c.Set(bx + 1, 35, Palette.Gold);
            c.Set(bx + 2, 35, Palette.Gold);
            return c;
        }

        /// <summary>Зууван чулуун тавцан: хажуу хана, хавтан чулууны заадас, гэрэлтэй ирмэг, руны цагираг.</summary>
        static void StonePlatform(PixelCanvas c, int cx, int cy, int rx, int ry, Px rune, int seed)
        {
            const int thick = 3;
            bool Inside(int x, int y)
            {
                float dx = (x - cx + 0.5f) / rx, dy = (y - cy + 0.5f) / ry;
                return dx * dx + dy * dy <= 1f;
            }
            // Газарт буусан сүүдэр
            for (int y = cy - ry; y <= cy + ry + thick + 2; y++)
                for (int x = cx - rx - 2; x <= cx + rx + 2; x++)
                {
                    float dx = (x - cx + 0.5f) / (rx + 2f), dy = (y - cy - thick - 0.5f) / (ry + 1f);
                    if (dx * dx + dy * dy <= 1f) c.Blend(x, y, Palette.Ink.A(90));
                }
            // Хажуу хана (тавцангийн зузаан)
            for (int y = cy - ry; y <= cy + ry + thick; y++)
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    bool side = false;
                    for (int k = 1; k <= thick; k++)
                        if (Inside(x, y - k)) { side = true; break; }
                    if (!side || Inside(x, y)) continue;
                    bool block = ((x - cx + rx) / 4) % 2 == 0;
                    c.Set(x, y, Inside(x, y - 1) ? (block ? Palette.Slate1 : Palette.Slate1.Shade(0.9f)) : Palette.Slate0);
                }
            // Дээд гадаргуу: хавтан чулуу
            for (int y = cy - ry; y <= cy + ry; y++)
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (!Inside(x, y)) continue;
                    int row = (y - (cy - ry)) / 3;
                    int off = row % 2 == 0 ? 0 : 3;
                    bool hseam = (y - (cy - ry)) % 3 == 0 && y != cy - ry;
                    bool vseam = (x - (cx - rx) + off) % 6 == 0;
                    Px col = Palette.Slate2;
                    if (Noise2(x / 3f, y / 2f, seed + 21) > 0.62f) col = Palette.Slate2.Shade(0.9f);
                    if (hseam || vseam) col = Palette.Slate1;
                    if (!Inside(x, y - 1)) col = Palette.Slate2.Shade(1.18f);              // дээд гэрэл
                    else if (!Inside(x - 1, y) || !Inside(x + 1, y)) col = Palette.Slate1; // хажуу ирмэг
                    c.Set(x, y, col);
                }
            // Руны цагираг (эзний өнгөөр)
            int rrx = rx - 5, rry = ry - 3;
            for (int i = 0; i < 48; i++)
            {
                double a = i * Math.PI * 2 / 48;
                int x = cx + (int)Math.Round(Math.Cos(a) * rrx), y = cy + (int)Math.Round(Math.Sin(a) * rry);
                c.Set(x, y, i % 6 == 0 ? Palette.Gold : rune.Shade(0.85f));
            }
        }

        static void Pines(PixelCanvas c, int groundY, Px col, Px rim, int seed)
        {
            var rng = new Rng((uint)(seed * 31 + 9));
            for (int x = -4; x < c.w + 4; x += rng.Range(5, 9))
            {
                int hgt = rng.Range(9, 16);
                for (int k = 0; k < hgt; k++)
                {
                    int half = (k * 4) / hgt + (k % 3 == 0 ? 0 : 1);
                    int y = groundY - hgt + k;
                    c.HLine(x - half, x + half, y, col);
                    c.Set(x - half, y, rim);
                }
            }
        }

        // ---------------- Цэсний арын хээ ----------------

        /// <summary>Цэсний дэлгэцүүдийн бараан, руны хээтэй дэвсгэр (давтагдана).</summary>
        public static PixelCanvas MenuPattern()
        {
            var c = new PixelCanvas(24, 24);
            c.Fill(Palette.UiNight);
            var line = H(0x1d1838);
            for (int i = 0; i < 24; i++)
            {
                c.Set(i, i, line);
                c.Set(23 - i, i, line);
            }
            c.Set(12, 12, H(0x2c2552));
            c.Set(0, 0, H(0x2c2552));
            return c;
        }

        // ---------------- UI 9-slice хүрээ ----------------

        /// <summary>Булангуудыг тайрсан (дугуйрсан) дүрс эсэх.</summary>
        static bool CornerCut(int x, int y, int w, int h, int r)
        {
            int dx = x < r ? r - x : x >= w - r ? x - (w - 1 - r) : 0;
            int dy = y < r ? r - y : y >= h - r ? y - (h - 1 - r) : 0;
            return dx + dy > r;
        }

        /// <summary>Үндсэн самбар: Ink хүрээ, алтан ирмэг, бараан ягаан дотор.</summary>
        public static NineSlice Panel()
        {
            int w = 20, h = 20;
            var c = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (CornerCut(x, y, w, h, 2)) continue;
                    c.Set(x, y, Palette.Ink);
                }
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    if (CornerCut(x - 1, y - 1, w - 2, h - 2, 1)) continue;
                    c.Set(x, y, y <= 2 ? Palette.Gold : Palette.GoldDark);
                }
            for (int y = 3; y < h - 3; y++)
                for (int x = 3; x < w - 3; x++)
                    c.Set(x, y, Palette.Ink);
            for (int y = 4; y < h - 4; y++)
                for (int x = 4; x < w - 4; x++)
                    c.Set(x, y, y == 4 ? Palette.UiPanelHi : Palette.UiPanel);
            return new NineSlice(c, 7, 7, 7, 7);
        }

        /// <summary>Дотогшоо хонхойсон бараан хайрцаг (жагсаалт, мэдээлэл).</summary>
        public static NineSlice Inset()
        {
            int w = 12, h = 12;
            var c = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (CornerCut(x, y, w, h, 1)) continue;
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    c.Set(x, y, edge ? Palette.Ink : y == 1 ? H(0x110d22) : Palette.UiPanelLo);
                }
            for (int x = 2; x < w - 2; x++) c.Set(x, h - 2, H(0x2a2346));
            return new NineSlice(c, 4, 4, 4, 4);
        }

        /// <summary>Товч: base өнгөөр, дээд гэрэл, доод 3px сүүдэр.</summary>
        public static NineSlice Button(Px baseCol)
        {
            int w = 16, h = 18;
            var c = new PixelCanvas(w, h);
            Px light = baseCol.Shade(1.35f), dark = baseCol.Shade(0.62f), darker = baseCol.Shade(0.45f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (CornerCut(x, y, w, h, 2)) continue;
                    c.Set(x, y, Palette.Ink);
                }
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    if (CornerCut(x - 1, y - 1, w - 2, h - 2, 1)) continue;
                    Px col = y >= h - 4 ? (y == h - 2 ? darker : dark) : y <= 2 ? light : baseCol;
                    c.Set(x, y, col);
                }
            // Гялбаа
            c.Set(3, 3, Palette.White);
            c.Set(4, 3, light);
            c.Set(3, 4, light);
            return new NineSlice(c, 6, 6, 6, 7);
        }

        /// <summary>Картын хүрээ: ховор байдлын өнгөтэй 2px ирмэг.</summary>
        public static NineSlice Card(Px rim)
        {
            int w = 22, h = 26;
            var c = new PixelCanvas(w, h);
            Px rimLight = rim.Shade(1.4f), rimDark = rim.Shade(0.6f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (CornerCut(x, y, w, h, 2)) continue;
                    c.Set(x, y, Palette.Ink);
                }
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    if (CornerCut(x - 1, y - 1, w - 2, h - 2, 1)) continue;
                    c.Set(x, y, y <= 1 || x <= 1 ? rimLight : y >= h - 3 || x >= w - 3 ? rimDark : rim);
                }
            for (int y = 3; y < h - 3; y++)
                for (int x = 3; x < w - 3; x++)
                    c.Set(x, y, Palette.Ink);
            for (int y = 4; y < h - 4; y++)
                for (int x = 4; x < w - 4; x++)
                    c.Set(x, y, y < 6 ? H(0x3a3060) : H(0x2a2244));
            return new NineSlice(c, 7, 7, 7, 7);
        }

        /// <summary>Жижиг шошго (тоо, нэр): бараан дугуйрсан тэгш өнцөгт.</summary>
        public static NineSlice Chip()
        {
            int w = 10, h = 10;
            var c = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (CornerCut(x, y, w, h, 2)) continue;
                    bool edge = CornerCut(x - 1, y - 1, w - 2, h - 2, 1) || x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    c.Set(x, y, edge ? Palette.Ink : y == 1 ? H(0x3a3060) : H(0x231c3c));
                }
            return new NineSlice(c, 4, 4, 4, 4);
        }

        /// <summary>Цагаан дүүргэлт (өнгөөр будна): дээд гэрэл, доод сүүдэр.</summary>
        public static NineSlice BarFill()
        {
            var c = new PixelCanvas(6, 6);
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 6; x++)
                    c.Set(x, y, y == 0 ? Px.Hex(0xffffff) : y >= 4 ? Px.Hex(0xb0b0b0) : Px.Hex(0xe6e6e6));
            return new NineSlice(c, 1, 2, 1, 2);
        }

        public static NineSlice BarBack()
        {
            var c = new PixelCanvas(8, 8);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    if (CornerCut(x, y, 8, 8, 1)) continue;
                    bool edge = x == 0 || y == 0 || x == 7 || y == 7;
                    c.Set(x, y, edge ? Palette.Ink : y == 1 ? H(0x0c0918) : H(0x1c1630));
                }
            return new NineSlice(c, 3, 3, 3, 3);
        }

        /// <summary>Цагаан пиксел дугуй (бөмбөлөг, тэмдэг, тоосонцор).</summary>
        public static PixelCanvas Circle(int d)
        {
            var c = new PixelCanvas(d, d);
            c.Disc(d / 2f, d / 2f, d / 2f, Px.Hex(0xffffff));
            return c;
        }

        /// <summary>Дүрсийн доорх сүүдэр.</summary>
        public static PixelCanvas Shadow()
        {
            var c = new PixelCanvas(14, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 14; x++)
                {
                    float dx = (x + 0.5f - 7f) / 7f, dy = (y + 0.5f - 2f) / 2f;
                    if (dx * dx + dy * dy <= 1f) c.Set(x, y, Palette.Ink.A(110));
                }
            return c;
        }

        /// <summary>Зөөлөн гэрэл (radial alpha) — сонгосон карт, ялалтын туяа.</summary>
        public static PixelCanvas Glow(int d)
        {
            var c = new PixelCanvas(d, d);
            float r = d / 2f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float t = 1f - (float)Math.Sqrt(dx * dx + dy * dy) / r;
                    if (t > 0f) c.Set(x, y, Px.Hex(0xffffff, (byte)(255 * t * t)));
                }
            return c;
        }

        /// <summary>Туяа (ялалтын дэлгэц): эргэлддэг 12 цацраг.</summary>
        public static PixelCanvas Rays(int d)
        {
            var c = new PixelCanvas(d, d);
            float r = d / 2f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy) / r;
                    if (dist > 1f) continue;
                    double ang = Math.Atan2(dy, dx);
                    double s = Math.Sin(ang * 6);
                    if (s > 0.35) c.Set(x, y, Px.Hex(0xffffff, (byte)(150 * (1f - dist))));
                }
            return c;
        }

        // ---------------- Лигийн тэмдэг ----------------

        /// <summary>Лигийн бамбай: tier 0..5 (Bronze..Mythic).</summary>
        public static PixelCanvas Emblem(int tier)
        {
            int w = 26, h = 30;
            var c = new PixelCanvas(w, h);
            Px col = Palette.Tier(tier), light = col.Shade(1.4f), dark = col.Shade(0.6f);
            int cx = w / 2;
            // Далавч (Platinum+)
            if (tier >= 3)
            {
                for (int i = 0; i < 5; i++)
                {
                    c.HLine(0 + i, 4, 8 + i * 3, light);
                    c.HLine(w - 5, w - 1 - i, 8 + i * 3, light);
                    c.HLine(1 + i, 4, 9 + i * 3, dark);
                    c.HLine(w - 5, w - 2 - i, 9 + i * 3, dark);
                }
            }
            // Бамбай
            for (int y = 3; y < h - 1; y++)
            {
                int half;
                if (y < 18) half = 9;
                else half = Math.Max(0, 9 - (y - 18) * 9 / 10);
                for (int x = cx - half; x < cx + half; x++)
                {
                    Px p = x < cx ? (y < 8 ? light : col) : (y < 8 ? col : dark);
                    c.Set(x, y, p);
                }
            }
            // Дотор хүрээ
            for (int y = 6; y < h - 4; y++)
            {
                int half = y < 18 ? 6 : Math.Max(0, 6 - (y - 18) * 6 / 8);
                c.Set(cx - half, y, dark);
                c.Set(cx + half - 1, y, light);
            }
            // Голын эрдэнэ
            Px gem = tier >= 4 ? Palette.Magenta : tier >= 2 ? Palette.Red : Palette.Blue;
            c.FillRect(cx - 2, 11, 4, 5, gem);
            c.Set(cx - 1, 10, gem); c.Set(cx, 10, gem);
            c.Set(cx - 1, 16, gem.Shade(0.6f)); c.Set(cx, 16, gem.Shade(0.6f));
            c.Set(cx - 1, 12, Palette.White);
            // Титэм (Mythic)
            if (tier >= 5)
            {
                c.FillRect(cx - 5, 1, 10, 3, Palette.Gold);
                c.Set(cx - 5, 0, Palette.Gold); c.Set(cx, 0, Palette.Gold); c.Set(cx - 1, 0, Palette.Gold); c.Set(cx + 4, 0, Palette.Gold);
            }
            // Одод (шат доторх түвшин биш, зөвхөн чимэг)
            for (int i = 0; i <= Math.Min(tier, 2); i++)
                c.Set(cx - 2 + i * 2 - (Math.Min(tier, 2)) + 1, 20, Palette.White);
            return c.Outlined(Palette.Ink);
        }

        /// <summary>Эрдэнийн багц (дэлгүүр): count ширхэг эрдэнийг овоолно.</summary>
        public static PixelCanvas GemPile(int count)
        {
            var gem = SpriteLib.Get("ui_gem");
            int w = 30, h = 22;
            var c = new PixelCanvas(w, h);
            int[][] spots =
            {
                new[] { 10, 8 }, new[] { 3, 11 }, new[] { 17, 11 }, new[] { 10, 13 }, new[] { 6, 4 }, new[] { 15, 4 }
            };
            for (int i = 0; i < Math.Min(count, spots.Length); i++)
                c.Blit(gem, spots[i][0], spots[i][1]);
            return c;
        }

        /// <summary>"Зар хасах" тэмдэг: зарын дүрс + улаан хөндлөн зураас.</summary>
        public static PixelCanvas NoAds()
        {
            var ad = SpriteLib.Get("ui_ad");
            var c = new PixelCanvas(ad.w + 2, ad.h + 2);
            c.Blit(ad, 1, 1);
            for (int i = 0; i < Math.Min(c.w, c.h); i++)
            {
                c.Set(i, c.h - 1 - i, Palette.Ink);
                c.Set(i + 1, c.h - 1 - i, Palette.Red);
                c.Set(i + 2, c.h - 1 - i, Palette.Red);
                c.Set(i + 3, c.h - 1 - i, Palette.Ink);
            }
            return c;
        }
    }
}
