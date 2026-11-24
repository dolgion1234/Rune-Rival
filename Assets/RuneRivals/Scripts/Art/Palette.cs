using System;
using System.Collections.Generic;

namespace RuneRivals
{
    /// <summary>RGBA өнгө (Unity-гүй). Unity талд Color32 болгож хэрэглэнэ.</summary>
    public struct Px : IEquatable<Px>
    {
        public byte r, g, b, a;

        public Px(byte r, byte g, byte b, byte a = 255)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }

        public static Px Hex(uint rgb, byte a = 255) => new Px((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, a);
        public static readonly Px Clear = new Px(0, 0, 0, 0);

        public bool IsClear => a == 0;
        public Px A(byte alpha) => new Px(r, g, b, alpha);

        /// <summary>f &lt; 1 бараан, f &gt; 1 цайвар (цагаан руу).</summary>
        public Px Shade(float f)
        {
            if (f <= 1f) return new Px((byte)(r * f), (byte)(g * f), (byte)(b * f), a);
            float t = Math.Min(1f, f - 1f);
            return new Px((byte)(r + (255 - r) * t), (byte)(g + (255 - g) * t), (byte)(b + (255 - b) * t), a);
        }

        public static Px Lerp(Px x, Px y, float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return new Px((byte)(x.r + (y.r - x.r) * t), (byte)(x.g + (y.g - x.g) * t),
                (byte)(x.b + (y.b - x.b) * t), (byte)(x.a + (y.a - x.a) * t));
        }

        public bool Equals(Px o) => r == o.r && g == o.g && b == o.b && a == o.a;
        public override bool Equals(object obj) => obj is Px p && Equals(p);
        public override int GetHashCode() => (r << 24) | (g << 16) | (b << 8) | a;
    }

    /// <summary>
    /// ART BIBLE (бүх өнгө зөвхөн эндээс):
    ///   1. Үдшийн гэрэлтэй 16-bit fantasy: бүдүүн пиксел, бүх дүрс 1px хар-ягаан (Ink) хүрээтэй.
    ///   2. Дулаан алт/улбар гэрэл ба сэрүүн хөх-ягаан сүүдэр; тод өнгө зөвхөн чухал зүйлд (энерги, карт, HP).
    ///   3. Багууд: тоглогч = хөх (Ally), өрсөлдөгч = улаан (Foe). Картын ховор: саарал / хөх / ягаан.
    /// Спрайтын тэмдэгт → өнгө: доорх Map. '.' = тунгалаг.
    /// </summary>
    public static class Palette
    {
        // ---- Спрайтын өнгө ----
        public static readonly Px Ink = Px.Hex(0x1b1525);      // K хүрээ, нүд
        public static readonly Px White = Px.Hex(0xf6f1e4);    // W
        public static readonly Px Slate0 = Px.Hex(0x2c2f3d);   // d
        public static readonly Px Slate1 = Px.Hex(0x555a6e);   // g
        public static readonly Px Slate2 = Px.Hex(0x9aa0b4);   // w
        public static readonly Px Bone = Px.Hex(0xece3c6);     // X
        public static readonly Px BoneDark = Px.Hex(0xb3a582); // x
        public static readonly Px Skin = Px.Hex(0xf3c69b);     // S
        public static readonly Px SkinDark = Px.Hex(0xc98d66); // s
        public static readonly Px Lime = Px.Hex(0xb5e45a);     // L
        public static readonly Px Green = Px.Hex(0x5bab45);    // G
        public static readonly Px GreenDark = Px.Hex(0x2e6b3c);// h
        public static readonly Px Leather = Px.Hex(0x8f5b37);  // N
        public static readonly Px LeatherDark = Px.Hex(0x5b3a27); // n
        public static readonly Px GoldPale = Px.Hex(0xfff3a8); // Z
        public static readonly Px Gold = Px.Hex(0xffd24a);     // Y
        public static readonly Px GoldDark = Px.Hex(0xc68a2e); // y
        public static readonly Px Steel = Px.Hex(0xc3cfdf);    // E
        public static readonly Px SteelDark = Px.Hex(0x7a88a3);// e
        public static readonly Px Iron = Px.Hex(0x3d4459);     // F
        public static readonly Px Red = Px.Hex(0xdf4a3f);      // R
        public static readonly Px RedDark = Px.Hex(0x8e2638);  // r
        public static readonly Px Orange = Px.Hex(0xf6932f);   // O
        public static readonly Px OrangeDark = Px.Hex(0xb3562b); // o
        public static readonly Px Blue = Px.Hex(0x4a86e0);     // B
        public static readonly Px BlueDark = Px.Hex(0x2a478a); // b
        public static readonly Px Ice = Px.Hex(0x8eeaf2);      // C
        public static readonly Px IceDark = Px.Hex(0x3ca5c8); // c
        public static readonly Px Purple = Px.Hex(0x8250c0);   // V
        public static readonly Px PurpleDark = Px.Hex(0x4b2d7d); // v
        public static readonly Px Magenta = Px.Hex(0xe27fc9);  // M
        public static readonly Px Teal = Px.Hex(0x3fbfa0);     // T
        public static readonly Px TealDark = Px.Hex(0x237a6e); // t
        public static readonly Px Pink = Px.Hex(0xff9bb8);     // P
        public static readonly Px PinkDark = Px.Hex(0xb54a7a); // p

