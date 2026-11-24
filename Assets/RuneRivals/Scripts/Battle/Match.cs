using System.Collections.Generic;

namespace RuneRivals
{
    public enum Phase { Pick, Fight, Over }

    /// <summary>
    /// Нэг тоглолт: 2 тал, ээлж бүр = карт сонгох → тулаан.
    /// Баатар нь үхсэн тал хожигдоно. sides[0] = тоглогч (доод), sides[1] = өрсөлдөгч (дээд).
    /// </summary>
    public sealed class Match
    {
        public readonly Side[] sides = new Side[2];
        public readonly List<BattleEvent> events = new List<BattleEvent>(256);
        public readonly Rng rng, waveRng;
        public Phase phase = Phase.Pick;
        public int turn;
        public float timer;       // Карт сонгох үлдсэн хугацаа
        public float fightTime;
        public int winner = -1;
        public bool recordEvents = true;
        public float levelScale = 1f;   // Долгионыг 2 талын дундаж түвшинд тааруулна

        public Side Player => sides[0];
        public Side Rival => sides[1];

        public Match(SideSetup a, SideSetup b, uint seed)
        {
            rng = new Rng(seed);
            waveRng = new Rng(seed * 7919u + 17u);
            sides[0] = new Side(this, 0, a, new Rng(seed * 31u + 1u));
            sides[1] = new Side(this, 1, b, new Rng(seed * 131u + 7u));
            sides[0].rival = sides[1];
            sides[1].rival = sides[0];
            foreach (var s in sides) s.lane.CreateHero();
            levelScale = (Balance.LevelMul(a.heroLevel) + Balance.LevelMul(a.troopLevel) +
                          Balance.LevelMul(b.heroLevel) + Balance.LevelMul(b.troopLevel)) / 4f;

            // Armory-гийн эхлэх картууд
            for (int i = 0; i < 2; i++)
            {
                var setup = i == 0 ? a : b;
                if (setup.starters == null) continue;
                foreach (var c in setup.starters)
                    if (c != null) sides[i].ApplyCard(c);
            }
        }

        public void Emit(BattleEvent e)
        {
            if (recordEvents) events.Add(e);
        }

        public void Begin() => StartTurn();

        // ---------------- Ээлж ----------------

        void StartTurn()
        {
            turn++;
            phase = Phase.Pick;
            timer = turn == 1 ? Balance.FirstPickDuration : Balance.PickDuration;
            foreach (var s in sides)
            {
                if (turn == 1) s.energy = System.Math.Min(Balance.MaxEnergy, Balance.StartEnergy + s.mods.goldMine);
                else
                {
                    int gain = s.Income + s.siphon - s.drain;
                    if (s.drain > 0) Emit(new BattleEvent { type = Ev.Energy, side = s.index, value = -s.drain, text = "siphon" });
                    s.siphon = s.drain = 0;
                    s.AddEnergy(gain);
                    s.HealHero(Balance.HeroRegenPerTurn);
                }
                s.ready = false;
                s.refreshes = 0;
                s.pickedThisTurn = 0;
                s.usedGetAll = false;
                s.DrawOffer();
            }
            Emit(new BattleEvent { type = Ev.TurnStart, value = turn });
        }

        void StartFight()
        {
            phase = Phase.Fight;
            fightTime = 0f;
            var wave = Waves.Build(turn, waveRng);
            foreach (var s in sides) s.lane.BeginFight(wave, turn);
            Emit(new BattleEvent { type = Ev.FightStart, value = turn });
            CheckWinner();
        }

        /// <summary>Карт сонгох үе: хугацаа дуусах эсвэл 2 тал бэлэн болоход тулаан эхэлнэ.</summary>
        public void TickPick(float dt)
        {
            if (phase != Phase.Pick) return;
            timer -= dt;
            if (timer <= 0f || (sides[0].ready && sides[1].ready)) StartFight();
        }

        /// <summary>Тулааны нэг алхам (Balance.Tick).</summary>
        public void TickFight(float dt)
        {
            if (phase != Phase.Fight) return;
            fightTime += dt;
            sides[0].lane.Tick(dt);
            sides[1].lane.Tick(dt);
            CheckWinner();
            if (phase == Phase.Fight && fightTime >= Balance.FightDuration) StartTurn();
        }

        public void ForceFight()
        {
            if (phase == Phase.Pick) StartFight();
        }

        internal void OnHeroDied(Side s)
        {
            // Хоёр тал нэг алхамд үхэж болох тул CheckWinner-т шийднэ
        }

        void CheckWinner()
        {
            if (phase == Phase.Over) return;
            bool d0 = !sides[0].lane.hero.alive, d1 = !sides[1].lane.hero.alive;
            if (!d0 && !d1) return;
            if (d0 && d1) winner = sides[0].lane.hero.hp >= sides[1].lane.hero.hp ? 0 : 1;
            else winner = d0 ? 1 : 0;
            phase = Phase.Over;
            Emit(new BattleEvent { type = Ev.Winner, side = winner });
        }

        // ---------------- Тоглогчийн үйлдэл ----------------

        public bool CanBuy(int side, int slot)
        {
            var s = sides[side];
            if (phase != Phase.Pick || s.ready || !s.CanPick || slot < 0 || slot >= s.offer.Count) return false;
            var o = s.offer[slot];
            return !o.sold && s.energy >= o.price;
        }

        public bool Buy(int side, int slot)
        {
            if (!CanBuy(side, slot)) return false;
            var s = sides[side];
            var o = s.offer[slot];
            s.energy -= o.price;
            o.sold = true;
            s.pickedThisTurn++;
            s.ApplyCard(o.card);
            Emit(new BattleEvent { type = Ev.CardBought, side = side, card = o.card, value = slot });
            return true;
        }

        public bool Refresh(int side)
        {
            var s = sides[side];
            if (phase != Phase.Pick || s.ready || !s.CanPick || s.energy < Balance.RefreshCost) return false;
            s.energy -= Balance.RefreshCost;
            s.refreshes++;
            s.DrawOffer();
            Emit(new BattleEvent { type = Ev.Refresh, side = side });
            return true;
        }

        /// <summary>Зар үзээд үлдсэн бүх картыг авах (3 долгион тутамд, Balance.GetAllTurn).</summary>
        public bool GetAll(int side)
        {
            var s = sides[side];
            if (phase != Phase.Pick || s.ready || !s.CanGetAll) return false;
            bool any = false;
            for (int i = 0; i < s.offer.Count; i++)
            {
                var o = s.offer[i];
                if (o.sold) continue;
                o.sold = true;
                s.ApplyCard(o.card);
                any = true;
                Emit(new BattleEvent { type = Ev.CardBought, side = side, card = o.card, value = i });
            }
            if (!any) return false;
            s.usedGetAll = true;
            s.pickedThisTurn = Balance.PicksPerTurn;
            Emit(new BattleEvent { type = Ev.GetAll, side = side });
            return true;
        }

        public void Ready(int side)
        {
            if (phase == Phase.Pick) sides[side].ready = true;
        }
    }
}
