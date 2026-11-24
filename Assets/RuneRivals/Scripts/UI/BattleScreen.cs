using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace RuneRivals
{
    /// <summary>
    /// Тулааны дэлгэц: өрсөлдөгч хайх → VS → ээлж бүр (карт сонгох → автомат тулаан) → үр дүн.
    /// Дээд талбай = өрсөлдөгч, доод талбай = тоглогч. Баатар нь түрүүлж унасан тал хожигдоно.
    /// </summary>
    public sealed partial class BattleScreen : UiScreen
    {
        enum Stage { Idle, Search, Versus, Play, Ending, Result }

        public override bool ShowTopBar => false;
        public override bool ShowNav => false;

        Stage stage = Stage.Idle;
        Match match;
        BotBrain bot;
        Opponent opp;
        LaneView rivalLane, playerLane;
        float acc, botTimer, stageT, nameT;
        bool adBusy, paused, surrendered, resultApplied;
        int lastTickSecond = -1;
        Popup pausePopup;
        RectTransform stageRoot;
        Text searchName;
        ResultInfo result;

        sealed class ResultInfo
        {
            public bool win;
            public int trophyDelta, trophiesBefore, trophiesAfter, gold, tierBefore, tierAfter;
            public int rewardGems, rewardGold, wave;
            public bool doubled;
        }

        void StartCo(IEnumerator e)
        {
            if (ui != null && root != null && root.gameObject.activeInHierarchy) ui.StartCoroutine(e);
        }

        // ================= Эхлэл / төгсгөл =================

        public override void OnShow()
        {
            Time.timeScale = 1f;
            Layout();
            BeginSearch();
        }

        public override void OnHide()
        {
            Time.timeScale = 1f;
            CloseTip();
            ClearStage();
            ClearTickers();
            foreach (Transform c in bannerLayer) UnityEngine.Object.Destroy(c.gameObject);
            if (rivalLane != null) UnityEngine.Object.Destroy(rivalLane.root.gameObject);
            if (playerLane != null) UnityEngine.Object.Destroy(playerLane.root.gameObject);
            rivalLane = playerLane = null;
            match = null;
            stage = Stage.Idle;
            paused = false;
            pausePopup = null;
            AudioManager.Instance?.PlayMusic(false);
        }

        void ClearStage()
        {
            if (stageRoot != null) UnityEngine.Object.Destroy(stageRoot.gameObject);
            stageRoot = null;
        }

        public override bool Back()
        {
            switch (stage)
            {
                case Stage.Search:
                    ui.ShowHome();
                    return true;
                case Stage.Play:
                    if (TipActive) { CloseTip(); return true; }
                    OpenPause();
                    return true;
                case Stage.Result:
                    Continue();
                    return true;
                default:
                    return true;
            }
        }

        public void OnAppPaused()
        {
            if (stage == Stage.Play && !paused) OpenPause();
        }

        // ================= Өрсөлдөгч хайх =================

        void BeginSearch()
        {
            stage = Stage.Search;
            stageT = 0f;
            surrendered = resultApplied = adBusy = paused = false;
            result = null;
            var p = Save.P;
            opp = Matchmaker.Find(p, new Rng((uint)Random.Range(1, int.MaxValue)));

            ClearStage();
            stageRoot = UIKit.Rect("Search", overlay).Fill();
            UIKit.Blocker(stageRoot, new Color(0.05f, 0.03f, 0.1f, 0.96f));
            var title = UIKit.Label(stageRoot, "FINDING OPPONENT", 84, UIKit.TextCol, true);
            title.rectTransform.At(0.5f, 0.5f, 0, 330, 1000, 110);
            var spin = UIKit.Pix(stageRoot, "ui_swords", 4f, "Spinner");
            spin.rectTransform.At(0.5f, 0.5f, 0, 60, spin.rectTransform.sizeDelta.x, spin.rectTransform.sizeDelta.y);
            spin.gameObject.AddComponent<Spin>().speed = -220f;
            searchName = UIKit.Label(stageRoot, "", 56, UIKit.Dim, true);
            searchName.rectTransform.At(0.5f, 0.5f, 0, -170, 900, 80);
            var league = UIKit.Label(stageRoot, League.Names[League.TierOf(p.trophies)] + " League  ·  " + p.trophies + " Trophies", 44, UIKit.Gold, true);
            league.rectTransform.At(0.5f, 0.5f, 0, -260, 1000, 70);
            var cancel = UIKit.Btn(stageRoot, "Cancel", "btn_gray", 56, () => ui.ShowHome(), "Cancel");
            cancel.rectTransform().At(0.5f, 0f, 0, 220, 380, 130);
            AudioManager.Instance?.PlayMusic(true);
        }

        void BeginVersus()
        {
            stage = Stage.Versus;
            stageT = 0f;
            var p = Save.P;
            var hero = Save.SelectedHero;
            ClearStage();
            stageRoot = UIKit.Rect("Versus", overlay).Fill();
            UIKit.Blocker(stageRoot, new Color(0.05f, 0.03f, 0.1f, 0.96f));

            var top = VsSide(stageRoot, opp.hero, opp.name, opp.trophies, opp.heroLevel, false);
            var bottom = VsSide(stageRoot, hero, p.playerName, p.trophies, Save.HeroLevel(hero.id), true);
            top.anchoredPosition = new Vector2(-1200, 0);
            bottom.anchoredPosition = new Vector2(1200, 0);
            StartCo(UIKit.Move(top, Vector2.zero, 0.35f, true));
            StartCo(UIKit.Move(bottom, Vector2.zero, 0.35f, true));

            var vs = UIKit.Outlined(stageRoot, "VS", 220, UIKit.Gold, "VS");
            vs.rectTransform.At(0.5f, 0.5f, 0, 0, 500, 260);
            StartCo(DelayedPop(vs.transform, 0.3f));
            AudioManager.Instance?.Play("horn", 0.8f);
            Haptics.Medium();
        }

        IEnumerator DelayedPop(Transform t, float delay)
        {
            t.localScale = Vector3.zero;
            float x = 0f;
            while (x < delay) { x += Time.unscaledDeltaTime; yield return null; }
            AudioManager.Instance?.Play("hit_heavy", 0.8f);
            yield return UIKit.Pop(t, 2f, 0.3f);
        }

        RectTransform VsSide(Transform parent, HeroDef hero, string name, int trophies, int level, bool player)
        {
            var holder = UIKit.Rect(player ? "Player" : "Rival", parent).Fill();
            var band = UIKit.Rect("Band", holder);
            band.anchorMin = new Vector2(0, player ? 0.08f : 0.56f);
            band.anchorMax = new Vector2(1, player ? 0.44f : 0.92f);
            band.offsetMin = band.offsetMax = Vector2.zero;
            var bg = UIKit.Block(band, (player ? UIKit.AllyCol : UIKit.FoeCol) * new Color(0.45f, 0.45f, 0.45f, 0.9f), "Bg");
            bg.rectTransform.Fill();
            var stripe = UIKit.Block(band, player ? UIKit.AllyCol : UIKit.FoeCol, "Stripe");
            if (player) stripe.rectTransform.TopStrip(10, 0); else stripe.rectTransform.BottomStrip(10, 0);
            var h = UIKit.Pix(band, hero.unit.sprite, 2.2f, "Hero");
            h.rectTransform.At(player ? 0.28f : 0.72f, 0.5f, 0, 0, h.rectTransform.sizeDelta.x, h.rectTransform.sizeDelta.y);
            if (!player) h.rectTransform.localScale = new Vector3(-1, 1, 1);
            var bob = h.gameObject.AddComponent<Bob>();
            bob.amplitude = 6f;
            bob.Rebase();
            var n = UIKit.Label(band, name, 72, UIKit.TextCol, true, player ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            n.rectTransform.At(player ? 0.72f : 0.28f, 0.5f, 0, 70, 520, 90);
            var sub = UIKit.Label(band, hero.name + "  Lv " + level, 46, UIKit.Dim, true, player ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            sub.rectTransform.At(player ? 0.72f : 0.28f, 0.5f, 0, 0, 520, 60);
            var tr = UIKit.Rect("Trophies", band).At(player ? 0.72f : 0.28f, 0.5f, 0, -70, 520, 64);
            var tt = UIKit.IconValue(tr, "ui_trophy", trophies.ToString("N0"), 52, UIKit.Gold, 1f, out _);
            if (!player) tt.alignment = TextAnchor.MiddleLeft;
            return holder;
        }

        // ================= Тулаан эхлэх =================

        void BeginPlay()
        {
            ClearStage();
            stage = Stage.Play;
            var p = Save.P;
            int tier = League.TierOf(p.trophies);
            int theme = Mathf.Min(tier, ProcArt.LaneThemes - 1);

            var hero = Save.SelectedHero;
            var mine = new SideSetup
            {
                name = p.playerName,
                hero = hero,
                heroLevel = Save.HeroLevel(hero.id),
                troopLevel = p.troopLevel,
                pool = Cards.UnlockedAt(League.CardTier(tier)),
                starters = StarterCards(p, tier),
            };
            var theirs = new SideSetup
            {
                name = opp.name,
                hero = opp.hero,
                heroLevel = opp.heroLevel,
                troopLevel = opp.troopLevel,
                statMul = opp.statMul,
                pool = Cards.UnlockedAt(League.CardTier(opp.tier)),
                starters = opp.starters,
                bot = true,
            };
            uint seed = (uint)Random.Range(1, int.MaxValue);
            match = new Match(mine, theirs, seed);
            bot = new BotBrain(opp.skill, seed * 3u + 1u);

            if (rivalLane != null) UnityEngine.Object.Destroy(rivalLane.root.gameObject);
            if (playerLane != null) UnityEngine.Object.Destroy(playerLane.root.gameObject);
            rivalLane = new LaneView(laneHolder, match.Rival, false, theme);
            playerLane = new LaneView(laneHolder, match.Player, true, theme);
            Layout();
            SetupHuds();
            ClearTickers();

            acc = 0f;
            botTimer = Random.Range(2f, 4f);
            match.Begin();
            Banner("PICK YOUR CARDS", UIKit.Gold, 110, 1.0f);
        }

        static List<CardDef> StarterCards(Profile p, int tier)
        {
            var list = new List<CardDef>();
            int slots = League.StarterSlots[tier];
            int unlockTier = League.CardTier(tier);
            // Слот i = p.starters[i] (хоосон слот = ""), нээгдээгүй слотыг алгасна
            for (int i = 0; i < slots && i < p.starters.Count; i++)
            {
                var c = Cards.Get(p.starters[i]);
                if (c == null || !c.starter || c.unlockTier > unlockTier) continue;
                list.Add(c);
            }
            return list;
        }

        // ================= Кадр бүр =================

        public override void Tick(float dt)
        {
            float udt = Time.unscaledDeltaTime;
            switch (stage)
            {
                case Stage.Search:
                    stageT += udt;
                    nameT -= udt;
                    if (nameT <= 0f && searchName != null)
                    {
                        nameT = 0.11f;
                        searchName.text = Matchmaker.RandomName(new Rng((uint)Random.Range(1, int.MaxValue)));
                    }
                    if (stageT > 2.2f) BeginVersus();
                    return;
                case Stage.Versus:
                    stageT += udt;
                    if (stageT > 2.3f) BeginPlay();
                    return;
                case Stage.Play:
                case Stage.Ending:
                case Stage.Result:
                    break;
                default:
                    return;
            }
            if (match == null) return;
            if (root.rect.size != lastSize) Layout();

            if (stage == Stage.Play && !paused && !TipActive)
            {
                if (match.phase == Phase.Pick)
                {
                    if (!adBusy) match.TickPick(dt);
                    BotPick(dt);
                }
                else if (match.phase == Phase.Fight)
                {
                    acc += dt;
                    int steps = 0;
                    while (acc >= Balance.Tick && steps < 10)
                    {
                        acc -= Balance.Tick;
                        match.TickFight(Balance.Tick);
                        steps++;
                        if (match.phase != Phase.Fight) break;
                    }
                    if (steps >= 10) acc = 0f;
                }
            }

            DrainEvents();
            bool fighting = match.phase == Phase.Fight;
            float alpha = fighting ? Mathf.Clamp01(acc / Balance.Tick) : 1f;
            float viewDt = TipActive || paused ? 0f : dt;
            playerLane.Sync(alpha, viewDt, fighting);
            rivalLane.Sync(alpha, viewDt, fighting);
            UpdateHud(dt);
            TimerTicks();

            if (stage == Stage.Play)
            {
                Tutorial();
                if (match.phase == Phase.Over) BeginEnding();
            }
            else if (stage == Stage.Ending)
            {
                stageT += udt;
                if (stageT > 1.7f) ShowResult();
            }
        }

        void BotPick(float dt)
        {
            var s = match.Rival;
            if (s.ready) return;
            botTimer -= dt;
            if (botTimer > 0f) return;
            bot.Step(match, 1);
            botTimer = match.Player.ready ? Random.Range(0.2f, 0.45f) : Random.Range(0.7f, 1.7f);
        }

        void TimerTicks()
        {
            if (match.phase != Phase.Pick || stage != Stage.Play) { lastTickSecond = -1; return; }
            int s = Mathf.CeilToInt(match.timer);
            if (s <= 5 && s >= 1 && s != lastTickSecond && !match.Player.ready)
            {
                lastTickSecond = s;
                AudioManager.Instance?.Play("tick", 0.9f);
                StartCo(UIKit.Punch(timerText.transform, 0.3f, 0.2f));
            }
        }

        // ================= Үйл явдал =================

        void DrainEvents()
        {
            var evs = match.events;
            for (int i = 0; i < evs.Count; i++)
            {
                var e = evs[i];
                switch (e.type)
                {
                    case Ev.TurnStart: OnTurnStart(e); break;
                    case Ev.FightStart: OnFightStart(e); break;
                    case Ev.CardBought: OnCardBought(e); break;
                    case Ev.Refresh:
                        if (e.side == 0) { AudioManager.Instance?.Play("refresh", 0.8f); StartCo(Deal()); }
                        break;
                    case Ev.GetAll:
                        if (e.side == 0) { playerLane.Flash(0.6f); AudioManager.Instance?.Play("reward", 0.9f); }
                        else Banner(opp.name + " used GET ALL!", UIKit.FoeCol, 64, 0.8f);
                        break;
                    case Ev.Sent:
                    case Ev.Winner:
                        break;
                    case Ev.Energy:
                        if (e.text == "siphon")
                        {
                            if (e.side == 0) Banner("-" + Mathf.Abs(Mathf.RoundToInt(e.value)) + " ENERGY", UIKit.FoeCol, 90, 0.7f, "Siphoned by " + opp.name);
                        }
                        else Lane(e.side).OnEvent(e);
                        break;
                    default:
                        Lane(e.side).OnEvent(e);
                        break;
                }
            }
            evs.Clear();
        }

        LaneView Lane(int side) => side == 0 ? playerLane : rivalLane;

        void OnTurnStart(BattleEvent e)
        {
            botTimer = Random.Range(1.2f, 3f);
            StartCo(Deal());
            if (Balance.GetAllTurn(match.turn) && stage == Stage.Play)
                Banner("GET ALL READY!", UIKit.Gold, 90, 0.8f, "Take every card this wave");
            if (match.turn > 1)
            {
                int income = Balance.Income(match.turn) + match.Player.mods.goldMine;
                StartCo(FloatOverOrb("+" + income));
                AudioManager.Instance?.Play("coin", 0.7f);
            }
            if (match.turn == 2 && Save.P.tutorialStep == 3) pendingTip = 3;
        }

        void OnFightStart(BattleEvent e)
        {
            int t = match.turn;
            if (t == Balance.SuddenDeathTurn)
                Banner("SUDDEN DEATH", UIKit.BadCol, 130, 1.1f, "Monsters grow much stronger!");
            else if (t % 5 == 0)
            {
                Banner("BOSS WAVE " + t, UIKit.BadCol, 130, 1.0f, "An Ember Drake approaches!");
                AudioManager.Instance?.Play("warn", 0.9f);
            }
            else Banner("WAVE " + t, UIKit.TextCol, 140, 0.6f);
            AudioManager.Instance?.Play("horn", 0.7f);
            Haptics.Light();
            if (Save.P.tutorialStep == 2) pendingTip = 2;
        }

        void OnCardBought(BattleEvent e)
        {
            var c = e.card;
            if (c == null) return;
            if (e.side == 1)
            {
                Ticker(c);
                if (c.kind == CardKind.Sabotage)
                {
                    AudioManager.Instance?.Play("warn", 0.8f);
                    playerLane.Flash(0.3f);
                }
                return;
            }

            AudioManager.Instance?.Play("buy", 0.9f);
            Haptics.Light();
            int slot = Mathf.Clamp((int)e.value, 0, offerCards.Length - 1);
            var from = offerCards[slot].root.TransformPoint(Vector3.zero);
            StartCo(UIKit.Punch(offerCards[slot].root, 0.12f, 0.2f));

            switch (c.kind)
            {
                case CardKind.Sabotage:
                    StartCo(FlyIcon(c.art, from, rivalLane.WorldCenter, () =>
                    {
                        rivalLane.Flash(0.5f);
                        rivalLane.Shake(0.3f);
                        AudioManager.Instance?.Play("whoosh", 0.8f);
                    }));
                    break;
                case CardKind.Boost:
                    StartCo(FlyIcon(c.art, from, playerLane.WorldCenter, () => playerLane.BoostAll(new Color(1f, 0.7f, 0.3f), c.name.ToUpper() + "!")));
                    break;
                case CardKind.Hero:
                    StartCo(FlyIcon(c.art, from, playerLane.HeroWorld, () => playerLane.BoostAll(UIKit.GoodCol, c.name.ToUpper() + "!")));
                    break;
                case CardKind.Economy:
                    StartCo(FlyIcon(c.art, from, energyOrb.transform.position, () => StartCo(UIKit.Punch(energyOrb.transform, 0.3f, 0.3f))));
                    break;
                default:
                    StartCo(FlyIcon(c.art, from, playerLane.WorldCenter, null));
                    break;
            }

            if (Save.P.tutorialStep == 1) pendingTip = 1;
        }

        IEnumerator Deal()
        {
            for (int i = 0; i < offerCards.Length; i++)
            {
                var r = offerCards[i].root;
                r.localScale = Vector3.zero;
            }
            for (int i = 0; i < offerCards.Length; i++)
            {
                float t = 0f;
                while (t < 0.07f) { t += Time.unscaledDeltaTime; yield return null; }
                if (offerCards[i].root == null) yield break;
                StartCo(UIKit.Pop(offerCards[i].root, 0.2f, 0.3f));
            }
        }

        IEnumerator FloatOverOrb(string text)
        {
            var t = UIKit.Outlined(overlay, text, 70, UIKit.Gold, "Income");
            t.transform.position = energyOrb.transform.position;
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(300, 100);
            Vector2 start = rt.anchoredPosition;
            float x = 0f;
            while (x < 1f && t != null)
            {
                x += Time.unscaledDeltaTime;
                rt.anchoredPosition = start + new Vector2(0, 120f * x);
                t.color = new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 1f - Mathf.Max(0f, x - 0.6f) / 0.4f);
                yield return null;
            }
            if (t != null) UnityEngine.Object.Destroy(t.gameObject);
        }

        // ================= Тоглогчийн үйлдэл =================

        void OnCardTap(int i)
        {
            if (match == null || stage != Stage.Play || match.phase != Phase.Pick || paused) return;
            var me = match.Player;
            if (i >= me.offer.Count) return;
            var o = me.offer[i];
            if (o.sold) return;
            if (me.ready) { ui.Toast("Waiting for your rival..."); return; }
            if (!me.CanPick)
            {
                AudioManager.Instance?.Error();
                ui.Toast(me.CanGetAll ? "1 card per wave. Use GET ALL for the rest!" : "Only 1 card per wave");
                return;
            }
            if (me.energy < o.price)
            {
                AudioManager.Instance?.Error();
                ui.Toast("Not enough energy", UIKit.BadCol);
                StartCo(UIKit.Punch(energyOrb.transform, 0.3f, 0.3f));
                return;
            }
            match.Buy(0, i);
        }

        void OnRefresh()
        {
            if (match == null || match.phase != Phase.Pick) return;
            if (!match.Player.CanPick) { ui.Toast("You already picked a card this wave"); return; }
            if (!match.Refresh(0)) ui.Toast("Not enough energy", UIKit.BadCol);
        }

        void OnGetAll()
        {
            if (match == null || match.phase != Phase.Pick || adBusy || !match.Player.CanGetAll) return;
            if (AdsManager.Instance == null) return;
            adBusy = true;
            AdsManager.Instance.ShowRewarded("get_all", ok =>
            {
                adBusy = false;
                if (!ok) { ui.Toast("Video not completed"); return; }
                if (match != null && match.phase == Phase.Pick && !match.GetAll(0)) ui.Toast("No cards left");
            });
        }

        void OnFight()
        {
            if (match == null || match.phase != Phase.Pick) return;
            match.Ready(0);
            if (!match.Rival.ready) botTimer = Mathf.Min(botTimer, 0.3f);
        }

        // ================= Түр зогсоох =================

        void OpenPause()
        {
            if (stage != Stage.Play || pausePopup != null || match == null) return;
            paused = true;
            Time.timeScale = 0f;
            var p = ui.OpenPopup("Paused", 820, 860);
            pausePopup = p;
            p.onClose = () =>
            {
                paused = false;
                pausePopup = null;
                if (stage == Stage.Play) Time.timeScale = 1f;
            };
            var resume = UIKit.Btn(p.body, "Resume", "btn_green", 66, () => p.Close(), "Resume");
            resume.rectTransform().TopStrip(140, 20);
            var settings = UIKit.Btn(p.body, "Settings", "btn_blue", 60, () => ui.OpenSettings(), "Settings");
            settings.rectTransform().TopStrip(130, 190);
            var give = UIKit.Btn(p.body, "Surrender", "btn_red", 60, () =>
            {
                ui.Confirm("Surrender?", "You will lose this match and its trophies.", "Surrender", () =>
                {
                    p.Close();
                    Surrender();
                });
            }, "Surrender");
            give.rectTransform().TopStrip(130, 350);
            var hint = UIKit.Para(p.body, "Wave " + match.turn + "  ·  vs " + opp.name, 40, UIKit.Dim, TextAnchor.UpperCenter);
            hint.rectTransform.TopStrip(60, 510);
        }

        void Surrender()
        {
            if (match == null || match.phase == Phase.Over) return;
            surrendered = true;
            match.winner = 1;
            match.phase = Phase.Over;
            BeginEnding();
        }

        // ================= Сургалт =================

        int pendingTip = -1;

        void Tutorial()
        {
            var p = Save.P;
            if (p.tutorialStep >= 99 || TipActive) return;
            if (p.tutorialStep == 0 && match.phase == Phase.Pick && match.turn == 1)
            {
                ShowTip("Pick <color=#ffd24a>1 card</color> each wave (it costs ENERGY).\nEvery 3rd wave, GET ALL takes them all!", cardsRow, true);
                SetTutorial(1);
            }
            else if (pendingTip == 1 && p.tutorialStep == 1)
            {
                ShowTip("Tap <color=#7fdc6a>FIGHT!</color> when you're done.\nUnspent energy carries over to the next turn.", buttonsRow, true);
                SetTutorial(2);
            }
            else if (pendingTip == 2 && p.tutorialStep == 2)
            {
                ShowTip("Your hero and soldiers fight on their own.\nYour rival fights the SAME waves up top!", playerLane.root, true);
                SetTutorial(3);
            }
            else if (pendingTip == 3 && p.tutorialStep == 3 && match.phase == Phase.Pick)
            {
                ShowTip("<color=#ff6a5a>SABOTAGE</color> cards send monsters to your rival.\nOutlast them to win the match!", null, true);
                SetTutorial(99);
            }
            pendingTip = -1;
        }

        static void SetTutorial(int step)
        {
            Save.P.tutorialStep = step;
            Save.Flush();
        }

        // ================= Төгсгөл =================

        void BeginEnding()
        {
            if (stage != Stage.Play) return;
            stage = Stage.Ending;
            stageT = 0f;
            CloseTip();
            bool win = match.winner == 0;
            Time.timeScale = surrendered ? 1f : 0.35f;
            ApplyResult(win);
            if (win)
            {
                Banner("VICTORY!", UIKit.Gold, 190, 1.2f);
                AudioManager.Instance?.Play("win");
            }
            else
            {
                Banner(surrendered ? "SURRENDERED" : "DEFEAT", UIKit.BadCol, 170, 1.2f);
                AudioManager.Instance?.Play("lose");
            }
            Haptics.Heavy();
            (win ? rivalLane : playerLane).Shake(0.8f);
        }

        void ApplyResult(bool win)
        {
            if (resultApplied) return;
            resultApplied = true;
            var p = Save.P;
            var r = new ResultInfo { win = win, trophiesBefore = p.trophies, wave = match.turn };
            r.tierBefore = League.TierOf(p.trophies);
            int delta = win ? League.WinTrophies(p.trophies, opp.trophies) : -League.LossTrophies(r.tierBefore, p.trophies, opp.trophies);
            p.trophies = Mathf.Max(League.Floor(r.tierBefore), p.trophies + delta);
            r.trophiesAfter = p.trophies;
            r.trophyDelta = p.trophies - r.trophiesBefore;
            r.gold = win ? League.WinGold(r.tierBefore) : League.LossGold(r.tierBefore);
            p.gold += r.gold;
            p.matches++;
            if (win)
            {
                p.wins++;
                p.winStreak++;
                p.bestStreak = Mathf.Max(p.bestStreak, p.winStreak);
            }
            else
            {
                p.losses++;
                p.winStreak = 0;
            }
            p.bestTrophies = Mathf.Max(p.bestTrophies, p.trophies);
            r.tierAfter = League.TierOf(p.trophies);
            for (int t = p.highestTier + 1; t <= r.tierAfter; t++)
            {
                r.rewardGems += League.RewardGems[t];
                r.rewardGold += League.RewardGold[t];
            }
            if (r.tierAfter > p.highestTier)
            {
                p.highestTier = r.tierAfter;
                p.gems += r.rewardGems;
                p.gold += r.rewardGold;
            }
            else r.rewardGems = r.rewardGold = 0;
            if (p.tutorialStep < 99) p.tutorialStep = 99;
            Save.Commit();
            result = r;
        }

        void ShowResult()
        {
            stage = Stage.Result;
            Time.timeScale = 1f;
            var r = result;
            ClearStage();
            stageRoot = UIKit.Rect("Result", overlay).Fill();
            UIKit.Blocker(stageRoot, new Color(0.04f, 0.02f, 0.09f, 0.88f));

            if (r.win)
            {
                var rays = UIKit.Img(stageRoot, ArtCache.Rays, new Color(1f, 0.85f, 0.4f, 0.45f), "Rays");
                rays.rectTransform.At(0.5f, 0.5f, 0, 520, 1300, 1300);
                rays.gameObject.AddComponent<Spin>().speed = 12f;
            }
            var title = UIKit.Outlined(stageRoot, r.win ? "VICTORY!" : "DEFEAT", 180, r.win ? UIKit.Gold : UIKit.BadCol, "Title");
            title.rectTransform.At(0.5f, 0.5f, 0, 560, 1000, 220);
            StartCo(UIKit.Pop(title.transform, 0.4f, 0.4f));
            var heroImg = UIKit.Pix(stageRoot, match.Player.hero.unit.sprite, 2f, "Hero");
            heroImg.rectTransform.At(0.5f, 0.5f, 0, 330, heroImg.rectTransform.sizeDelta.x, heroImg.rectTransform.sizeDelta.y);
            if (!r.win) heroImg.color = new Color(0.6f, 0.6f, 0.7f);
            else heroImg.gameObject.AddComponent<Bob>().Rebase();
            string line = surrendered ? "You surrendered to " + opp.name
                : r.win ? "You outlasted " + opp.name + "!" : opp.name + " survived longer.";
            var sub = UIKit.Label(stageRoot, line + "\nReached wave " + r.wave, 46, UIKit.TextCol, false);
            sub.rectTransform.At(0.5f, 0.5f, 0, 150, 1000, 120);

            var panel = UIKit.Frame(stageRoot, "panel", "Panel").rectTransform.At(0.5f, 0.5f, 0, -150, 900, 420);
            // Цом
            var trRow = UIKit.Rect("Trophies", panel).At(0.5f, 1f, -60, -80, 600, 90);
            var trText = UIKit.IconValue(trRow, "ui_trophy", "0", 80, r.trophyDelta >= 0 ? UIKit.Gold : UIKit.BadCol, 1.6f, out _);
            StartCo(UIKit.Count(trText, 0, Mathf.Abs(r.trophyDelta), 0.8f, r.trophyDelta >= 0 ? "+" : "-", ""));
            // Алт
            var goldRow = UIKit.Rect("Gold", panel).At(0.5f, 1f, -60, -190, 600, 90);
            var goldText = UIKit.IconValue(goldRow, "ui_coin", "+0", 80, UIKit.Gold, 1.6f, out _);
            StartCo(UIKit.Count(goldText, 0, r.gold, 0.8f, "+", ""));
            resultGoldText = goldText;
            // Лиг
            int tier = r.tierAfter;
            var emblem = UIKit.Img(panel, ArtCache.Emblem(tier), Color.white, "Emblem");
            emblem.rectTransform.Place(0f, 0f, 40, 30, 28 * 3.4f, 32 * 3.4f);
            var lname = UIKit.Label(panel, League.Names[tier] + "  " + r.trophiesAfter.ToString("N0"), 44, UIKit.TextCol, true, TextAnchor.MiddleLeft);
            lname.rectTransform.Place(0f, 0f, 150, 84, 600, 60);
            var bar = UIKit.Bar(panel, UIKit.Gold, "League");
            bar.rectTransform().Place(0f, 0f, 150, 36, 680, 44);
            bar.Set(League.Progress(r.trophiesBefore), true);
            StartCo(UIKit.Delay(0.5f, () => { if (bar != null) bar.Set(League.Progress(r.trophiesAfter)); }));

            // Товчнууд
            if (r.win && AdsManager.Instance != null)
            {
                doubleBtn = UIKit.Btn(stageRoot, null, "btn_purple", 0, DoubleGold, "Double");
                doubleBtn.rectTransform().At(0.5f, 0.5f, 0, -470, 620, 140);
                var ic = UIKit.Pix(doubleBtn.transform, "ui_ad", 1.3f, "Icon");
                ic.rectTransform.anchoredPosition = new Vector2(-220, 4);
                var t = UIKit.Label(doubleBtn.transform, "x2 GOLD", 64, UIKit.TextCol, true);
                t.rectTransform.Stretch(80, 10, 10, 0);
                doubleBtn.label = t;
                StartCo(DelayedPop(doubleBtn.transform, 0.6f));
            }
            var cont = UIKit.Btn(stageRoot, "CONTINUE", "btn_green", 70, Continue, "Continue");
            cont.rectTransform().At(0.5f, 0.5f, 0, r.win ? -640 : -500, 620, 150);
            AudioManager.Instance?.Play("coin");
        }

        Text resultGoldText;
        UiButton doubleBtn;

        void DoubleGold()
        {
            if (result == null || result.doubled) return;
            AdsManager.Instance.ShowRewarded("double_gold", ok =>
            {
                if (!ok || result == null || result.doubled) return;
                result.doubled = true;
                Save.AddGold(result.gold);
                if (resultGoldText != null) StartCo(UIKit.Count(resultGoldText, result.gold, result.gold * 2, 0.6f, "+", ""));
                if (doubleBtn != null) doubleBtn.gameObject.SetActive(false);
                AudioManager.Instance?.Play("reward");
            });
        }

        void Continue()
        {
            if (stage != Stage.Result) return;
            var r = result;
            stage = Stage.Idle;
            AdsManager.Instance.AfterMatch(() =>
            {
                ui.ShowHome();
                if (r != null && r.tierAfter > r.tierBefore && r.rewardGems + r.rewardGold > 0)
                {
                    int newCards = League.NewCardsAt(r.tierAfter);
                    string line = "Welcome to " + League.Names[r.tierAfter] + " League!" +
                                  (newCards > 0 ? "\n" + newCards + " new cards unlocked" : "") +
                                  (League.StarterSlots[r.tierAfter] > League.StarterSlots[r.tierBefore] ? "\n+1 starter card slot" : "");
                    ui.RewardPopup("PROMOTED!", null, ArtCache.Emblem(r.tierAfter), line, r.rewardGold, r.rewardGems);
                }
            });
        }
    }
}
