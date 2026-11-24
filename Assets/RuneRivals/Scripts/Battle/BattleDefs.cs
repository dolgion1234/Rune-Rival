using System.Collections.Generic;

namespace RuneRivals
{
    public enum UnitRole { Hero, Soldier, Elite, Boss, Structure, Monster, MonsterElite, MonsterBoss }

    public enum Shot { None, Arrow, BoneArrow, Fireball, Frost, Magic, Cannonball, Ember }

    /// <summary>Нэгжийн үндсэн өгөгдөл (түвшин 1, долгион 1).</summary>
    public sealed class UnitDef
    {
        public string id, name, sprite;
        public UnitRole role;
        public float hp, atk, interval = 1f, range = 5f, speed, armor;
        public bool ranged, flying, cleave;
        public float splash;          // Тойрог гэмтлийн радиус (талбайн нэгжээр)
        public float slowOnHit;       // Хурд сааруулах (0.35 = 35%)
        public int pierceEvery;       // N дахь сум бүр 2 дайсныг нэвтэлнэ
        public Shot shot;
        public float shotSpeed = 70f;
        public int cost;              // Долгионы оноо
        public int minWave = 1;
        public string splitInto;      // Үхэхдээ хуваагдах
        public int splitCount;
        public float healPower;       // Шидтэн дайснуудаа эмчилнэ
        public float regen;           // Секунд бүр max HP-ийн хэдэн хувийг нөхөх
        public float scale = 1f;      // Дэлгэц дээрх хэмжээ

        public bool IsMonster => role >= UnitRole.Monster;
        public bool IsAllyTroop => role == UnitRole.Soldier || role == UnitRole.Elite || role == UnitRole.Boss;
    }

    public static class UnitDefs
    {
        public static readonly Dictionary<string, UnitDef> All = new Dictionary<string, UnitDef>();

        // ---- Баатрууд ----
        public static readonly UnitDef Arin = Add(new UnitDef
        {
            id = "h_archer", name = "Arin", sprite = "hero_archer", role = UnitRole.Hero,
            hp = 1000, atk = 40, interval = 0.8f, range = 66, ranged = true, shot = Shot.Arrow, shotSpeed = 90, pierceEvery = 4, scale = 1.15f
        });
        public static readonly UnitDef Brakka = Add(new UnitDef
        {
            id = "h_knight", name = "Brakka", sprite = "hero_knight", role = UnitRole.Hero,
            hp = 1650, atk = 64, interval = 1.0f, range = 14, cleave = true, armor = 0.2f, regen = 0.012f, scale = 1.15f
        });
        public static readonly UnitDef Seren = Add(new UnitDef
        {
            id = "h_pyro", name = "Seren", sprite = "hero_pyro", role = UnitRole.Hero,
            hp = 900, atk = 50, interval = 1.25f, range = 60, ranged = true, shot = Shot.Fireball, shotSpeed = 55, splash = 9, scale = 1.15f
        });
        public static readonly UnitDef Volka = Add(new UnitDef
        {
            id = "h_frost", name = "Volka", sprite = "hero_frost", role = UnitRole.Hero,
            hp = 950, atk = 42, interval = 0.85f, range = 62, ranged = true, shot = Shot.Frost, shotSpeed = 75, slowOnHit = 0.4f, splash = 4, scale = 1.15f
        });

        // ---- Манай цэргүүд ----
        public static readonly UnitDef Footman = Add(new UnitDef
        {
            id = "footman", name = "Footman", sprite = "footman", role = UnitRole.Soldier,
            hp = 260, atk = 22, interval = 0.9f, range = 5, speed = 12
        });
        public static readonly UnitDef Bowman = Add(new UnitDef
        {
            id = "bowman", name = "Bowman", sprite = "bowman", role = UnitRole.Soldier,
            hp = 150, atk = 19, interval = 1.1f, range = 55, ranged = true, shot = Shot.Arrow, shotSpeed = 80
        });
        public static readonly UnitDef Guardian = Add(new UnitDef
        {
            id = "guardian", name = "Guardian", sprite = "guardian", role = UnitRole.Elite,
            hp = 720, atk = 46, interval = 1.0f, range = 6, speed = 11, armor = 0.2f, scale = 1.1f
        });
        public static readonly UnitDef Ranger = Add(new UnitDef
        {
            id = "ranger", name = "Ranger", sprite = "ranger", role = UnitRole.Elite,
            hp = 360, atk = 44, interval = 0.9f, range = 60, ranged = true, shot = Shot.Arrow, shotSpeed = 90, scale = 1.1f
        });
        public static readonly UnitDef Champion = Add(new UnitDef
        {
            id = "champion", name = "Champion", sprite = "champion", role = UnitRole.Boss,
            hp = 1900, atk = 95, interval = 1.2f, range = 8, speed = 10, armor = 0.2f, cleave = true, scale = 1.25f
        });
        public static readonly UnitDef Cannon = Add(new UnitDef
        {
            id = "cannon", name = "Cannon", sprite = "cannon", role = UnitRole.Structure,
            hp = 500, atk = 75, interval = 2.6f, range = 72, ranged = true, shot = Shot.Cannonball, shotSpeed = 60, splash = 8
        });

