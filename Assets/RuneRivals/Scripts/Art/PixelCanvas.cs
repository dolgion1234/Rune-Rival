using System;

namespace RuneRivals
{
    /// <summary>
    /// Энгийн RGBA зураг (Unity-гүй): pixel art-ыг кодоор зурна.
    /// (0,0) = зүүн ДЭЭД булан. Unity Texture2D болгохдоо мөрийг эргүүлнэ (ArtCache).
    /// </summary>
    public sealed class PixelCanvas
    {
        public readonly int w, h;
        public readonly Px[] px;

        // 4x4 Bayer: дулаан/хүйтэн өнгийн шилжилтийг "dither" хийх
        static readonly int[] Bayer4 = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        public PixelCanvas(int w, int h)
        {
            this.w = Math.Max(1, w);
            this.h = Math.Max(1, h);
            px = new Px[this.w * this.h];
        }

        public static float Dither(int x, int y) => (Bayer4[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;

        public bool In(int x, int y) => x >= 0 && y >= 0 && x < w && y < h;
        public Px Get(int x, int y) => In(x, y) ? px[y * w + x] : Px.Clear;

        public void Set(int x, int y, Px c)
        {
            if (In(x, y)) px[y * w + x] = c;
        }

        /// <summary>Alpha-тай давхарлах.</summary>
        public void Blend(int x, int y, Px c)
        {
            if (!In(x, y) || c.a == 0) return;
            if (c.a == 255) { px[y * w + x] = c; return; }
            var d = px[y * w + x];
            float a = c.a / 255f, da = d.a / 255f;
            float oa = a + da * (1f - a);
            if (oa <= 0f) return;
            px[y * w + x] = new Px(
                (byte)((c.r * a + d.r * da * (1f - a)) / oa),
                (byte)((c.g * a + d.g * da * (1f - a)) / oa),
                (byte)((c.b * a + d.b * da * (1f - a)) / oa),
                (byte)(oa * 255f));
        }

        public void Fill(Px c)
        {
            for (int i = 0; i < px.Length; i++) px[i] = c;
        }

        public void FillRect(int x, int y, int rw, int rh, Px c)
        {
            for (int j = y; j < y + rh; j++)
                for (int i = x; i < x + rw; i++)
                    Set(i, j, c);
        }

        public void Rect(int x, int y, int rw, int rh, Px c)
        {
            HLine(x, x + rw - 1, y, c);
            HLine(x, x + rw - 1, y + rh - 1, c);
            VLine(x, y, y + rh - 1, c);
            VLine(x + rw - 1, y, y + rh - 1, c);
        }

        public void HLine(int x0, int x1, int y, Px c)
        {
            if (x1 < x0) { int t = x0; x0 = x1; x1 = t; }
            for (int x = x0; x <= x1; x++) Set(x, y, c);
        }

        public void VLine(int x, int y0, int y1, Px c)
        {
            if (y1 < y0) { int t = y0; y0 = y1; y1 = t; }
            for (int y = y0; y <= y1; y++) Set(x, y, c);
        }

        public void Line(int x0, int y0, int x1, int y1, Px c)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void Disc(float cx, float cy, float r, Px c)
        {
            int x0 = (int)Math.Floor(cx - r), x1 = (int)Math.Ceiling(cx + r);
            int y0 = (int)Math.Floor(cy - r), y1 = (int)Math.Ceiling(cy + r);
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r2) Set(x, y, c);
                }
        }

        public void Ring(float cx, float cy, float r, float thickness, Px c)
        {
            int x0 = (int)Math.Floor(cx - r), x1 = (int)Math.Ceiling(cx + r);
            int y0 = (int)Math.Floor(cy - r), y1 = (int)Math.Ceiling(cy + r);
            float ro = r * r, ri = (r - thickness) * (r - thickness);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy, d = dx * dx + dy * dy;
                    if (d <= ro && d > ri) Set(x, y, c);
                }
        }

