namespace RuneRivals
{
    /// <summary>Тулаан дахь нэг амьтан (баатар, цэрэг, дайсан, их буу).</summary>
    public sealed class Unit
    {
        static int nextId;

        public readonly int id = ++nextId;
        public UnitDef def;
        public bool enemy;            // true = дайсан (баруунаас ирнэ)
        public bool sent;             // Өрсөлдөгч илгээсэн
        public int slot = -1;         // Манай цэргийн байрлал

        public float x, y, prevX, prevY, homeX, homeY;
        public float hp, maxHp, atk, armor, crit, dodge, lifesteal;
        public int multishot;
        public float hpMul = 1f, atkMul = 1f;   // Дайсны долгионы үржүүлэгч
        public float cd;
        public float stun, slow, slowAmount;
        public float healTimer;
        public int shots;
        public bool alive = true;
        public bool moving;
        public float attackAnim = 99f;  // Сүүлд довтолсноос хойших хугацаа (харагдацад)
        public float age;

        public UnitRole Role => def.role;
        public bool IsHero => def.role == UnitRole.Hero;
        public bool IsTroop => def.IsAllyTroop;
        public bool Ranged => def.ranged;
        public bool Flying => def.flying;
        public float HpFrac => maxHp > 0 ? hp / maxHp : 0f;
        public float SpeedNow => def.speed * (slow > 0 ? 1f - slowAmount : 1f);
    }

    public sealed class Projectile
    {
        public Unit owner, target;
        public Shot kind;
        public float x, y, prevX, prevY, speed;
        public float damage, splash, slow;
        public bool crit, pierce, alive = true, fromEnemy;
    }

    public sealed class Trap
    {
        public float x, y;
        public bool alive = true;
    }

    /// <summary>Нэг талын бүх сайжруулалт (карт сонгох бүрт өөрчлөгдөнө).</summary>
    public sealed class Mods
    {
        public float soldierHp = 1f, soldierAtk = 1f, atkSpeed = 1f, def, lifesteal, crit = Balance.BaseCrit;
        public int multishot;
        public float heroHp = 1f, heroAtk = 1f, heroDodge;
        public int lastStand, traps, frost, smite, poison, goldMine, bounty;
    }

    public enum Ev
    {
        Spawn, Attack, Shoot, Hit, Dodge, Heal, Death, Stun, Slow, Smite, TrapSnap, Poison,
        Energy, CardBought, Refresh, GetAll, Promote, Sent, TurnStart, FightStart, Winner, Text
    }

    /// <summary>Харагдацын (дүрс, дуу, эффект) хэсэгт дамжуулах үйл явдал.</summary>
    public struct BattleEvent
    {
        public Ev type;
        public int side;
        public Unit unit, other;
        public float value, x, y;
        public bool crit;
        public Shot shot;
        public CardDef card;
        public string text;
    }
}
