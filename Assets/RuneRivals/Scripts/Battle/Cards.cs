using System;
using System.Collections.Generic;

namespace RuneRivals
{
    public enum CardKind { Troop, Boost, Hero, Sabotage, Economy, Tactic }
    public enum Rarity { Common, Rare, Epic }

    /// <summary>Нэг картын тодорхойлолт. apply = сонгох үед хийгдэх үйлдэл.</summary>
    public sealed class CardDef
    {
        public string id, name, text, trigger = "", art;
        public int cost;
        public Rarity rarity;
        public CardKind kind;
        public int maxStacks;       // 0 = хязгааргүй
        public int unlockTier;      // Лигийн шат (0 = Bronze)
        public bool starter;        // Armory-д эхлэх карт болгож болох уу
        public bool summon;         // Цэрэг дууддаг уу (цэрэг дүүрсэн үед санал болгохгүй)
        public float value;         // Ботын үнэлгээ (энергийн нэгжээр)
        public Action<Match, Side> apply;
    }

    /// <summary>
    /// 30 карт. Нэр, тайлбар, үнийг эндээс өөрчилнө.
    /// unlockTier: 0 Bronze, 1 Silver, 2 Gold, 3 Platinum.
    /// </summary>
    public static class Cards
    {
        public static readonly List<CardDef> All = new List<CardDef>();
        static readonly Dictionary<string, CardDef> byId = new Dictionary<string, CardDef>();

        public static CardDef Get(string id) => id != null && byId.TryGetValue(id, out var c) ? c : null;

