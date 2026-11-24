using System;
using System.Collections.Generic;

namespace RuneRivals
{
    /// <summary>
    /// Нэг талын тулааны талбай: баатар + цэргүүд (зүүн тал) болон дайснууд (баруунаас ирнэ).
    /// Бүх зүйл автоматаар тулалдана. x: 0..100, y: 0..1 (дээрээс доош).
    /// </summary>
    public sealed class Lane
    {
        public readonly Match match;
        public readonly Side side;
        public readonly List<Unit> units = new List<Unit>(64);
        public readonly List<Projectile> shots = new List<Projectile>(64);
        public readonly List<Trap> traps = new List<Trap>();
        public Unit hero;
        public float time;
        public int frostCharges;

        struct Spawn
        {
            public UnitDef def;
            public float at, y, hpMul, atkMul;
            public bool sent;
        }

        readonly List<Spawn> pending = new List<Spawn>();
        readonly List<Unit> scratch = new List<Unit>(32);
        bool smiteDone;

        Rng Rng => side.rng;

        public Lane(Match match, Side side)
        {
            this.match = match;
            this.side = side;
        }

        public int PendingCount => pending.Count;

        public int MonsterCount
        {
            get
            {
                int n = 0;
                foreach (var u in units) if (u.alive && u.enemy) n++;
                return n;
            }
        }

        // ================= Эхлэл =================

        public void CreateHero()
        {
            hero = new Unit { def = side.hero.unit, enemy = false, homeX = Balance.HeroX, homeY = 0.5f };
            hero.x = hero.prevX = hero.homeX;
            hero.y = hero.prevY = hero.homeY;
            side.Recalc(hero);
            hero.hp = hero.maxHp;
            units.Add(hero);
        }

        public void BeginFight(List<WaveEntry> wave, int waveNo)
        {
            time = 0f;
            smiteDone = false;
            // Өмнөх тулаанаас гарч амжаагүй дайснууд эхэнд нь гарна
            for (int i = 0; i < pending.Count; i++)
            {
                var p = pending[i];
                p.at = 0.2f + 0.3f * i;
                pending[i] = p;
            }
            float hpMul = Balance.WaveHpMul(waveNo) * match.levelScale, atkMul = Balance.WaveAtkMul(waveNo) * match.levelScale;

            foreach (var e in wave)
                pending.Add(new Spawn { def = e.def, at = e.delay, y = e.y, hpMul = hpMul, atkMul = atkMul });

            for (int i = 0; i < side.incoming.Count; i++)
                pending.Add(new Spawn
                {
                    def = side.incoming[i], at = 0.8f + i * 0.45f, y = Rng.Range(0.1f, 0.9f),
                    hpMul = hpMul * Balance.SentPower, atkMul = atkMul * Balance.SentPower, sent = true
                });
            side.incoming.Clear();

            // Хавх: шинээр тавих
            int alive = 0;
            foreach (var t in traps) if (t.alive) alive++;
            traps.RemoveAll(t => !t.alive);
            for (int i = alive; i < side.mods.traps; i++)
                traps.Add(new Trap { x = 40f + 7f * i, y = Rng.Range(0.2f, 0.8f) });

            frostCharges = 2 * side.mods.frost;

            // Хорт худаг (өрсөлдөгчийн карт)
            int poison = side.rival.mods.poison;
            if (poison > 0 && hero.alive)
                TrueDamage(hero, hero.maxHp * 0.04f * poison, Ev.Poison, lethal: true);
        }

        // ================= Алхам =================

        public void Tick(float dt)
        {
            time += dt;

            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                u.prevX = u.x;
                u.prevY = u.y;
            }
            for (int i = 0; i < shots.Count; i++)
            {
                shots[i].prevX = shots[i].x;
                shots[i].prevY = shots[i].y;
            }