        /// <summary>Босоо dither градиент: stops-ийн хооронд Bayer-ээр шилжинэ.</summary>
        public void VGradient(int x, int y, int rw, int rh, Px[] stops)
        {
            if (stops == null || stops.Length == 0) return;
            for (int j = 0; j < rh; j++)
            {
                float t = rh <= 1 ? 0f : j / (float)(rh - 1);
                float f = t * (stops.Length - 1);
                int i0 = Math.Min(stops.Length - 1, (int)f);
                int i1 = Math.Min(stops.Length - 1, i0 + 1);
                float frac = f - i0;
                for (int i = 0; i < rw; i++)
                {
                    int X = x + i, Y = y + j;
                    Set(X, Y, frac > Dither(X, Y) ? stops[i1] : stops[i0]);
                }
            }
        }

        /// <summary>Нэг өнгөөс нөгөө рүү dither-ээр шилжих (t = 0..1).</summary>
        public static Px DitherMix(Px a, Px b, float t, int x, int y) => t > Dither(x, y) ? b : a;

        public void Blit(PixelCanvas s, int dx, int dy, bool flipX = false)
        {
            for (int y = 0; y < s.h; y++)
                for (int x = 0; x < s.w; x++)
                {
                    var c = s.px[y * s.w + (flipX ? s.w - 1 - x : x)];
                    if (c.a > 0) Blend(dx + x, dy + y, c);
                }
        }

        /// <summary>Тунгалаг биш пикселийг тойруулан 1px хүрээ нэмнэ (шинэ зураг, хэмжээ +2).</summary>
        public PixelCanvas Outlined(Px c, bool diagonal = false)
        {
            var o = new PixelCanvas(w + 2, h + 2);
            o.Blit(this, 1, 1);
            for (int y = 0; y < o.h; y++)
                for (int x = 0; x < o.w; x++)
                {
                    if (o.px[y * o.w + x].a > 0) continue;
                    bool near = Solid(x - 1, y) || Solid(x + 1, y) || Solid(x, y - 1) || Solid(x, y + 1);
                    if (!near && diagonal)
                        near = Solid(x - 1, y - 1) || Solid(x + 1, y - 1) || Solid(x - 1, y + 1) || Solid(x + 1, y + 1);
                    if (near) o.px[y * o.w + x] = c;
                }
            return o;

            // Анхны зургийн координат (o-ийн x-1, y-1)
            bool Solid(int ox, int oy)
            {
                int sx = ox - 1, sy = oy - 1;
                return sx >= 0 && sy >= 0 && sx < w && sy < h && px[sy * w + sx].a > 0;
            }
        }

        public PixelCanvas FlipX()
        {
            var o = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    o.px[y * w + x] = px[y * w + (w - 1 - x)];
            return o;
        }

        /// <summary>Бүх тунгалаг биш пикселийг нэг өнгөөр (цохилтын цагаан анивчил).</summary>
        public PixelCanvas Silhouette(Px c)
        {
            var o = new PixelCanvas(w, h);
            for (int i = 0; i < px.Length; i++)
                if (px[i].a > 0) o.px[i] = c.A(px[i].a);
            return o;
        }

        public PixelCanvas Scaled(int k)
        {
            k = Math.Max(1, k);
            var o = new PixelCanvas(w * k, h * k);
            for (int y = 0; y < o.h; y++)
                for (int x = 0; x < o.w; x++)
                    o.px[y * o.w + x] = px[(y / k) * w + x / k];
            return o;
        }

        public PixelCanvas Copy()
        {
            var o = new PixelCanvas(w, h);
            Array.Copy(px, o.px, px.Length);
            return o;
        }

        /// <summary>Өнгө солих (жишээ нь өрсөлдөгчийн туг улаан болгох).</summary>
        public void Replace(Px from, Px to)
        {
            for (int i = 0; i < px.Length; i++)
                if (px[i].Equals(from)) px[i] = to;
        }
    }
}