        // ---- Дайснууд (10 төрөл) ----
        public static readonly UnitDef Slime = Add(new UnitDef
        {
            id = "slime", name = "Gloop", sprite = "slime", role = UnitRole.Monster,
            hp = 130, atk = 11, interval = 1.2f, range = 5, speed = 7, cost = 1, minWave = 1, splitInto = "slimelet", splitCount = 2
        });
        public static readonly UnitDef Slimelet = Add(new UnitDef
        {
            id = "slimelet", name = "Gloopling", sprite = "slimelet", role = UnitRole.Monster,
            hp = 45, atk = 6, interval = 1.0f, range = 5, speed = 9, cost = 0, minWave = 999
        });
        public static readonly UnitDef Goblin = Add(new UnitDef
        {
            id = "goblin", name = "Goblin", sprite = "goblin", role = UnitRole.Monster,
            hp = 170, atk = 17, interval = 1.0f, range = 5, speed = 10, cost = 2, minWave = 1
        });
        public static readonly UnitDef Bat = Add(new UnitDef
        {
            id = "bat", name = "Night Bat", sprite = "bat", role = UnitRole.Monster,
            hp = 75, atk = 13, interval = 0.9f, range = 5, speed = 16, flying = true, cost = 1, minWave = 2
        });
        public static readonly UnitDef BoneArcher = Add(new UnitDef
        {
            id = "bonearcher", name = "Bone Archer", sprite = "bonearcher", role = UnitRole.Monster,
            hp = 115, atk = 15, interval = 1.4f, range = 40, speed = 8, ranged = true, shot = Shot.BoneArrow, shotSpeed = 70, cost = 2, minWave = 3
        });
        public static readonly UnitDef Wolf = Add(new UnitDef
        {
            id = "wolf", name = "Dire Wolf", sprite = "wolf", role = UnitRole.Monster,
            hp = 155, atk = 21, interval = 0.8f, range = 5, speed = 17, cost = 2, minWave = 4
        });
        public static readonly UnitDef Imp = Add(new UnitDef
        {
            id = "imp", name = "Fire Imp", sprite = "imp", role = UnitRole.Monster,
            hp = 105, atk = 20, interval = 1.4f, range = 34, speed = 12, ranged = true, shot = Shot.Ember, shotSpeed = 55, splash = 3, cost = 3, minWave = 6
        });
        public static readonly UnitDef Shaman = Add(new UnitDef
        {
            id = "shaman", name = "Mire Shaman", sprite = "shaman", role = UnitRole.Monster,
            hp = 165, atk = 14, interval = 1.5f, range = 36, speed = 8, ranged = true, shot = Shot.Magic, shotSpeed = 55, healPower = 0.15f, cost = 3, minWave = 7
        });
        public static readonly UnitDef Ogre = Add(new UnitDef
        {
            id = "ogre", name = "Ogre", sprite = "ogre", role = UnitRole.MonsterElite,
            hp = 680, atk = 50, interval = 1.6f, range = 7, speed = 7, cost = 6, minWave = 8, scale = 1.2f
        });
        public static readonly UnitDef Golem = Add(new UnitDef
        {
            id = "golem", name = "Stone Golem", sprite = "golem", role = UnitRole.MonsterElite,
            hp = 1150, atk = 42, interval = 1.8f, range = 7, speed = 4.5f, armor = 0.3f, cost = 8, minWave = 10, scale = 1.25f
        });
        public static readonly UnitDef Drake = Add(new UnitDef
        {
            id = "drake", name = "Ember Drake", sprite = "drake", role = UnitRole.MonsterBoss,
            hp = 2600, atk = 70, interval = 1.7f, range = 30, speed = 5, ranged = true, shot = Shot.Fireball, shotSpeed = 50, splash = 7, cost = 0, minWave = 999, scale = 1.35f
        });

        public static readonly UnitDef[] Monsters = { Slime, Goblin, Bat, BoneArcher, Wolf, Imp, Shaman, Ogre, Golem, Drake };

        static UnitDef Add(UnitDef d)
        {
            All[d.id] = d;
            return d;
        }

        public static UnitDef Get(string id) => All.TryGetValue(id, out var d) ? d : null;
    }

    /// <summary>Тоглогчийн сонгох баатар.</summary>
    public sealed class HeroDef
    {
        public string id, name, title, ability;
        public UnitDef unit;
        public int goldCost, gemCost;   // 0, 0 = үнэгүй
    }

    public static class Heroes
    {
        public static readonly HeroDef[] All =
        {
            new HeroDef { id = "archer", name = "Arin", title = "Swift Archer", unit = UnitDefs.Arin,
                ability = "Fast arrows. Every 4th arrow pierces 2 foes." },
            new HeroDef { id = "knight", name = "Brakka", title = "Iron Knight", unit = UnitDefs.Brakka, goldCost = 2500,
                ability = "Huge HP, armor and regeneration. Cleaves every foe nearby." },
            new HeroDef { id = "pyro", name = "Seren", title = "Pyromancer", unit = UnitDefs.Seren, gemCost = 150,
                ability = "Fireballs explode and burn groups of foes." },
            new HeroDef { id = "frost", name = "Volka", title = "Frost Witch", unit = UnitDefs.Volka, gemCost = 300,
                ability = "Ice bolts shatter on impact and slow foes by 40%." },
        };

        public static HeroDef Get(string id)
        {
            foreach (var h in All) if (h.id == id) return h;
            return All[0];
        }
    }
}
