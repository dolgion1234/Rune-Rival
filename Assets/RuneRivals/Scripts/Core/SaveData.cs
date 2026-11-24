using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuneRivals
{
    /// <summary>Тоглогчийн бүх хадгалагдах өгөгдөл (JSON болж PlayerPrefs-д хадгалагдана).</summary>
    [Serializable]
    public class Profile
    {
        public int version = 1;
        public string playerName = "";
        public int gold = GameConfig.StartGold;
        public int gems = GameConfig.StartGems;

        public int trophies, bestTrophies, highestTier;
        public int wins, losses, matches, winStreak, bestStreak;

        public string selectedHero = "archer";
        public List<string> ownedHeroes = new List<string> { "archer" };
        public List<string> heroLevelIds = new List<string>();
        public List<int> heroLevelValues = new List<int>();
        public int troopLevel = 1;
        public List<string> starters = new List<string> { "footman" };

        public bool noAds, starterPack;
        public bool soundOn = true, musicOn = true, vibrationOn = true;
        public int tutorialStep;              // 0 = эхлээгүй, 99 = дууссан
        public int freeRewardsToday;
        public string freeRewardsDay = "";
        public int matchesSinceAd;
        public List<string> processedTx = new List<string>();
    }

    /// <summary>Хадгалалт: Save.P-ээр уншиж, өөрчилсний дараа Save.Flush().</summary>
    public static class Save
    {
        const string Key = "rr_profile_v1";
        static Profile p;

        /// <summary>Мөнгө, баатар, тохиргоо өөрчлөгдөхөд (UI шинэчлэх).</summary>
        public static event Action Changed;

        public static Profile P
        {
            get
            {
                if (p == null) Load();
                return p;
            }
        }

        static void Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { p = JsonUtility.FromJson<Profile>(json); }
                catch (Exception e) { Debug.LogWarning("[Save] Corrupt save, starting fresh: " + e.Message); }
            }
            if (p == null) p = new Profile();
            if (string.IsNullOrEmpty(p.playerName)) p.playerName = "Player" + UnityEngine.Random.Range(1000, 10000);
            if (p.ownedHeroes == null) p.ownedHeroes = new List<string>();
            if (!p.ownedHeroes.Contains("archer")) p.ownedHeroes.Add("archer");
            if (p.heroLevelIds == null) p.heroLevelIds = new List<string>();
            if (p.heroLevelValues == null) p.heroLevelValues = new List<int>();
            if (p.starters == null) p.starters = new List<string>();
            if (p.processedTx == null) p.processedTx = new List<string>();
            if (!p.ownedHeroes.Contains(p.selectedHero)) p.selectedHero = "archer";
            p.troopLevel = Mathf.Clamp(p.troopLevel, 1, Progression.MaxLevel);
        }

        public static void Flush()
        {
            if (p == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(p));
            PlayerPrefs.Save();
        }

        /// <summary>Хадгалаад UI-д мэдэгдэнэ.</summary>
        public static void Commit()
        {
            Flush();
            Changed?.Invoke();
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            p = null;
            Changed?.Invoke();
        }

        // ---------------- Мөнгө ----------------

        public static void AddGold(int n)
        {
            P.gold = Math.Max(0, P.gold + n);
            Commit();
        }

        public static void AddGems(int n)
        {
            P.gems = Math.Max(0, P.gems + n);
            Commit();
        }

        public static bool SpendGold(int n)
        {
            if (n < 0 || P.gold < n) return false;
            P.gold -= n;
            Commit();
            return true;
        }

        public static bool SpendGems(int n)
        {
            if (n < 0 || P.gems < n) return false;
            P.gems -= n;
            Commit();
            return true;
        }

        // ---------------- Баатар ----------------

        public static bool OwnsHero(string id) => P.ownedHeroes.Contains(id);

        public static void UnlockHero(string id)
        {
            if (!P.ownedHeroes.Contains(id)) P.ownedHeroes.Add(id);
            Commit();
        }

        public static int HeroLevel(string id)
        {
            int i = P.heroLevelIds.IndexOf(id);
            return i >= 0 && i < P.heroLevelValues.Count ? Mathf.Clamp(P.heroLevelValues[i], 1, Progression.MaxLevel) : 1;
        }

        public static void SetHeroLevel(string id, int level)
        {
            int i = P.heroLevelIds.IndexOf(id);
            if (i < 0 || i >= P.heroLevelValues.Count)
            {
                P.heroLevelIds.Add(id);
                P.heroLevelValues.Add(level);
            }
            else P.heroLevelValues[i] = level;
            Commit();
        }

        public static HeroDef SelectedHero => Heroes.Get(P.selectedHero);

        // ---------------- Тохиргоо ----------------

        public static bool SoundOn => P.soundOn;
        public static bool MusicOn => P.musicOn;
        public static bool VibrationOn => P.vibrationOn;

        // ---------------- Худалдан авалт ----------------

        public static bool IsTransactionProcessed(string key) => !string.IsNullOrEmpty(key) && P.processedTx.Contains(key);

        public static void MarkTransaction(string key)
        {
            if (string.IsNullOrEmpty(key) || P.processedTx.Contains(key)) return;
            P.processedTx.Add(key);
            if (P.processedTx.Count > 200) P.processedTx.RemoveAt(0);
        }

        // ---------------- Өдрийн үнэгүй шагнал ----------------

        static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        public static int FreeRewardsLeft
        {
            get
            {
                if (P.freeRewardsDay != Today) return GameConfig.FreeRewardsPerDay;
                return Math.Max(0, GameConfig.FreeRewardsPerDay - P.freeRewardsToday);
            }
        }

        public static void UseFreeReward()
        {
            if (P.freeRewardsDay != Today)
            {
                P.freeRewardsDay = Today;
                P.freeRewardsToday = 0;
            }
            P.freeRewardsToday++;
            Commit();
        }
    }
}
