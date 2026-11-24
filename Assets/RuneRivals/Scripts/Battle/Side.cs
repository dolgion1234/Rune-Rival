using System;
using System.Collections.Generic;

namespace RuneRivals
{
    public sealed class OfferSlot
    {
        public CardDef card;
        public int price;
        public bool sold;
        public bool IsFree => price == 0 && card.cost > 0;
    }

    /// <summary>Тулаанд оролцох нэг талын тохиргоо (тоглогч эсвэл бот).</summary>
    public sealed class SideSetup
    {
        public string name;
        public HeroDef hero;
        public int heroLevel = 1, troopLevel = 1;
        public float statMul = 1f;      // Ботын хүндрэлийн үржүүлэгч
        public List<CardDef> pool;      // Санал болгох картууд
        public List<CardDef> starters;  // Armory-гийн эхлэх картууд
        public bool bot;
    }

    /// <summary>Нэг тал: энерги, санал болгож буй картууд, сайжруулалтууд, өөрийн талбай.</summary>
    public sealed class Side
    {
        public readonly int index;
        public readonly Match match;
        public Side rival;
        public readonly Rng rng;
        public readonly Lane lane;

        public string name;
        public HeroDef hero;
        public int heroLevel, troopLevel;
        public float statMul;
        public bool isBot;

        public int energy;
        public readonly Mods mods = new Mods();
        public readonly List<OfferSlot> offer = new List<OfferSlot>();
        public readonly List<CardDef> pool;
        public readonly Dictionary<string, int> stacks = new Dictionary<string, int>();
        public readonly List<CardDef> picks = new List<CardDef>();
        public readonly List<UnitDef> incoming = new List<UnitDef>();
        public int offerSize = 3;
        public bool ready, usedGetAll;  // usedGetAll = энэ ээлжид GET ALL ашигласан
        public int pickedThisTurn;      // Энэ ээлжид авсан картын тоо (Balance.PicksPerTurn хүртэл)

        public bool CanPick => pickedThisTurn < Balance.PicksPerTurn;
        public bool CanGetAll => Balance.GetAllTurn(match.turn) && !usedGetAll;
        public int refreshes, kills, bountyKills;
        public int drain, siphon;     // Siphon картын дараагийн ээлжийн хасалт / нэмэлт

        static readonly float[,] MeleeSlots =
        {
            { 25, .28f }, { 25, .72f }, { 29, .5f }, { 21, .1f }, { 21, .9f },
            { 32, .18f }, { 32, .82f }, { 19, .5f }, { 35, .38f }, { 35, .62f }
        };

        static readonly float[,] RangedSlots =
        {
            { 15, .2f }, { 15, .8f }, { 7, .12f }, { 7, .88f }, { 17, .5f },
            { 12, .02f }, { 12, .98f }, { 4, .32f }, { 4, .68f }, { 19, .33f }
        };

        static readonly float[,] CannonSlots = { { 1.5f, .2f }, { 1.5f, .8f } };

        public Side(Match match, int index, SideSetup setup, Rng rng)
        {
            this.match = match;
            this.index = index;
            this.rng = rng;
            name = setup.name;
            hero = setup.hero ?? Heroes.All[0];
            heroLevel = Math.Max(1, setup.heroLevel);
            troopLevel = Math.Max(1, setup.troopLevel);
            statMul = setup.statMul;
            isBot = setup.bot;
            pool = setup.pool ?? Cards.UnlockedAt(0);
            lane = new Lane(match, this);
        }

        // ---------- Тоо ----------

        public int Stacks(string id) => stacks.TryGetValue(id, out int n) ? n : 0;

        public int AllyCount
        {
            get
            {
                int n = 0;
                foreach (var u in lane.units) if (u.alive && !u.enemy && u.IsTroop) n++;
                return n;
            }
        }

        public int CountAllies(Func<Unit, bool> filter)
        {
            int n = 0;
            foreach (var u in lane.units) if (u.alive && !u.enemy && filter(u)) n++;
            return n;
        }

        public int Income => Balance.Income(match.turn) + mods.goldMine;

        public void AddEnergy(int n) => energy = Math.Max(0, Math.Min(Balance.MaxEnergy, energy + n));

        // ---------- Цэрэг ----------

        public Unit Summon(UnitDef def)
        {
            float[,] table;
            if (def.role == UnitRole.Structure)
            {
                if (CountAllies(u => u.Role == UnitRole.Structure) >= Balance.MaxCannons) return null;
                table = CannonSlots;
            }
            else
            {
                if (AllyCount >= Balance.MaxAllies) return null;
                table = def.ranged ? RangedSlots : MeleeSlots;
            }

            int slot = -1;
            for (int i = 0; i < table.GetLength(0) && slot < 0; i++)
            {
                bool used = false;
                foreach (var u in lane.units)
                    if (u.alive && !u.enemy && u.slot == i && u.def.ranged == def.ranged && (u.Role == UnitRole.Structure) == (def.role == UnitRole.Structure))
                    { used = true; break; }
                if (!used) slot = i;
            }
            if (slot < 0) return null;

            var unit = new Unit
            {
                def = def,
                enemy = false,
                slot = slot,
                homeX = table[slot, 0],
                homeY = table[slot, 1],
            };
            unit.x = unit.prevX = unit.homeX;
            unit.y = unit.prevY = unit.homeY;
            Recalc(unit);
            unit.hp = unit.maxHp;
            unit.cd = rng.Range(0f, def.interval * 0.5f);
            lane.units.Add(unit);
            match.Emit(new BattleEvent { type = Ev.Spawn, side = index, unit = unit, x = unit.x, y = unit.y });
            return unit;
        }

