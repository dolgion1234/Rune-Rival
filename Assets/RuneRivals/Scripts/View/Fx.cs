using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneRivals
{
    /// <summary>
    /// Пиксел тоосонцор, хөвөх тоо, гэрлийн багана, цагираг, аянга. Бүгд дахин ашиглагдана (pool).
    /// </summary>
    public sealed class Fx
    {
        const int MaxParticles = 220, MaxTexts = 24;

        sealed class Part
        {
            public Image img;
            public Vector2 pos, vel;
            public float life, max, size, gravity;
            public Color color;
            public bool active;
        }

        sealed class FloatText
        {
            public Text text;
            public Vector2 pos;
            public float t, life;
            public bool active, important;
        }

        sealed class Shape
        {
            public Image img;
            public float t, life;
            public int kind;          // 0 багана, 1 цагираг, 2 аянга, 3 гэрэл
            public Vector2 size;
            public Color color;
        }

        readonly RectTransform layer, textLayer;
        readonly List<Part> parts = new List<Part>();
        readonly List<FloatText> texts = new List<FloatText>();
        readonly List<Shape> shapes = new List<Shape>();
        int nextPart, nextText;

        public Fx(RectTransform layer, RectTransform textLayer)
        {
            this.layer = layer;
            this.textLayer = textLayer;
        }

        // ---------------- Тоосонцор ----------------

        public void Particle(Vector2 pos, Vector2 vel, Color color, float size, float life, float gravity = 500f)
        {
            Part p;
            if (parts.Count < MaxParticles)
            {
                p = new Part();
                p.img = UIKit.Img(layer, ArtCache.White, color, "P");
                p.img.rectTransform.anchorMin = p.img.rectTransform.anchorMax = Vector2.zero;
                p.img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                parts.Add(p);
            }
            else
            {
                p = parts[nextPart];
                nextPart = (nextPart + 1) % parts.Count;
            }
            p.active = true;
            p.pos = pos;
            p.vel = vel;
            p.life = p.max = life;
            p.size = Mathf.Max(2f, Mathf.Round(size));
            p.gravity = gravity;
            p.color = color;
            p.img.gameObject.SetActive(true);
            p.img.color = color;
            p.img.rectTransform.sizeDelta = new Vector2(p.size, p.size);
            p.img.rectTransform.anchoredPosition = pos;
        }

        public void Burst(Vector2 pos, Color color, int count, float size, float speed, float gravity = 500f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float s = speed * Random.Range(0.35f, 1f);
                var vel = new Vector2(Mathf.Cos(a) * s, Mathf.Sin(a) * s + speed * 0.4f);
                var c = Color.Lerp(color, Color.white, Random.Range(0f, 0.25f));
                Particle(pos, vel, c, size * Random.Range(0.7f, 1.3f), Random.Range(0.35f, 0.7f), gravity);
            }
        }

        // ---------------- Хөвөх текст ----------------

        public void Text(Vector2 pos, string s, Color color, int size, bool important)
        {
            FloatText ft = null;
            if (texts.Count < MaxTexts)
            {
                ft = new FloatText();
                ft.text = UIKit.Outlined(textLayer, s, size, color, "T");
                ft.text.rectTransform.anchorMin = ft.text.rectTransform.anchorMax = Vector2.zero;
                ft.text.rectTransform.sizeDelta = new Vector2(400, 100);
                texts.Add(ft);
            }
            else
            {
                // Чухал бус хуучин текстийг дахин ашиглана
                for (int i = 0; i < texts.Count; i++)
                {
                    var c = texts[(nextText + i) % texts.Count];
                    if (!c.active || !c.important || important) { ft = c; nextText = (nextText + i + 1) % texts.Count; break; }
                }
                if (ft == null) return;
            }
            ft.active = true;
            ft.important = important;
            ft.pos = pos + new Vector2(Random.Range(-12f, 12f), 0);
            ft.t = 0f;
            ft.life = important ? 1.05f : 0.7f;
            ft.text.text = s;
            ft.text.fontSize = size;
            ft.text.color = color;
            ft.text.gameObject.SetActive(true);
            ft.text.transform.SetAsLastSibling();
        }

        // ---------------- Дүрс эффект ----------------

        Shape NewShape(int kind, Sprite sprite, Color color, float life)
        {
            var s = new Shape { kind = kind, life = life, color = color };
            s.img = UIKit.Img(layer, sprite, color, "Shape");
            s.img.rectTransform.anchorMin = s.img.rectTransform.anchorMax = Vector2.zero;
            shapes.Add(s);
            return s;
        }

        /// <summary>Дуудах үеийн гэрлийн багана.</summary>
        public void Beam(Vector2 pos, Color color, float scale)
        {
            var s = NewShape(0, ArtCache.White, color, 0.5f);
            s.size = new Vector2(10f * scale, 70f * scale);
            s.img.rectTransform.pivot = new Vector2(0.5f, 0f);
            s.img.rectTransform.anchoredPosition = pos;
            s.img.rectTransform.sizeDelta = s.size;
        }

        /// <summary>Тэсрэлтийн цагираг.</summary>
        public void Ring(Vector2 pos, float diameter, Color color)
        {
            var s = NewShape(1, ArtCache.Ring, color, 0.35f);
            s.size = new Vector2(diameter, diameter * 0.5f);
            s.img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            s.img.rectTransform.anchoredPosition = pos;
            s.img.rectTransform.sizeDelta = s.size * 0.3f;
        }

        /// <summary>Тэнгэрээс буух аянга.</summary>
        public void Bolt(Vector2 pos, float laneHeight, float scale)
        {
            var s = NewShape(2, ArtCache.White, new Color(0.85f, 0.97f, 1f), 0.35f);
            s.size = new Vector2(7f * scale, laneHeight - pos.y + 40f);
            s.img.rectTransform.pivot = new Vector2(0.5f, 0f);
            s.img.rectTransform.anchoredPosition = pos;
            s.img.rectTransform.sizeDelta = s.size;
            var g = NewShape(3, ArtCache.Glow, new Color(0.8f, 0.95f, 1f), 0.45f);
            g.size = new Vector2(70f * scale, 40f * scale);
            g.img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            g.img.rectTransform.anchoredPosition = pos + new Vector2(0, 6f * scale);
            g.img.rectTransform.sizeDelta = g.size;
        }

        // ---------------- Шинэчлэх ----------------

        public void Update(float dt)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (!p.active) continue;
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.active = false;
                    p.img.gameObject.SetActive(false);
                    continue;
                }
                p.vel.y -= p.gravity * dt;
                p.vel *= 1f - Mathf.Min(1f, 1.5f * dt);
                p.pos += p.vel * dt;
                float k = p.life / p.max;
                float sz = Mathf.Max(2f, Mathf.Round(p.size * (0.4f + 0.6f * k)));
                p.img.rectTransform.sizeDelta = new Vector2(sz, sz);
                p.img.rectTransform.anchoredPosition = new Vector2(Mathf.Round(p.pos.x), Mathf.Round(p.pos.y));
                var c = p.color;
                c.a = Mathf.Clamp01(k * 1.6f);
                p.img.color = c;
            }

            for (int i = 0; i < texts.Count; i++)
            {
                var t = texts[i];
                if (!t.active) continue;
                t.t += dt;
                if (t.t >= t.life)
                {
                    t.active = false;
                    t.text.gameObject.SetActive(false);
                    continue;
                }
                float k = t.t / t.life;
                float rise = (1f - (1f - k) * (1f - k)) * (t.important ? 80f : 55f);
                t.text.rectTransform.anchoredPosition = t.pos + new Vector2(0, rise);
                float s = t.t < 0.12f ? Mathf.Lerp(1.5f, 1f, t.t / 0.12f) : 1f;
                t.text.rectTransform.localScale = new Vector3(s, s, 1f);
                var c = t.text.color;
                c.a = k > 0.65f ? 1f - (k - 0.65f) / 0.35f : 1f;
                t.text.color = c;
            }

            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                var s = shapes[i];
                s.t += dt;
                float k = s.t / s.life;
                if (k >= 1f)
                {
                    Object.Destroy(s.img.gameObject);
                    shapes.RemoveAt(i);
                    continue;
                }
                var c = s.color;
                switch (s.kind)
                {
                    case 0:
                        s.img.rectTransform.sizeDelta = new Vector2(s.size.x * (1f - k), s.size.y * (0.6f + 0.4f * k));
                        c.a = 0.8f * (1f - k);
                        break;
                    case 1:
                        s.img.rectTransform.sizeDelta = s.size * (0.3f + 0.7f * Mathf.Sqrt(k));
                        c.a = 1f - k;
                        break;
                    case 2:
                        s.img.rectTransform.sizeDelta = new Vector2(s.size.x * (1f - k * 0.7f), s.size.y);
                        c.a = (Mathf.Sin(s.t * 60f) > -0.3f ? 1f : 0.3f) * (1f - k);
                        break;
                    default:
                        s.img.rectTransform.sizeDelta = s.size * (0.6f + 0.6f * k);
                        c.a = 0.9f * (1f - k);
                        break;
                }
                s.img.color = c;
            }
        }

        public void Clear()
        {
            foreach (var p in parts) { p.active = false; p.img.gameObject.SetActive(false); }
            foreach (var t in texts) { t.active = false; t.text.gameObject.SetActive(false); }
            foreach (var s in shapes) Object.Destroy(s.img.gameObject);
            shapes.Clear();
        }
    }
}