        // ---- UI өнгө ----
        public static readonly Px UiNight = Px.Hex(0x16122a);  // Хамгийн бараан дэвсгэр
        public static readonly Px UiPanel = Px.Hex(0x2b2347);  // Самбарын дотор
        public static readonly Px UiPanelHi = Px.Hex(0x43386a);
        public static readonly Px UiPanelLo = Px.Hex(0x1f1936);
        public static readonly Px UiText = Px.Hex(0xf6f1e4);
        public static readonly Px UiTextDim = Px.Hex(0xb0a6cc);
        public static readonly Px Good = Px.Hex(0x7fdc6a);
        public static readonly Px Bad = Px.Hex(0xff5f5f);
        public static readonly Px Ally = Px.Hex(0x5cb4ff);
        public static readonly Px Foe = Px.Hex(0xff6a5a);
        public static readonly Px Energy = Px.Hex(0xffd24a);
        public static readonly Px Gem = Px.Hex(0x8eeaf2);
        public static readonly Px RarityCommon = Px.Hex(0x9aa0b4);
        public static readonly Px RarityRare = Px.Hex(0x4a9af0);
        public static readonly Px RarityEpic = Px.Hex(0xb569f0);

        static readonly Dictionary<char, Px> map = new Dictionary<char, Px>
        {
            { 'K', Ink }, { 'W', White }, { 'd', Slate0 }, { 'g', Slate1 }, { 'w', Slate2 },
            { 'X', Bone }, { 'x', BoneDark }, { 'S', Skin }, { 's', SkinDark },
            { 'L', Lime }, { 'G', Green }, { 'h', GreenDark }, { 'N', Leather }, { 'n', LeatherDark },
            { 'Z', GoldPale }, { 'Y', Gold }, { 'y', GoldDark }, { 'E', Steel }, { 'e', SteelDark },
            { 'F', Iron }, { 'R', Red }, { 'r', RedDark }, { 'O', Orange }, { 'o', OrangeDark },
            { 'B', Blue }, { 'b', BlueDark }, { 'C', Ice }, { 'c', IceDark }, { 'V', Purple },
            { 'v', PurpleDark }, { 'M', Magenta }, { 'T', Teal }, { 't', TealDark }, { 'P', Pink }, { 'p', PinkDark },
        };

        public static bool TryGet(char c, out Px p) => map.TryGetValue(c, out p);

        public static Px Rarity(Rarity r) =>
            r == RuneRivals.Rarity.Epic ? RarityEpic : r == RuneRivals.Rarity.Rare ? RarityRare : RarityCommon;

        /// <summary>Картын төрлийн өнгө (картын толгой хэсэг).</summary>
        public static Px Kind(CardKind k)
        {
            switch (k)
            {
                case CardKind.Troop: return Px.Hex(0x4a86e0);
                case CardKind.Boost: return Px.Hex(0xf6932f);
                case CardKind.Hero: return Px.Hex(0x5bab45);
                case CardKind.Sabotage: return Px.Hex(0xdf4a3f);
                case CardKind.Economy: return Px.Hex(0xe0b030);
                default: return Px.Hex(0x3fbfa0);
            }
        }

        /// <summary>Лигийн шатны өнгө: Bronze, Silver, Gold, Platinum, Diamond, Mythic.</summary>
        public static Px Tier(int tier)
        {
            switch (tier)
            {
                case 0: return Px.Hex(0xc9824a);
                case 1: return Px.Hex(0xc9d1e0);
                case 2: return Px.Hex(0xffd24a);
                case 3: return Px.Hex(0x6fe0d8);
                case 4: return Px.Hex(0x9a8cff);
                default: return Px.Hex(0xff6f9a);
            }
        }
    }
}
