using System;
using UnityEngine;

namespace RuneRivals
{
    /// <summary>Зар үзүүлэгч (жинхэнэ зарын сүлжээг холбохдоо энэ интерфэйсийг хэрэгжүүлнэ).</summary>
    public interface IAdProvider
    {
        bool RewardedReady { get; }
        bool InterstitialReady { get; }
        void ShowRewarded(string placement, Action<bool> done);
        void ShowInterstitial(string placement, Action done);
    }

    /// <summary>
    /// Зар: rewarded (сайн дураар үзээд шагнал авна) ба interstitial (тоглолтын хооронд).
    /// Одоогоор ТУРШИЛТЫН горимд: жинхэнэ зар биш, 3 секундын түр дэлгэц харуулна.
    /// Жинхэнэ зар (Unity LevelPlay) холбох алхмыг ЗААВАР.md-ээс харна уу: тэр үед зөвхөн
    /// IAdProvider-ийг хэрэгжүүлсэн класс бичээд AdsManager.Provider-т онооно.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        /// <summary>Жинхэнэ зарын сүлжээ (null бол туршилтын горим).</summary>
        public static IAdProvider Provider;

        public bool IsTestMode => Provider == null;

        void Awake()
        {
            Instance = this;
        }

        public bool RewardedReady => Provider == null || Provider.RewardedReady;

        /// <summary>Шагналтай зар. done(true) = бүрэн үзсэн, шагнал олгоно.</summary>
        public void ShowRewarded(string placement, Action<bool> done)
        {
            AudioManager.Instance?.DuckMusic(true);
            Action<bool> finish = ok =>
            {
                AudioManager.Instance?.DuckMusic(false);
                done?.Invoke(ok);
            };
            if (Provider != null)
            {
                if (!Provider.RewardedReady) { finish(false); return; }
                Provider.ShowRewarded(placement, finish);
                return;
            }
            var ui = UIManager.Instance;
            if (ui == null) { finish(true); return; }
            ui.ShowTestAd(true, placement, finish);
        }

        /// <summary>Тоглолтын дараа: хэдэн тоглолт тутам завсрын зар (No Ads авсан бол үгүй).</summary>
        public void AfterMatch(Action done)
        {
            var p = Save.P;
            p.matchesSinceAd++;
            bool due = !p.noAds && p.matches >= GameConfig.InterstitialAfterMatches &&
                       p.matchesSinceAd >= GameConfig.InterstitialEveryMatches;
            if (!due) { done?.Invoke(); return; }

            if (Provider != null)
            {
                if (!Provider.InterstitialReady) { done?.Invoke(); return; }
                p.matchesSinceAd = 0;
                Save.Flush();
                AudioManager.Instance?.DuckMusic(true);
                Provider.ShowInterstitial("match_end", () =>
                {
                    AudioManager.Instance?.DuckMusic(false);
                    done?.Invoke();
                });
                return;
            }

            // Туршилтын горим: зөвхөн Editor болон Development build-д харуулна (жинхэнэ тоглогчдод хоосон зар харуулахгүй)
            if (!(Application.isEditor || Debug.isDebugBuild) || UIManager.Instance == null) { done?.Invoke(); return; }
            p.matchesSinceAd = 0;
            Save.Flush();
            UIManager.Instance.ShowTestAd(false, "match_end", ok => done?.Invoke());
        }
    }
}
