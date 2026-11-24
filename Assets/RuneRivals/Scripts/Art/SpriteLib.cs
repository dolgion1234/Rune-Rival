using System;
using System.Collections.Generic;

namespace RuneRivals
{
    /// <summary>
    /// SpriteData.Text доторх ASCII зургуудыг уншиж PixelCanvas болгоно (1px Ink хүрээтэй).
    /// Формат: "@нэр" мөрөөр эхэлж, дараагийн мөрүүд нь пиксел ('.' = тунгалаг), хоосон мөрөөр дуусна.
    /// '#'-ээр эхэлсэн мөр = тайлбар.
    /// </summary>
    public static class SpriteLib
    {
        static Dictionary<string, PixelCanvas> raw;
        static readonly Dictionary<string, PixelCanvas> outlined = new Dictionary<string, PixelCanvas>();

        static void Load()
        {
            if (raw != null) return;
            raw = new Dictionary<string, PixelCanvas>();
            var lines = SpriteData.Text.Replace("\r", "").Split('\n');
            string name = null;
            var rows = new List<string>();
            foreach (var line in lines)
            {
                var t = line.TrimEnd();
                if (t.StartsWith("@"))
                {
                    Flush(name, rows);
                    name = t.Substring(1).Trim();
                    rows.Clear();
                }
                else if (t.Length == 0 || t.StartsWith("#"))
                {
                    Flush(name, rows);
                    name = null;
                    rows.Clear();
                }
                else if (name != null)
                {
                    rows.Add(t);
                }
            }
            Flush(name, rows);
        }

        static void Flush(string name, List<string> rows)
        {
            if (name == null || rows.Count == 0) return;
            int w = 0;
            foreach (var r in rows) w = Math.Max(w, r.Length);
            var c = new PixelCanvas(w, rows.Count);
            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (Palette.TryGet(rows[y][x], out var p)) c.Set(x, y, p);
            raw[name] = c;
        }

        public static bool Has(string name)
        {
            Load();
            return name != null && raw.ContainsKey(name);
        }

        public static IEnumerable<string> Names
        {
            get { Load(); return raw.Keys; }
        }

        /// <summary>Хүрээгүй түүхий зураг (хуулбар биш, бүү өөрчил).</summary>
        public static PixelCanvas Raw(string name)
        {
            Load();
            return name != null && raw.TryGetValue(name, out var c) ? c : null;
        }

        /// <summary>1px Ink хүрээтэй зураг (кэшлэгдэнэ).</summary>
        public static PixelCanvas Get(string name)
        {
            if (name != null && outlined.TryGetValue(name, out var o)) return o;
            var r = Raw(name);
            if (r == null) return null;
            o = r.Outlined(Palette.Ink);
            outlined[name] = o;
            return o;
        }
    }
}