            // Дайсан гаргах (талбай дүүрсэн бол түр хүлээнэ)
            int monsters = MonsterCount;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].at > time) continue;
                if (monsters >= Balance.MaxMonsters) break;
                monsters++;
                var p = pending[i];
                pending.RemoveAt(i);
                SpawnMonster(p.def, p.hpMul, p.atkMul, p.sent, Balance.SpawnX + Rng.Range(0f, 3f), p.y);
            }

            // Аянга (Smite)
            if (!smiteDone && time >= 4f && side.mods.smite > 0)
            {
                smiteDone = true;
                for (int k = 0; k < side.mods.smite; k++)
                {
                    Unit best = null;
                    foreach (var u in units)
                        if (u.alive && u.enemy && (best == null || u.hp > best.hp)) best = u;
                    if (best == null) break;
                    TrueDamage(best, best.maxHp * 0.4f, Ev.Smite, lethal: true);
                }
            }

            int count = units.Count;
            for (int i = 0; i < count; i++)
            {
                var u = units[i];
                if (!u.alive) continue;
                u.age += dt;
                u.attackAnim += dt;
                if (u.slow > 0f) u.slow -= dt;
                if (u.stun > 0f) { u.stun -= dt; u.moving = false; continue; }
                if (u.cd > 0f) u.cd -= dt;
                if (u.enemy) UpdateMonster(u, dt);
                else UpdateAlly(u, dt);
            }

            UpdateShots(dt);
            UpdateTraps();

            units.RemoveAll(u => !u.alive && u != hero);
            shots.RemoveAll(s => !s.alive);
        }

        // ================= Дайсан =================

        void UpdateMonster(Unit u, float dt)
        {
            if (u.def.healPower > 0f)
            {
                u.healTimer -= dt;
                if (u.healTimer <= 0f)
                {
                    u.healTimer = 4f;
                    Unit worst = null;
                    foreach (var o in units)
                        if (o.alive && o.enemy && o != u && o.HpFrac < 0.95f && Math.Abs(o.x - u.x) < 35f &&
                            (worst == null || o.HpFrac < worst.HpFrac)) worst = o;
                    if (worst != null) Heal(worst, worst.maxHp * u.def.healPower);
                }
            }

            var t = FindAllyTarget(u);
            float dist = t != null ? u.x - t.x : float.MaxValue;
            if (t != null && dist <= u.def.range)
            {
                u.moving = false;
                if (!u.def.ranged) u.y += (t.y - u.y) * Math.Min(1f, dt * 2f);
                if (u.cd <= 0f) Attack(u, t);
            }
            else
            {
                u.moving = true;
                u.x -= u.SpeedNow * dt;
                if (t != null && !u.def.ranged && dist < 14f) u.y += (t.y - u.y) * Math.Min(1f, dt * 1.5f);
                float minX = Balance.HeroX + 3f;
                if (u.x < minX) u.x = minX;
            }
        }

        Unit FindAllyTarget(Unit m)
        {
            if (m.Flying) return hero != null && hero.alive ? hero : null;
            Unit best = null;
            float bestD = float.MaxValue;
            foreach (var a in units)
            {
                if (!a.alive || a.enemy) continue;
                float d = m.x - a.x;
                if (d < -2f) continue;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        // ================= Манай тал =================

        void UpdateAlly(Unit u, float dt)
        {
            if (u.def.regen > 0f && u.hp < u.maxHp) u.hp = Math.Min(u.maxHp, u.hp + u.maxHp * u.def.regen * dt);
            if (u.IsHero || u.def.ranged || u.Role == UnitRole.Structure)
            {
                var t = FindMonsterTarget(u, u.def.range, true);
                if (t != null && u.cd <= 0f) Attack(u, t);
                return;
            }

            // Ойрын тулаанч: хамгаалах шугамаасаа давшиж дайралт хийнэ
            Unit target = null;
            float bestD = float.MaxValue;
            foreach (var m in units)
            {
                if (!m.alive || !m.enemy) continue;
                if (m.Flying)
                {
                    if (Math.Abs(m.x - u.x) <= u.def.range && Math.Abs(m.y - u.y) < 0.35f)
                    {
                        float df = Math.Abs(m.x - u.x);
                        if (df < bestD) { bestD = df; target = m; }
                    }
                    continue;
                }
                if (m.x > u.homeX + Balance.EngageZone) continue;
                float d = Math.Abs(m.x - u.x) + Math.Abs(m.y - u.y) * 8f;
                if (d < bestD) { bestD = d; target = m; }
            }

            if (target != null)
            {
                float dx = target.x - u.x;
                if (Math.Abs(dx) <= u.def.range)
                {
                    u.moving = false;
                    if (u.cd <= 0f) Attack(u, target);
                }
                else
                {
                    u.moving = true;
                    u.x += Math.Sign(dx) * u.def.speed * dt;
                    u.y += (target.y - u.y) * Math.Min(1f, dt * 2f);
                }
            }
            else
            {
                float dx = u.homeX - u.x, dy = u.homeY - u.y;
                if (Math.Abs(dx) > 0.3f || Math.Abs(dy) > 0.02f)
                {
                    u.moving = true;
                    u.x += Math.Sign(dx) * Math.Min(Math.Abs(dx), u.def.speed * dt);
                    u.y += dy * Math.Min(1f, dt * 2f);
                }
                else u.moving = false;
            }
        }

        /// <summary>Зай дотор байгаа, баатарт хамгийн ойр дайсан.</summary>
        Unit FindMonsterTarget(Unit u, float range, bool any)
        {
            Unit best = null;
            foreach (var m in units)
            {
                if (!m.alive || !m.enemy) continue;
                float d = m.x - u.x;
                if (d > range || d < -4f) continue;
                if (!any && m.Flying) continue;
                if (best == null || m.x < best.x) best = m;
            }
            return best;
        }

        // ================= Довтолгоо =================

        void Attack(Unit u, Unit t)
        {
            float speed = u.enemy ? 1f : side.mods.atkSpeed;
            u.cd = u.def.interval / speed;
            u.attackAnim = 0f;
            bool crit = u.crit > 0f && Rng.Chance(u.crit);
            float dmg = u.atk * (crit ? Balance.CritMult : 1f);

            if (u.def.ranged)
            {
                bool pierce = false;
                if (u.def.pierceEvery > 0)
                {
                    u.shots++;
                    pierce = u.shots % u.def.pierceEvery == 0;
                }
                Fire(u, t, dmg, crit, pierce);

                for (int k = 0; k < u.multishot; k++)
                {
                    scratch.Clear();
                    foreach (var m in units)
                        if (m.alive && m.enemy != u.enemy && m != t && m.x - u.x <= u.def.range && m.x - u.x > -4f) scratch.Add(m);
                    var t2 = scratch.Count > 0 ? Rng.Pick(scratch) : t;
                    Fire(u, t2, dmg * 0.6f, false, false);
                }
            }
            else
            {
                match.Emit(new BattleEvent { type = Ev.Attack, side = side.index, unit = u, other = t });
                Damage(u, t, dmg, crit);
                if (u.def.cleave)
                {
                    scratch.Clear();
                    foreach (var o in units)
                        if (o.alive && o != t && o.enemy == t.enemy && Math.Abs(o.x - t.x) < 7f && Math.Abs(o.y - t.y) < 0.5f) scratch.Add(o);
                    foreach (var o in scratch) Damage(u, o, dmg * Balance.SplashRatio, false);
                }
            }
        }

        void Fire(Unit u, Unit t, float dmg, bool crit, bool pierce)
        {
            var p = new Projectile
            {
                owner = u, target = t, kind = u.def.shot, speed = u.def.shotSpeed,
                x = u.x + (u.enemy ? -2f : 2f), y = u.y, damage = dmg, crit = crit, pierce = pierce,
                splash = u.def.splash, slow = u.def.slowOnHit, fromEnemy = u.enemy
            };
            p.prevX = p.x;
            p.prevY = p.y;
            shots.Add(p);
            match.Emit(new BattleEvent { type = Ev.Shoot, side = side.index, unit = u, other = t, shot = p.kind });
        }

        void UpdateShots(float dt)
        {
            for (int i = 0; i < shots.Count; i++)
            {
                var p = shots[i];
                if (!p.alive) continue;
                var t = p.target;
                if (t == null || !t.alive)
                {
                    p.alive = false;
                    continue;
                }
                float dx = t.x - p.x;
                float step = p.speed * dt;
                p.y += (t.y - p.y) * Math.Min(1f, dt * 10f);
                if (Math.Abs(dx) <= Math.Max(1.2f, step))
                {
                    p.x = t.x;
                    Impact(p);
                }
                else p.x += Math.Sign(dx) * step;
            }
        }

        void Impact(Projectile p)
        {
            p.alive = false;
            var t = p.target;
            float tx = t.x, ty = t.y;
            bool targetEnemy = t.enemy;
            Damage(p.owner, t, p.damage, p.crit);
            if (p.slow > 0f && t.alive) ApplySlow(t, p.slow, 2.5f);

            if (p.splash > 0f)
            {
                match.Emit(new BattleEvent { type = Ev.Text, side = side.index, x = tx, y = ty, value = p.splash, shot = p.kind, text = "blast" });
                scratch.Clear();
                foreach (var o in units)
                    if (o.alive && o != t && o.enemy == targetEnemy && Math.Abs(o.x - tx) <= p.splash && Math.Abs(o.y - ty) < 0.6f) scratch.Add(o);
                foreach (var o in scratch) Damage(p.owner, o, p.damage * Balance.SplashRatio, false);
            }

            if (p.pierce)
            {
                Unit next = null;
                foreach (var o in units)
                    if (o.alive && o != t && o.enemy == targetEnemy && o.x > tx && o.x - tx < 14f && (next == null || o.x < next.x)) next = o;
                if (next != null) Damage(p.owner, next, p.damage * 0.8f, false);
            }
        }

        // ================= Гэмтэл, эдгэрэлт =================

        public void Damage(Unit src, Unit dst, float raw, bool crit)
        {
            if (dst == null || !dst.alive) return;
            if (dst.dodge > 0f && Rng.Chance(dst.dodge))
            {
                match.Emit(new BattleEvent { type = Ev.Dodge, side = side.index, unit = dst, x = dst.x, y = dst.y });
                return;
            }
            float dmg = raw * (1f - Math.Min(Balance.MaxDef, dst.armor));
            dst.hp -= dmg;
            match.Emit(new BattleEvent { type = Ev.Hit, side = side.index, unit = dst, other = src, value = dmg, crit = crit, x = dst.x, y = dst.y });

            if (src != null && src.alive && src.lifesteal > 0f && src.hp < src.maxHp)
                src.hp = Math.Min(src.maxHp, src.hp + dmg * src.lifesteal);

            if (dst.hp <= 0f) Kill(dst);
        }

        /// <summary>Хамгаалалт, зайлсхийлтийг үл тоох гэмтэл (хор, аянга, Blood Price).</summary>
        public void TrueDamage(Unit dst, float amount, Ev type, bool lethal)
        {
            if (dst == null || !dst.alive || amount <= 0f) return;
            if (!lethal) amount = Math.Min(amount, dst.hp - 1f);
            if (amount <= 0f) return;
            dst.hp -= amount;
            match.Emit(new BattleEvent { type = type, side = side.index, unit = dst, value = amount, x = dst.x, y = dst.y });
            if (dst.hp <= 0f) Kill(dst);
        }

        public void Heal(Unit u, float amount)
        {
            if (u == null || !u.alive || amount <= 0f) return;
            float before = u.hp;
            u.hp = Math.Min(u.maxHp, u.hp + amount);
            float healed = u.hp - before;
            if (healed > 0.5f)
                match.Emit(new BattleEvent { type = Ev.Heal, side = side.index, unit = u, value = healed, x = u.x, y = u.y });
        }

        public void ApplySlow(Unit u, float amount, float duration)
        {
            bool fresh = u.slow <= 0f;
            u.slowAmount = fresh ? amount : Math.Max(u.slowAmount, amount);
            u.slow = Math.Max(u.slow, duration);
            if (fresh) match.Emit(new BattleEvent { type = Ev.Slow, side = side.index, unit = u, x = u.x, y = u.y });
        }

        void Kill(Unit u)
        {
            if (!u.alive) return;
            u.alive = false;
            u.hp = 0f;
            match.Emit(new BattleEvent { type = Ev.Death, side = side.index, unit = u, x = u.x, y = u.y });

            if (u.enemy)
            {
                side.kills++;
                if (side.mods.bounty > 0)
                {
                    side.bountyKills++;
                    if (side.bountyKills >= 6)
                    {
                        side.bountyKills -= 6;
                        side.AddEnergy(side.mods.bounty);
                        match.Emit(new BattleEvent { type = Ev.Energy, side = side.index, value = side.mods.bounty, x = u.x, y = u.y });
                    }
                }
                if (!string.IsNullOrEmpty(u.def.splitInto))
                {
                    var child = UnitDefs.Get(u.def.splitInto);
                    for (int i = 0; i < u.def.splitCount; i++)
                        SpawnMonster(child, u.hpMul, u.atkMul, u.sent, u.x + (i == 0 ? -1.5f : 1.5f),
                            Clamp(u.y + (i == 0 ? -0.12f : 0.12f), 0.05f, 0.95f));
                }
            }
            else if (u.IsHero)
            {
                match.OnHeroDied(side);
            }
            else if (u.IsTroop && side.mods.lastStand > 0)
            {
                float frac = 0.1f * side.mods.lastStand;
                foreach (var a in units)
                    if (a.alive && !a.enemy && a != u) Heal(a, a.maxHp * frac);
            }
        }

        void SpawnMonster(UnitDef def, float hpMul, float atkMul, bool sent, float x, float y)
        {
            var u = new Unit
            {
                def = def, enemy = true, sent = sent, x = x, y = y, prevX = x, prevY = y,
                hpMul = hpMul, atkMul = atkMul, moving = true,
            };
            u.maxHp = u.hp = def.hp * hpMul;
            u.atk = def.atk * atkMul;
            u.armor = def.armor;
            u.cd = Rng.Range(0f, def.interval * 0.6f);
            u.healTimer = 2f;
            units.Add(u);
            match.Emit(new BattleEvent { type = Ev.Spawn, side = side.index, unit = u, x = x, y = y });

            if (frostCharges > 0 && u.def.role != UnitRole.MonsterBoss)
            {
                frostCharges--;
                ApplySlow(u, 0.5f, 8f);
            }
        }

        void UpdateTraps()
        {
            foreach (var t in traps)
            {
                if (!t.alive) continue;
                foreach (var m in units)
                {
                    if (!m.alive || !m.enemy || m.Flying) continue;
                    if (m.x <= t.x + 0.5f && m.x > t.x - 10f)
                    {
                        t.alive = false;
                        m.stun = Math.Max(m.stun, 3f);
                        match.Emit(new BattleEvent { type = Ev.TrapSnap, side = side.index, unit = m, x = t.x, y = t.y });
                        TrueDamage(m, m.maxHp * 0.1f, Ev.Hit, lethal: true);
                        break;
                    }
                }
            }
        }

        static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
    }
}
