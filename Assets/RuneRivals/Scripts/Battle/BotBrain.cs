using System;

namespace RuneRivals
{
    /// <summary>
    /// Өрсөлдөгч бот: картын үнэ цэнийг нөхцөл байдлаас хамааруулж тооцоод хамгийн ашигтайг авна.
    /// skill: 0 (санамсаргүйвтэр) .. 1 (ухаалаг).
    /// </summary>
    public sealed class BotBrain
    {
        public float skill;
        public float getAllChance = 0.35f;   // Хүн шиг "Get All" дарах магадлал
        readonly Rng rng;

        public BotBrain(float skill, uint seed)
        {
            this.skill = Math.Max(0f, Math.Min(1f, skill));
            rng = new Rng(seed);
        }

        /// <summary>
        /// Нэг үйлдэл хийнэ (карт авах / refresh / бэлэн). true = дахиад үйлдэл хийх боломжтой.
        /// </summary>
        public bool Step(Match m, int sideIndex)
        {
            var s = m.sides[sideIndex];
            if (m.phase != Phase.Pick || s.ready) return false;

            // Get All (зар): 3 долгион тутамд, үлдсэн картууд үнэтэй үед (хүн шиг заримдаа л дарна)
            if (s.CanGetAll && rng.Chance(getAllChance))
            {
                float sum = 0f;
                int left = 0;
                foreach (var o in s.offer)
                    if (!o.sold) { sum += Value(m, s, o.card); left++; }
                if (left >= 2 && sum > 6f && m.GetAll(sideIndex)) return true;
            }

            // Ээлжид 1 карт: авсан бол бэлэн болно (GET ALL-ийн ээлжид дахиад боломж байж магадгүй)
            if (!s.CanPick)
            {
                if (s.CanGetAll && rng.Chance(0.5f)) return true;
                m.Ready(sideIndex);
                return false;
            }

            int best = -1;
            // Энерги их хуримтлагдсан бол үнэтэй картыг ч авна
            float bestNet = 0.15f - 0.3f * Math.Max(0, s.energy - 6);
            for (int i = 0; i < s.offer.Count; i++)
            {
                var o = s.offer[i];
                if (o.sold || o.price > s.energy) continue;
                float net = Value(m, s, o.card) - o.price;
                if (o.price == 0) net += 1f;
                if (net > bestNet) { bestNet = net; best = i; }
            }

            if (best >= 0)
            {
                m.Buy(sideIndex, best);
                return true;
            }

            // Сайн карт алга, энерги хангалттай бол дахин сугалах
            if (s.refreshes < (s.energy >= 9 ? 3 : 2) && s.energy >= Balance.RefreshCost + 3)
            {
                bool hasGood = false;
                foreach (var o in s.offer)
                    if (!o.sold && Value(m, s, o.card) >= o.card.cost + 0.5f) hasGood = true;
                if (!hasGood && m.Refresh(sideIndex)) return true;
            }

            m.Ready(sideIndex);
            return false;
        }

        /// <summary>Картын үнэ цэнэ (энергийн нэгжээр).</summary>
        public float Value(Match m, Side s, CardDef c)
        {
            float v = c.value;
            int allies = s.AllyCount;
            int ranged = s.CountAllies(u => u.IsTroop && u.def.ranged);
            float heroHp = s.lane.hero.HpFrac;
            int turn = m.turn;

            switch (c.id)
            {
                case "footman":
                case "bowman":
                case "recruits":
                case "guardian":
                case "ranger":
                case "champion":
                    v *= allies < 3 ? 1.5f : allies < 6 ? 1.1f : 0.75f;
                    if (c.id == "bowman" || c.id == "ranger") v *= 1f + 0.05f * s.mods.multishot * 4f;
                    break;
                case "promotion":
                    v *= Math.Min(3, s.CountAllies(u => u.Role == UnitRole.Soldier)) * 0.35f + 0.3f;
                    break;
                case "whetstone":
                case "rations":
                    v *= 0.3f + 0.16f * allies;
                    break;
                case "drums":
                case "shieldwall":
                case "bloodlust":
                case "laststand":
                    v *= 0.5f + 0.11f * allies;
                    break;
                case "volley":
                    v *= 0.3f + 0.35f * ranged;
                    break;
                case "mend":
                    v *= heroHp < 0.35f ? 2.8f : heroHp < 0.65f ? 1.4f : 0.3f;
                    break;
                case "fortify":
                    v *= heroHp < 0.5f ? 1.2f : 1f;
                    break;
                case "bloodprice":
                    v *= heroHp > 0.75f ? 1.2f : heroHp > 0.5f ? 0.6f : 0.05f;
                    break;
                case "goldmine":
                case "bounty":
                    v *= turn <= 4 ? 1.4f : turn <= 8 ? 0.9f : 0.35f;
                    break;
                case "poison":
                    v *= turn <= 6 ? 1.3f : 0.85f;
                    break;
                case "siphon":
                    v *= turn <= 10 ? 1f : 0.7f;
                    break;
                case "smite":
                    v *= turn >= 5 ? 1.2f : 0.85f;
                    break;
                case "raid":
                case "batswarm":
                case "ogre":
                    v *= turn <= 3 ? 0.6f : turn <= 6 ? 0.85f : 1.05f;
                    break;
            }

            // Ухаалаг бус бот илүү санамсаргүй сонгоно
            float noise = (rng.Value - 0.5f) * 2f * (1f - skill) * 0.7f;
            return v * (1f + noise);
        }
    }
}