        static Cards()
        {
            // ---------------- TROOPS ----------------
            Add("footman", "Footman", "+1 Footman\n(melee soldier)", 1, Rarity.Common, CardKind.Troop, "footman", 0, 2.2f,
                (m, s) => s.Summon(UnitDefs.Footman), starter: true, summon: true);
            Add("bowman", "Bowman", "+1 Bowman\n(ranged soldier)", 1, Rarity.Common, CardKind.Troop, "bowman", 0, 2.2f,
                (m, s) => s.Summon(UnitDefs.Bowman), starter: true, summon: true);
            Add("recruits", "Recruits", "Summon 2 random soldiers", 2, Rarity.Common, CardKind.Troop, "icon_recruits", 0, 4f,
                (m, s) =>
                {
                    for (int i = 0; i < 2; i++) s.Summon(s.rng.Chance(0.5f) ? UnitDefs.Footman : UnitDefs.Bowman);
                }, starter: true, summon: true);
            Add("guardian", "Guardian", "+1 Guardian\n(melee elite)", 3, Rarity.Rare, CardKind.Troop, "guardian", 0, 4.8f,
                (m, s) => s.Summon(UnitDefs.Guardian), starter: true, summon: true);
            Add("ranger", "Ranger", "+1 Ranger\n(ranged elite)", 3, Rarity.Rare, CardKind.Troop, "ranger", 0, 4.8f,
                (m, s) => s.Summon(UnitDefs.Ranger), starter: true, summon: true);
            Add("promotion", "Promotion", "Turn 1 random soldier into an elite", 2, Rarity.Rare, CardKind.Troop, "icon_promote", 1, 3.6f,
                (m, s) => s.Promote());
            Add("champion", "Champion", "+1 Champion\n(boss ally)", 5, Rarity.Epic, CardKind.Troop, "champion", 3, 8f,
                (m, s) => s.Summon(UnitDefs.Champion), summon: true);

            // ---------------- BOOSTS ----------------
            Add("whetstone", "Whetstone", "Soldiers\n+30% ATK", 2, Rarity.Common, CardKind.Boost, "icon_sword", 0, 3.4f,
                (m, s) => { s.mods.soldierAtk += 0.3f; s.RecalcAll(); }, starter: true);
            Add("rations", "Iron Rations", "Soldiers\n+30% max HP", 2, Rarity.Common, CardKind.Boost, "icon_meat", 0, 3.2f,
                (m, s) => { s.mods.soldierHp += 0.3f; s.RecalcAll(); }, starter: true);
            Add("drums", "War Drums", "All allies\n+20% attack speed", 3, Rarity.Rare, CardKind.Boost, "icon_drum", 2, 4.4f,
                (m, s) => { s.mods.atkSpeed += 0.2f; }, max: 5);
            Add("shieldwall", "Shield Wall", "All allies\n+15% defense", 2, Rarity.Rare, CardKind.Boost, "icon_shield", 1, 3.2f,
                (m, s) => { s.mods.def += 0.15f; s.RecalcAll(); }, max: 4, starter: true);
            Add("volley", "Volley", "Ranged soldiers shoot +1 arrow", 3, Rarity.Epic, CardKind.Boost, "icon_volley", 3, 4.4f,
                (m, s) => { s.mods.multishot++; s.RecalcAll(); }, max: 2);
            Add("bloodlust", "Bloodlust", "All allies\n+12% lifesteal", 2, Rarity.Rare, CardKind.Boost, "icon_fang", 1, 3f,
                (m, s) => { s.mods.lifesteal += 0.12f; s.RecalcAll(); }, max: 4);
            Add("laststand", "Last Stand", "All allies heal 10% HP", 2, Rarity.Rare, CardKind.Boost, "icon_banner", 2, 3f,
                (m, s) => { s.mods.lastStand++; }, max: 3, trigger: "ON ALLY DEATH");

            // ---------------- HERO ----------------
            Add("mend", "Mend", "Heal your hero\n30% HP", 1, Rarity.Common, CardKind.Hero, "icon_heart", 0, 2.6f,
                (m, s) => s.HealHero(0.3f));
            Add("fortify", "Fortify", "Hero\n+25% max HP", 2, Rarity.Common, CardKind.Hero, "icon_armor", 0, 3f,
                (m, s) => { s.mods.heroHp += 0.25f; s.RecalcAll(); }, starter: true);
            Add("fury", "Hero's Fury", "Hero\n+35% ATK", 2, Rarity.Common, CardKind.Hero, "icon_fury", 0, 3.2f,
                (m, s) => { s.mods.heroAtk += 0.35f; s.RecalcAll(); }, starter: true);
            Add("evasion", "Evasion", "Hero\n+25% dodge chance", 3, Rarity.Rare, CardKind.Hero, "icon_boot", 2, 3.6f,
                (m, s) => { s.mods.heroDodge += 0.25f; s.RecalcAll(); }, max: 2);

            // ---------------- SABOTAGE ----------------
            Add("raid", "Goblin Raid", "Send 2 Goblins to rival's lane", 2, Rarity.Common, CardKind.Sabotage, "goblin", 0, 3f,
                (m, s) => s.rival.QueueIncoming(UnitDefs.Goblin, 2, s));
            Add("batswarm", "Bat Swarm", "Send 3 Bats at rival's hero", 3, Rarity.Rare, CardKind.Sabotage, "bat", 1, 4.2f,
                (m, s) => s.rival.QueueIncoming(UnitDefs.Bat, 3, s));
            Add("ogre", "Hired Ogre", "Send an Ogre to rival's lane", 5, Rarity.Epic, CardKind.Sabotage, "ogre", 3, 5.5f,
                (m, s) => s.rival.QueueIncoming(UnitDefs.Ogre, 1, s));
            Add("poison", "Poisoned Well", "Rival's hero loses 4% HP", 3, Rarity.Rare, CardKind.Sabotage, "icon_poison", 2, 4f,
                (m, s) => { s.mods.poison++; }, max: 4, trigger: "EACH TURN");
            Add("siphon", "Siphon", "Steal 2 energy from rival's income", 1, Rarity.Rare, CardKind.Sabotage, "icon_siphon", 1, 2.6f,
                (m, s) => { s.rival.drain += 2; s.siphon += 2; }, trigger: "NEXT TURN");

            // ---------------- ECONOMY ----------------
            Add("goldmine", "Gold Mine", "+1 energy", 2, Rarity.Rare, CardKind.Economy, "icon_mine", 0, 3.6f,
                (m, s) => { s.mods.goldMine++; }, max: 3, starter: true, trigger: "EACH TURN");
            Add("bloodprice", "Blood Price", "+3 energy\nHero loses 25% HP", 0, Rarity.Epic, CardKind.Economy, "icon_blood", 0, 2.2f,
                (m, s) =>
                {
                    s.AddEnergy(3);
                    s.lane.TrueDamage(s.lane.hero, s.lane.hero.hp * 0.25f, Ev.Hit, lethal: false);
                }, trigger: "ON PICK");
            Add("bounty", "Bounty", "+1 energy for every 6 kills", 3, Rarity.Epic, CardKind.Economy, "icon_bounty", 3, 4.2f,
                (m, s) => { s.mods.bounty++; }, max: 2);

            // ---------------- TACTICS ----------------
            Add("beartrap", "Bear Trap", "First foe to reach your line is stunned 3s", 2, Rarity.Common, CardKind.Tactic, "icon_trap", 0, 2.8f,
                (m, s) => { s.mods.traps++; }, max: 3, trigger: "EACH TURN");
            Add("frost", "Frost Rune", "The first 2 foes are slowed 50%", 2, Rarity.Rare, CardKind.Tactic, "icon_frost", 1, 3f,
                (m, s) => { s.mods.frost++; }, max: 3, trigger: "EACH TURN");
            Add("smite", "Smite", "Strike the toughest foe for 40% HP", 3, Rarity.Epic, CardKind.Tactic, "icon_bolt", 2, 4.4f,
                (m, s) => { s.mods.smite++; }, max: 3, trigger: "EACH TURN");
            Add("cannon", "Cannon", "Build a cannon that blasts groups of foes", 4, Rarity.Epic, CardKind.Tactic, "cannon", 3, 5.4f,
                (m, s) => s.Summon(UnitDefs.Cannon));
        }

        static void Add(string id, string name, string text, int cost, Rarity rarity, CardKind kind, string art, int tier, float value,
            Action<Match, Side> apply, int max = 0, bool starter = false, bool summon = false, string trigger = "")
        {
            var c = new CardDef
            {
                id = id, name = name, text = text, cost = cost, rarity = rarity, kind = kind, art = art, unlockTier = tier,
                value = value, apply = apply, maxStacks = max, starter = starter, summon = summon, trigger = trigger
            };
            All.Add(c);
            byId[id] = c;
        }

        /// <summary>Тухайн лигийн шатанд нээгдсэн картууд.</summary>
        public static List<CardDef> UnlockedAt(int tier)
        {
            var list = new List<CardDef>();
            foreach (var c in All) if (c.unlockTier <= tier) list.Add(c);
            return list;
        }

        public static int CountUnlockedAt(int tier)
        {
            int n = 0;
            foreach (var c in All) if (c.unlockTier <= tier) n++;
            return n;
        }
    }
}