        /// <summary>Нэг энгийн цэргийг элит болгоно.</summary>
        public void Promote()
        {
            var soldiers = new List<Unit>();
            foreach (var u in lane.units)
                if (u.alive && !u.enemy && u.Role == UnitRole.Soldier) soldiers.Add(u);
            if (soldiers.Count == 0) return;
            var s = rng.Pick(soldiers);
            float frac = s.HpFrac;
            s.def = s.def.ranged ? UnitDefs.Ranger : UnitDefs.Guardian;
            s.maxHp = 0;
            Recalc(s);
            s.hp = s.maxHp * Math.Max(frac, 0.6f);
            match.Emit(new BattleEvent { type = Ev.Promote, side = index, unit = s, x = s.x, y = s.y });
        }

        public void Recalc(Unit u)
        {
            float lvl = Balance.LevelMul(u.IsHero ? heroLevel : troopLevel) * statMul;
            float hpM = lvl, atkM = lvl;
            if (u.IsHero) { hpM *= mods.heroHp; atkM *= mods.heroAtk; }
            else if (u.IsTroop) { hpM *= mods.soldierHp; atkM *= mods.soldierAtk; }

            float newMax = u.def.hp * hpM;
            if (u.maxHp > 0 && newMax > u.maxHp) u.hp += newMax - u.maxHp;
            u.maxHp = newMax;
            if (u.hp > u.maxHp) u.hp = u.maxHp;
            u.atk = u.def.atk * atkM;
            u.armor = Math.Min(Balance.MaxDef, u.def.armor + mods.def);
            u.crit = mods.crit;
            u.lifesteal = mods.lifesteal;
            u.dodge = u.IsHero ? Math.Min(0.6f, mods.heroDodge) : 0f;
            u.multishot = u.IsTroop && u.def.ranged ? mods.multishot : 0;
        }

        public void RecalcAll()
        {
            foreach (var u in lane.units)
                if (u.alive && !u.enemy) Recalc(u);
        }

        public void HealHero(float frac)
        {
            var h = lane.hero;
            if (h == null || !h.alive) return;
            lane.Heal(h, h.maxHp * frac);
        }

        /// <summary>Өрсөлдөгч энэ талбай руу дайсан илгээнэ (дараагийн тулаанд гарч ирнэ).</summary>
        public void QueueIncoming(UnitDef def, int count, Side from)
        {
            for (int i = 0; i < count; i++) incoming.Add(def);
            match.Emit(new BattleEvent { type = Ev.Sent, side = index, value = count, text = def.name, other = null });
        }

        // ---------- Санал болгох картууд ----------

        public bool CanOffer(CardDef c)
        {
            if (c.maxStacks > 0 && Stacks(c.id) >= c.maxStacks) return false;
            if (c.summon && AllyCount >= Balance.MaxAllies) return false;
            if (c.id == "cannon" && CountAllies(u => u.Role == UnitRole.Structure) >= Balance.MaxCannons) return false;
            if (c.id == "promotion" && CountAllies(u => u.Role == UnitRole.Soldier) == 0) return false;
            if (c.id == "mend" && lane.hero != null && lane.hero.HpFrac > 0.9f) return false;
            return true;
        }

        public void DrawOffer()
        {
            offer.Clear();
            var candidates = new List<CardDef>();
            var weights = new List<float>();
            foreach (var c in pool)
            {
                if (!CanOffer(c)) continue;
                float w = c.rarity == Rarity.Common ? 10f : c.rarity == Rarity.Rare ? 6f : 3f;
                if (match.turn <= 2 && c.summon) w *= 2.5f;
                candidates.Add(c);
                weights.Add(w);
            }

            for (int k = 0; k < offerSize && candidates.Count > 0; k++)
            {
                float total = 0f;
                foreach (var w in weights) total += w;
                float r = rng.Value * total;
                int pick = 0;
                for (int i = 0; i < weights.Count; i++)
                {
                    r -= weights[i];
                    if (r <= 0f) { pick = i; break; }
                }
                var card = candidates[pick];
                candidates.RemoveAt(pick);
                weights.RemoveAt(pick);

                int price = card.cost;
                if (price >= 1 && price <= 3 && rng.Chance(Balance.FreeCardChance)) price = 0;
                offer.Add(new OfferSlot { card = card, price = price });
            }
        }

        public void ApplyCard(CardDef c)
        {
            c.apply?.Invoke(match, this);
            stacks[c.id] = Stacks(c.id) + 1;
            picks.Add(c);
        }
    }
}
