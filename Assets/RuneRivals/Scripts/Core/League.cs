using System;

namespace RuneRivals
{
    /// <summary>
    /// Ranked лиг: цомоор (trophy) шат ахина. Шат бүр шинэ карт, эхлэх картын слот, шагнал өгнө.
    /// Нэг шатанд хүрсэн бол түүнээс доош буухгүй (шатны доод хязгаар хамгаална).
    /// </summary>
    public static class League
    {
        public static readonly string[] Names = { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Mythic" };
        public static readonly int[] Thresholds = { 0, 300, 800, 1500, 2400, 3500 };
        public static readonly int[] RewardGems = { 0, 50, 100, 150, 250, 500 };
        public static readonly int[] RewardGold = { 0, 500, 1000, 2000, 3000, 5000 };
        public static readonly int[] StarterSlots = { 1, 1, 2, 2, 3, 3 };

        public static int Count => Names.Length;

        public static int TierOf(int trophies)
        {
            int t = 0;
            for (int i = 0; i < Thresholds.Length; i++)
                if (trophies >= Thresholds[i]) t = i;
            return t;
        }

        /// <summary>Картын нээгдэх шат (Cards.unlockTier нь 0..3).</summary>
        public static int CardTier(int tier) => Math.Min(3, tier);

        public static int Floor(int tier) => Thresholds[Math.Max(0, Math.Min(tier, Thresholds.Length - 1))];

        /// <summary>Одоогийн шат доторх ахиц 0..1 (сүүлийн шатанд 1).</summary>
        public static float Progress(int trophies)
        {
            int t = TierOf(trophies);
            if (t >= Thresholds.Length - 1) return 1f;
            return (trophies - Thresholds[t]) / (float)(Thresholds[t + 1] - Thresholds[t]);
        }

        public static int NextThreshold(int trophies)
        {
            int t = TierOf(trophies);
            return t >= Thresholds.Length - 1 ? -1 : Thresholds[t + 1];
        }

        /// <summary>Ялалтын цом: хүчтэй өрсөлдөгчийг ялбал илүү.</summary>
        public static int WinTrophies(int mine, int rival) => Math.Max(24, Math.Min(36, 30 + (rival - mine) / 15));

        /// <summary>Ялагдлын цом (Bronze-д бага хасна).</summary>
        public static int LossTrophies(int tier, int mine, int rival)
        {
            int baseLoss = tier == 0 ? 12 : 18 + tier;
            return Math.Max(8, Math.Min(30, baseLoss + (rival - mine) / -20));
        }

        public static int WinGold(int tier) => 40 + 15 * tier;
        public static int LossGold(int tier) => 12 + 4 * tier;

        /// <summary>Энэ шатанд нээгдсэн шинэ картуудын тоо.</summary>
        public static int NewCardsAt(int tier)
        {
            int ct = CardTier(tier);
            if (tier > 3) return 0;
            int n = 0;
            foreach (var c in Cards.All) if (c.unlockTier == ct) n++;
            return n;
        }
    }

    /// <summary>Баатар ба цэргийн түвшин ахиулах үнэ (алт).</summary>
    public static class Progression
    {
        public const int MaxLevel = 10;
        static readonly int[] Costs = { 0, 150, 300, 500, 800, 1200, 1700, 2400, 3200, 4200 };

        /// <summary>level → level+1 болгох үнэ; дээд түвшинд -1.</summary>
        public static int HeroCost(int level) => level >= MaxLevel || level < 1 ? -1 : Costs[level];

        public static int TroopCost(int level) => level >= MaxLevel || level < 1 ? -1 : (int)Math.Round(Costs[level] * 1.2f / 10f) * 10;
    }
}
