using System;

namespace RuneRivals
{
    /// <summary>
    /// Тулааны бүх тохируулах тоо нэг дор. Хүндрэл, хурд, энергийг эндээс өөрчилнө.
    /// </summary>
    public static class Balance
    {
        // ---- Цаг ----
        public const float Tick = 0.05f;              // Тулааны нэг алхам (20 удаа/сек)
        public const float FightDuration = 15f;       // Нэг долгионы тулааны хугацаа
        public const float PickDuration = 15f;        // Карт сонгох хугацаа
        public const float FirstPickDuration = 25f;   // Эхний ээлж (шинэ тоглогчид илүү хугацаа)
        public const int SuddenDeathTurn = 15;        // Энэ ээлжээс хойш дайснууд маш хурдан хүчирхэгжинэ

        // ---- Энерги ----
        public const int StartEnergy = 3;
        public const int MaxEnergy = 15;
        public const int RefreshCost = 1;
        public static int Income(int turn) => Math.Min(7, 3 + (turn - 1) / 3);

        // ---- Карт сонголт ----
        public const int PicksPerTurn = 1;            // Долгион бүрийн өмнө авах картын тоо
        public const int GetAllEvery = 3;             // "GET ALL" 3 долгион тутамд гарна (3, 6, 9...)
        public static bool GetAllTurn(int turn) => GetAllEvery > 0 && turn % GetAllEvery == 0;

        // ---- Талбай (0 = баатар, 100 = дайсан гарч ирэх тал) ----
        public const float HeroX = 9f;
        public const float SpawnX = 103f;
        public const float EngageZone = 24f;          // Ойрын тулаанчид хамгаалах шугамаасаа хэр хол давших
        public const int MaxAllies = 10;              // Цэрэг + элит + аварга
        public const int MaxCannons = 2;
        public const int MaxMonsters = 28;            // Нэг талбайд нэг зэрэг байх дайсны дээд тоо
        public const float SentPower = 0.8f;          // Илгээсэн дайсны хүч (долгионы дайсантай харьцуулахад)

        // ---- Түвшин ----
        public const float LevelStep = 0.07f;         // Түвшин бүр +7% HP/ATK
        public const float HeroRegenPerTurn = 0.05f;  // Ээлж бүрийн эхэнд баатар 5% эдгэнэ
        public const float BaseCrit = 0.05f;
        public const float CritMult = 2f;
        public const float MaxDef = 0.7f;
        public const float SplashRatio = 0.6f;
        public const float FreeCardChance = 0.10f;

        public static float LevelMul(int level) => 1f + LevelStep * Math.Max(0, level - 1);

        // ---- Долгион ----
        public static int WaveBudget(int wave) => 3 + (int)(1.6f * wave);

        public static float WaveHpMul(int wave)
        {
            float m = (float)Math.Pow(1.08, wave - 1);
            if (wave >= SuddenDeathTurn) m *= (float)Math.Pow(1.5, wave - SuddenDeathTurn + 1);
            return m;
        }

        public static float WaveAtkMul(int wave)
        {
            float m = (float)Math.Pow(1.05, wave - 1);
            if (wave >= SuddenDeathTurn) m *= (float)Math.Pow(1.4, wave - SuddenDeathTurn + 1);
            return m;
        }
    }
}
