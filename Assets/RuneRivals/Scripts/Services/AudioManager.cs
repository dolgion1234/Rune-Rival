using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuneRivals
{
    /// <summary>
    /// Бүх дуу, хөгжмийг кодоор үүсгэнэ (дууны файл шаардлагагүй).
    /// Өөрийн дуу ашиглах бол Resources/RuneRivals/Audio/&lt;нэр&gt; (жишээ нь "hit", "music_menu") файл тавина.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const int Rate = 22050;
        AudioSource sfx, music;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        AudioClip menuMusic, battleMusic;
        System.Random noise = new System.Random(7);

        void Awake()
        {
            Instance = this;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0.32f;
            BuildSfx();
        }

        // ================= Синтез =================

        float Noise() => (float)(noise.NextDouble() * 2.0 - 1.0);

        static float Env(float t, float d, float attack)
        {
            float a = Mathf.Clamp01(t / attack);
            float r = 1f - Mathf.Clamp01(t / d);
            return a * r * r;
        }

        static float Sq(float phase, float duty = 0.5f) => (phase - Mathf.Floor(phase)) < duty ? 1f : -1f;

        static float Tri(float phase)
        {
            float p = phase - Mathf.Floor(phase);
            return 4f * Mathf.Abs(p - 0.5f) - 1f;
        }

        static float Hz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        AudioClip Make(string name, float duration, Func<float, float, float> wave)
        {
            var custom = Resources.Load<AudioClip>("RuneRivals/Audio/" + name);
            if (custom != null) return custom;
            int n = Mathf.CeilToInt(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, duration), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Add(string name, float duration, Func<float, float, float> wave) => clips[name] = Make(name, duration, wave);

        void BuildSfx()
        {
            Add("click", 0.05f, (t, d) => Sq(t * 1200f, 0.3f) * Env(t, d, 0.001f) * 0.25f);
            Add("buy", 0.22f, (t, d) => Sq(t * (t < 0.08f ? 660f : 990f), 0.25f) * Env(t, d, 0.003f) * 0.3f);
            Add("coin", 0.2f, (t, d) => Sq(t * (t < 0.06f ? 988f : 1319f), 0.5f) * Env(t, d, 0.002f) * 0.22f);
            Add("error", 0.2f, (t, d) => Sq(t * 140f, 0.5f) * Env(t, d, 0.004f) * 0.3f);
            Add("refresh", 0.25f, (t, d) => Sq(t * Mathf.Lerp(400f, 1100f, t / d), 0.25f) * Env(t, d, 0.01f) * 0.22f);
            Add("hit", 0.07f, (t, d) => (Noise() * 0.7f + Sq(t * 180f) * 0.3f) * Env(t, d, 0.001f) * 0.35f);
            Add("hit_heavy", 0.16f, (t, d) => (Noise() * 0.6f + Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(160f, 60f, t / d) * t)) * Env(t, d, 0.002f) * 0.45f);
            Add("shoot", 0.09f, (t, d) => Tri(t * Mathf.Lerp(900f, 300f, t / d)) * Env(t, d, 0.001f) * 0.25f);
            Add("fire", 0.3f, (t, d) => Noise() * Env(t, d, 0.05f) * 0.3f * (0.6f + 0.4f * Mathf.Sin(t * 60f)));
            Add("frost", 0.25f, (t, d) => (Mathf.Sin(2 * Mathf.PI * 1568f * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * 2093f * t)) * Env(t, d, 0.002f) * 0.18f);
            Add("boom", 0.45f, (t, d) => (Noise() * 0.8f + Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(90f, 30f, t / d) * t)) * Env(t, d, 0.002f) * 0.55f);
            Add("death", 0.18f, (t, d) => Sq(t * Mathf.Lerp(500f, 120f, t / d), 0.5f) * Env(t, d, 0.002f) * 0.22f);
            Add("squish", 0.15f, (t, d) => (Noise() * 0.5f + Tri(t * Mathf.Lerp(300f, 90f, t / d))) * Env(t, d, 0.002f) * 0.28f);
            Add("hurt", 0.2f, (t, d) => Sq(t * Mathf.Lerp(300f, 150f, t / d), 0.4f) * Env(t, d, 0.003f) * 0.3f);
            Add("heal", 0.35f, (t, d) => Sq(t * Hz(t < 0.1f ? 72 : t < 0.2f ? 76 : 79), 0.25f) * Env(t, d, 0.005f) * 0.18f);
            Add("spawn", 0.2f, (t, d) => (Noise() * 0.4f + Tri(t * Mathf.Lerp(200f, 700f, t / d))) * Env(t, d, 0.01f) * 0.22f);
            Add("smite", 0.6f, (t, d) => (Noise() * (t < 0.08f ? 1f : 0.5f) + Mathf.Sin(2 * Mathf.PI * 55f * t) * 0.6f) * Env(t, d, 0.001f) * 0.5f);
            Add("trap", 0.18f, (t, d) => (Sq(t * 900f) * 0.5f + Noise() * 0.5f) * Env(t, d, 0.001f) * 0.3f);
            Add("whoosh", 0.25f, (t, d) => Noise() * Mathf.Sin(Mathf.PI * t / d) * 0.18f);
            Add("tick", 0.04f, (t, d) => Sq(t * 1800f) * Env(t, d, 0.001f) * 0.15f);
            Add("horn", 0.6f, (t, d) => (Sq(t * Hz(57), 0.3f) * 0.5f + Sq(t * Hz(64), 0.3f) * 0.35f) * Env(t, d, 0.04f) * 0.3f);
            Add("warn", 0.4f, (t, d) => Sq(t * (Mathf.Repeat(t, 0.2f) < 0.1f ? 880f : 660f), 0.5f) * Env(t, d, 0.005f) * 0.22f);
            Add("levelup", 0.6f, (t, d) =>
            {
                int note = t < 0.1f ? 72 : t < 0.2f ? 76 : t < 0.3f ? 79 : 84;
                return Sq(t * Hz(note), 0.25f) * Env(t, d, 0.004f) * 0.25f;
            });
            Add("win", 1.2f, (t, d) =>
            {
                int[] notes = { 67, 72, 76, 79, 84 };
                int i = Mathf.Min(notes.Length - 1, (int)(t / 0.14f));
                float v = Sq(t * Hz(notes[i]), 0.25f) * 0.6f + Tri(t * Hz(notes[i] - 12)) * 0.4f;
                return v * Env(t, d, 0.005f) * 0.3f;
            });
            Add("lose", 1.1f, (t, d) =>
            {
                int[] notes = { 67, 63, 60, 55 };
                int i = Mathf.Min(notes.Length - 1, (int)(t / 0.22f));
                return (Sq(t * Hz(notes[i]), 0.5f) * 0.5f + Tri(t * Hz(notes[i] - 12)) * 0.5f) * Env(t, d, 0.005f) * 0.28f;
            });
            Add("reward", 0.5f, (t, d) =>
            {
                float f = Hz(84 + ((int)(t / 0.05f) % 4) * 3);
                return Sq(t * f, 0.5f) * Env(t, d, 0.002f) * 0.15f;
            });
        }

        // ================= Хөгжим (chiptune) =================

        const int Hold = -2, Rest = -1;

        AudioClip Song(string name, float bpm, int[] lead, int[][] chords, bool driving)
        {
            var custom = Resources.Load<AudioClip>("RuneRivals/Audio/" + name);
            if (custom != null) return custom;

            float step = 60f / bpm / 2f;     // 8-р нот
            int steps = lead.Length;
            int n = Mathf.CeilToInt(steps * step * Rate);
            var data = new float[n];
            int stepsPerBar = 8;
            float leadPhase = 0f, bassPhase = 0f, padPhase = 0f;
            int curLead = Rest;
            float leadStart = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int s = Mathf.Min(steps - 1, (int)(t / step));
                float ts = t - s * step;
                int bar = s / stepsPerBar;
                var chord = chords[bar % chords.Length];

                // Гол аялгуу
                int ln = lead[s];
                if (ln >= 0 && ts < 1f / Rate * 1.5f) { curLead = ln; leadStart = t; }
                else if (ln == Rest) curLead = Rest;
                float v = 0f;
                if (curLead >= 0)
                {
                    leadPhase += Hz(curLead) / Rate;
                    float lt = t - leadStart;
                    float vib = 1f + 0.004f * Mathf.Sin(lt * 30f);
                    v += Sq(leadPhase * vib, 0.25f) * 0.16f * Mathf.Clamp01(1f - lt * 0.9f + 0.35f);
                }

                // Басс: 8-р нот бүр, 2 дахь нь октав дээш
                int root = chord[0] - 24 + ((s % 2 == 1 && driving) ? 12 : 0);
                bassPhase += Hz(root) / Rate;
                v += Tri(bassPhase) * 0.22f * Env(ts, step, 0.004f) * 1.4f;

                // Арпеджио (дэвсгэр)
                int arp = chord[(int)(t / (step / 2f)) % chord.Length];
                padPhase += Hz(arp) / Rate;
                v += Sq(padPhase, 0.125f) * 0.045f;

                // Бөмбөр
                int inBar = s % stepsPerBar;
                if (inBar == 0 || inBar == 4 || (driving && inBar == 6 && ts < step * 0.5f))
                    v += Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(120f, 40f, Mathf.Clamp01(ts / 0.12f)) * ts) * Env(ts, 0.14f, 0.002f) * 0.5f;
                if (inBar == 2 || inBar == 6)
                    v += Noise() * Env(ts, 0.1f, 0.001f) * 0.22f;
                if (driving || inBar % 2 == 1)
                    v += Noise() * Env(ts, 0.03f, 0.001f) * 0.07f;

                data[i] = Mathf.Clamp(v, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        AudioClip MenuSong()
        {
            const int H = Hold, R = Rest;
            int[] lead =
            {
                69, H, 72, 76, H, 74, 72, 71,
                69, H, 65, 69, 72, H, 69, 67,
                67, H, 72, 76, 79, H, 76, 74,
                74, H, 71, 67, 71, 74, H, R,
                76, H, 81, 79, 76, H, 72, 74,
                72, H, 69, 72, 77, H, 76, 74,
                74, H, 77, 76, 74, 72, 69, H,
                71, H, 68, 71, 76, H, 74, 71,
            };
            int[][] chords =
            {
                new[] { 69, 72, 76 }, new[] { 65, 69, 72 }, new[] { 72, 76, 79 }, new[] { 67, 71, 74 },
                new[] { 69, 72, 76 }, new[] { 65, 69, 72 }, new[] { 62, 65, 69 }, new[] { 64, 68, 71 },
            };
            return Song("music_menu", 104f, lead, chords, false);
        }

        AudioClip BattleSong()
        {
            const int H = Hold, R = Rest;
            int[] lead =
            {
                74, 74, 77, 74, 81, H, 79, 77,
                70, 70, 74, 77, 79, H, 77, 74,
                72, 72, 76, 79, 84, H, 81, 79,
                81, H, 79, 77, 76, H, 73, R,
                74, 77, 81, 77, 86, H, 84, 81,
                82, H, 81, 79, 77, 79, 77, 74,
                72, 76, 79, 76, 81, 79, 76, 72,
                73, H, 76, 79, 81, H, R, R,
            };
            int[][] chords =
            {
                new[] { 62, 65, 69 }, new[] { 58, 62, 65 }, new[] { 60, 64, 67 }, new[] { 57, 61, 64 },
                new[] { 62, 65, 69 }, new[] { 58, 62, 65 }, new[] { 60, 64, 67 }, new[] { 57, 61, 64 },
            };
            return Song("music_battle", 138f, lead, chords, true);
        }

        public void PlayMusic(bool battle)
        {
            if (battle && battleMusic == null) battleMusic = BattleSong();
            if (!battle && menuMusic == null) menuMusic = MenuSong();
            var clip = battle ? battleMusic : menuMusic;
            if (music.clip != clip)
            {
                music.clip = clip;
                music.time = 0f;
            }
            if (Save.MusicOn)
            {
                if (!music.isPlaying) music.Play();
            }
            else music.Stop();
        }

        public void RefreshMusic()
        {
            if (Save.MusicOn) { if (music.clip != null && !music.isPlaying) music.Play(); }
            else music.Stop();
        }

        public void DuckMusic(bool duck) => music.volume = duck ? 0.08f : 0.32f;

        // ================= Тоглуулах =================

        /// <summary>minGap: ижил дууг хэт олон удаа давтахгүй (олон цохилт нэг дор).</summary>
        public void Play(string name, float volume = 1f, float pitch = 1f, float minGap = 0.04f)
        {
            if (!Save.SoundOn) return;
            if (!clips.TryGetValue(name, out var clip) || clip == null) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(name, out float last) && now - last < minGap) return;
            lastPlayed[name] = now;
            sfx.pitch = pitch;
            sfx.PlayOneShot(clip, volume);
        }

        public void Click() => Play("click", 0.8f);
        public void Coin() => Play("coin", 0.8f, UnityEngine.Random.Range(0.95f, 1.1f));
        public void Error() => Play("error", 0.8f);
    }
}
