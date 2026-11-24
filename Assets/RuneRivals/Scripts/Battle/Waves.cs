using System.Collections.Generic;

namespace RuneRivals
{
    public struct WaveEntry
    {
        public UnitDef def;
        public float delay, y;
    }

    /// <summary>
    /// Долгион үүсгэгч. Хоёр талд яг ижил долгион ирнэ (шударга байдлын тулд).
    /// 5 дахь долгион бүрт босс (Ember Drake) гарна.
    /// </summary>
    public static class Waves
    {
        static float Weight(UnitDef d, int wave)
        {
            float w;
            switch (d.id)
            {
                case "slime": w = 3f; break;
                case "goblin": w = 4f; break;
                case "bat": w = 2.5f; break;
                case "bonearcher": w = 3f; break;
                case "wolf": w = 3f; break;
                case "imp": w = 2f; break;
                case "shaman": w = 1.5f; break;
                case "ogre": w = 1.5f; break;
                case "golem": w = 1f; break;
                default: w = 1f; break;
            }
            // Шинээр гарч ирсэн төрлийг эхний 2 долгионд илүү олон гаргана
            if (wave - d.minWave < 2) w *= 1.8f;
            return w;
        }

        public static List<WaveEntry> Build(int wave, Rng rng)
        {
            var picks = new List<UnitDef>();
            int budget = Balance.WaveBudget(wave);
            bool boss = wave % 5 == 0;
            if (boss) budget = budget * 2 / 3;

            var options = new List<UnitDef>();
            foreach (var d in UnitDefs.Monsters)
                if (d.cost > 0 && d.minWave <= wave) options.Add(d);

            var cands = new List<UnitDef>();
            var weights = new List<float>();
            int guard = 0;
            while (budget > 0 && guard++ < 100)
            {
                cands.Clear();
                weights.Clear();
                float total = 0f;
                foreach (var d in options)
                {
                    if (d.cost > budget) continue;
                    float w = Weight(d, wave);
                    cands.Add(d);
                    weights.Add(w);
                    total += w;
                }
                if (cands.Count == 0) break;
                float r = rng.Value * total;
                int pick = cands.Count - 1;
                for (int i = 0; i < cands.Count; i++)
                {
                    r -= weights[i];
                    if (r <= 0f) { pick = i; break; }
                }
                picks.Add(cands[pick]);
                budget -= cands[pick].cost;
            }

            rng.Shuffle(picks);
            var list = new List<WaveEntry>(picks.Count + 1);
            float spread = 5.5f;
            for (int i = 0; i < picks.Count; i++)
            {
                float t = 0.3f + spread * i / System.Math.Max(1, picks.Count) + rng.Range(0f, 0.3f);
                list.Add(new WaveEntry { def = picks[i], delay = t, y = rng.Range(0.08f, 0.92f) });
            }
            if (boss) list.Add(new WaveEntry { def = UnitDefs.Drake, delay = 3f, y = 0.5f });
            return list;
        }
    }
}
