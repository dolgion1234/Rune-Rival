using System.Collections.Generic;

namespace RuneRivals
{
    /// <summary>
    /// Санамсаргүй тоо (xorshift). UnityEngine.Random-оос хамааралгүй тул тулааныг
    /// Unity-гүйгээр (тест, симуляц) ажиллуулах боломжтой, нэг seed = нэг ижил тулаан.
    /// </summary>
    public sealed class Rng
    {
        uint s;

        public Rng(uint seed)
        {
            s = seed == 0 ? 0x9E3779B9u : seed;
            for (int i = 0; i < 4; i++) NextU();
        }

        public uint NextU()
        {
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            return s;
        }

        /// <summary>[0, 1)</summary>
        public float Value => (NextU() & 0xFFFFFF) / 16777216f;

        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            return min + (int)(NextU() % (uint)(maxExclusive - min));
        }

        public float Range(float a, float b) => a + (b - a) * Value;

        public bool Chance(float p) => Value < p;

        public T Pick<T>(IList<T> list) => list[Range(0, list.Count)];

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
