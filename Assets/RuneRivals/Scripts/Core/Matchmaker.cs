using System;
using System.Collections.Generic;

namespace RuneRivals
{
    /// <summary>Олдсон өрсөлдөгч (AI). Нэр нь жинхэнэ тоглогчийн нэр шиг.</summary>
    public sealed class Opponent
    {
        public string name;
        public int trophies, tier;
        public HeroDef hero;
        public int heroLevel, troopLevel;
        public float skill, statMul = 1f;
        public List<CardDef> starters = new List<CardDef>();
    }

    /// <summary>
    /// Өрсөлдөгч олох: цом, түвшинд ойролцоо AI өрсөлдөгч үүсгэнэ.
    /// Шинэ тоглогчийн эхний тоглолтууд арай хялбар (сурах үе).
    /// </summary>
    public static class Matchmaker
    {
        static readonly string[] Names =
        {
            "ShadowFox", "Temuujin", "NightOwl", "Bayaraa", "IronMaiden", "Khulan", "RuneLord", "Saruul_7",
            "DragonSlayer", "Ganbold", "PixelPaladin", "Anu", "MoonBlade", "Tuguldur", "xArcherx", "Bold",
            "Erdene", "FrostByte", "Munkh", "Zaya", "Odko", "StormRider", "Tengis", "LuckyLancer", "Nomin",
            "Batbayar", "Gerel", "Altai", "KhanWolf", "SilentArrow", "Uuganaa", "CritMaster", "Enkhjin", "SkyHawk",
            "Naraa", "BraveHeart", "Sodoo", "Orgil", "Misheel", "TumurNinja", "GoldenEagle", "Bilguun", "Delger",
            "MightyMouse", "Sarnai", "Ariunaa", "SteppeRider", "Bataa_99", "Tsetseg", "Oyunaa", "CaptainCow",
            "LunaStar", "Mendee", "Dashka", "Hulan", "Yesui", "Jargal", "Khaliun", "Lukas", "Mateo", "Aiko",
            "Sofia_R", "JinWoo", "Ravi", "EmmaK", "NoahPlays", "Kenji", "Ivan_G", "Amira", "Leo77", "Yuki",
            "Carlos", "Hana", "Omar", "Elif", "Mika", "Zane", "Sven", "Nadia", "Kai", "Bruno", "Ayla", "Tomas",
            "GrimJack", "WaffleKing", "SirLancelot", "MageMaster", "TheLastRune", "Snowball", "PotatoHero",
            "QuickShot", "Thunderbolt", "SlimeHunter", "OgreBasher", "ManaBurn", "TinyTitan", "Wanderer",
        };

        public static Opponent Find(Profile p, Rng rng)
        {
            int myTier = League.TierOf(p.trophies);
            bool learning = p.matches < 3;

            var o = new Opponent();
            o.name = Names[rng.Range(0, Names.Length)];
            if (o.name == p.playerName) o.name = Names[(Array.IndexOf(Names, o.name) + 1) % Names.Length];
            o.trophies = Math.Max(0, p.trophies + rng.Range(-40, 41));
            if (learning) o.trophies = Math.Max(0, p.trophies - rng.Range(0, 20));
            o.tier = League.TierOf(o.trophies);

            // Баатар: доод шатанд энгийн баатрууд илүү
            float r = rng.Value;
            if (learning) o.hero = Heroes.All[0];
            else if (o.tier == 0) o.hero = r < 0.55f ? Heroes.All[0] : r < 0.85f ? Heroes.All[1] : Heroes.All[2];
            else o.hero = Heroes.All[rng.Range(0, Heroes.All.Length)];

            // Түвшин нь цомоос хамаарна (тоглогч ахиулсан бол давуу тал олно)
            int lvl = 1 + o.trophies / 350;
            o.heroLevel = Clamp(lvl + rng.Range(-1, 2), 1, Progression.MaxLevel);
            o.troopLevel = Clamp(lvl + rng.Range(-1, 2), 1, Progression.MaxLevel);

            // Ухаалаг байдал: цом өсөх тусам сайжирна
            float t = Math.Min(1f, o.trophies / 3000f);
            o.skill = Math.Max(0.1f, Math.Min(0.97f, 0.35f + 0.55f * t + rng.Range(-0.1f, 0.1f)));
            if (learning)
            {
                o.skill = 0.1f + 0.1f * p.matches;
                o.statMul = 0.82f + 0.05f * p.matches;
                o.heroLevel = o.troopLevel = 1;
            }

            // Эхлэх картууд
            int slots = League.StarterSlots[o.tier];
            if (learning) slots = 0;
            var choices = new List<CardDef>();
            foreach (var c in Cards.UnlockedAt(League.CardTier(o.tier))) if (c.starter) choices.Add(c);
            rng.Shuffle(choices);
            for (int i = 0; i < slots && i < choices.Count; i++) o.starters.Add(choices[i]);
            return o;
        }

        static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;

        /// <summary>Хайлтын үеэр гүйлгэж харуулах нэрс.</summary>
        public static string RandomName(Rng rng) => Names[rng.Range(0, Names.Length)];
    }
}
