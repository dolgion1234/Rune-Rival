namespace RuneRivals
{
    /// <summary>
    /// Нэг газар өөрчлөх тохиргоонууд. Google Play-д гаргахаасаа өмнө эдгээрийг шалгаарай.
    /// </summary>
    public static class GameConfig
    {
        public const string ProductName = "Rune Rivals";
        public const string CompanyName = "Rune Rivals Studio";

        // Google Play дээрх апп-ын ID. Нэг удаа гаргасны дараа ӨӨРЧЛӨХ БОЛОМЖГҮЙ.
        public const string PackageId = "com.runerivalsstudio.runerivals";

        // Нууцлалын бодлогын хуудасны холбоос (store/privacy-policy.html-ийг GitHub Pages дээр байршуулна)
        public const string PrivacyPolicyUrl = "https://example.com/runerivals/privacy";

        // ---- Эхлэх хөрөнгө ----
        public const int StartGold = 300;
        public const int StartGems = 30;

        // ---- Зар ----
        public const int InterstitialEveryMatches = 3;   // Хэдэн тоглолт тутам завсрын зар (No Ads авсан бол үгүй)
        public const int InterstitialAfterMatches = 4;   // Эхний хэдэн тоглолтод зар гаргахгүй
        public const int FreeRewardsPerDay = 5;          // Дэлгүүрийн "зар үзээд шагнал" өдрийн хязгаар
        public const int FreeRewardGems = 10;
        public const int FreeRewardGold = 150;

        // ---- In-App Purchase бүтээгдэхүүн (Google Play Console-д яг ижил ID-тай үүсгэнэ) ----
        public const string IapGemsSmall = "gems_small";       // Consumable
        public const string IapGemsMedium = "gems_medium";     // Consumable
        public const string IapGemsLarge = "gems_large";       // Consumable
        public const string IapNoAds = "no_ads";               // Non-consumable
        public const string IapStarterPack = "starter_pack";   // Non-consumable

        public const int GemsSmall = 100;
        public const int GemsMedium = 550;
        public const int GemsLarge = 1200;
        public const int StarterGems = 300;
        public const int StarterGold = 3000;
        public const string StarterHero = "pyro";

        // Store холбогдоогүй үед харуулах жишээ үнэ (жинхэнэ үнийг Google Play-ээс авна)
        public static string FallbackPrice(string id)
        {
            switch (id)
            {
                case IapGemsSmall: return "$0.99";
                case IapGemsMedium: return "$4.99";
                case IapGemsLarge: return "$9.99";
                case IapNoAds: return "$2.99";
                case IapStarterPack: return "$1.99";
                default: return "";
            }
        }

        // ---- Алтыг эрдэнээр солих (дэлгүүр) ----
        public static readonly int[] GoldPackGold = { 1000, 2800, 6500 };
        public static readonly int[] GoldPackGems = { 50, 120, 250 };
    }
}
