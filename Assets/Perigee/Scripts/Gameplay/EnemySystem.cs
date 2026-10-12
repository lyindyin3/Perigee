using UnityEngine;

namespace Perigee
{
    public class EnemySystem : MonoBehaviour
    {
        // обязательная часть префаба Паразита: кольцо, пока он сидит на звезде
        const string AttachMarkerName = "AttachMarker";

        class Enemy
        {
            public Visual visual;
            public Transform transform;
            public Transform body;
            public SpriteRenderer flash;
            public HpRing hpRing;
            public DamageStages cracks;
            public GameObject attachMarker;
            public EnemyDef def;
            public Vector2 position;
            public float hp;
            public float maxHp;
            // урон уже был: кольцо HP в режиме «после удара» остаётся на виду
            public bool hurt;
            public float speedMul;
            public float age;
            public float[] hitCooldown;
            // Паразит: присосался к звезде. Стрелок: встал на позицию.
            public bool engaged;
            // Паразит: секунды на звезде. Стрелок: секунды до выстрела.
            public float timer;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] StarController star;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] Fx fx;
        [SerializeField] AudioManager sfx;
        [SerializeField] Transform root;

        // живые враги лежат в начале массива: [0, count)
        Enemy[] live;
        int count;
        // запас выключенных врагов, свой на каждый тип: free[тип][0, freeCount[тип])
        Enemy[][] free;
        int[] freeCount;

        void Awake()
        {
            live = new Enemy[config.maxEnemies];
            free = new Enemy[config.enemies.Length][];
            freeCount = new int[free.Length];
            for (int t = 0; t < free.Length; t++) free[t] = new Enemy[config.maxEnemies];
        }

        public void ResetRun()
        {
            // пулы собираются заново из префабов: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            count = 0;

            EnemyDef[] defs = config.enemies;
            for (int i = 0; i < defs.Length; i++)
            {
                int t = (int)defs[i].id;
                freeCount[t] = 0;
                for (int k = 0; k < defs[i].prewarm; k++) free[t][freeCount[t]++] = Create(defs[i]);
            }
        }

        public void Spawn(EnemyType type, Vector2 position, float speedMul)
        {
            if (count == live.Length) return;

            EnemyDef def = Def(type);
            int t = (int)type;
            Enemy e = freeCount[t] > 0 ? free[t][--freeCount[t]] : Create(def);
            live[count++] = e;
            e.def = def;
            e.position = position;
            // эра усиливает только врагов с базовым hp от eraHpMinBaseHp
            e.maxHp = def.hp >= config.eraHpMinBaseHp ? def.hp * loop.Era.hpMul : def.hp;
            e.hp = e.maxHp;
            e.hurt = false;
            e.hpRing.Fill();
            e.speedMul = speedMul;
            e.age = 0f;
            e.engaged = false;
            e.timer = 0f;
            System.Array.Clear(e.hitCooldown, 0, e.hitCooldown.Length);
            Place(e);
            Gauge(e, 0f);
            e.visual.gameObject.SetActive(true);
        }

        public void Tick(float dt)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                e.age += dt;
                for (int k = 0; k < e.hitCooldown.Length; k++) e.hitCooldown[k] -= dt;

                switch (e.def.behavior)
                {
                    case EnemyBehavior.Straight:
                        if (Advance(e, 0f, dt) <= config.starRadius + e.def.radius)
                        {
                            star.Damage(e.def.starDamage);
                            Remove(i);
                            continue;
                        }
                        break;

                    case EnemyBehavior.Attach:
                        if (e.engaged)
                        {
                            // чем дольше сидит, тем быстрее пьёт
                            e.timer += dt;
                            star.Drain(Mathf.Min(config.parasiteDrainBase + config.parasiteDrainGrowth * e.timer, config.parasiteDrainMax) * dt);
                        }
                        else if (Advance(e, 0f, dt) <= config.starRadius + e.def.radius)
                        {
                            e.engaged = true;
                            e.timer = 0f;
                            e.position = e.position.normalized * (config.starRadius + config.parasiteAttachOffset);
                        }
                        break;

                    case EnemyBehavior.Stopper:
                        if (e.engaged)
                        {
                            e.timer -= dt;
                            if (e.timer <= 0f)
                            {
                                e.timer += config.shooterFireInterval;
                                Spawn(EnemyType.Bolt, e.position, 1f);
                            }
                        }
                        else if (Advance(e, config.shooterStopRadius, dt) <= config.shooterStopRadius)
                        {
                            e.engaged = true;
                            e.timer = config.shooterFirstDelay;
                        }
                        break;

                    case EnemyBehavior.Burster:
                        if (Advance(e, 0f, dt) <= config.hiveBurstRadius)
                        {
                            Burst(i);
                            continue;
                        }
                        break;
                }
                Place(e);
                Gauge(e, dt);
            }
        }

        public void Collide()
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                if (e.age < config.spawnFadeIn) continue;

                float reach = satellites.Radius + e.def.radius;
                for (int k = 0; k < satellites.Count; k++)
                {
                    if (!satellites.Active(k)) continue;
                    if (e.hitCooldown[k] > 0f) continue;
                    if ((satellites.Position(k) - e.position).sqrMagnitude > reach * reach) continue;

                    e.hitCooldown[k] = config.hitCooldown;
                    float damage = satellites.Damage(k);
                    Vector2 hitter = satellites.Position(k);
                    // броня: удар слабее брони урона не наносит; ВСПЛЕСК броню не замечает
                    if (damage < e.def.armor && !abilities.Active(AbilityId.Surge))
                    {
                        fx.Sparks(hitter, config.clinkSparks);
                        if (e.maxHp >= config.numberMinHp) fx.Number(Above(e), 0f, NumberKind.Blocked);
                        fx.HitStop(config.clinkHitStop);
                        sfx.Play(SfxId.Clink);
                        continue;
                    }

                    if (Hurt(i, damage, hitter)) break;
                    fx.Shards(hitter, config.hitShards);
                    sfx.Play(SfxId.Hit);
                }
            }
        }

        // волна от звезды: Рой гибнет сразу, остальные получают урон
        public void Blast(float radius, float damage)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                if (e.position.sqrMagnitude > radius * radius) continue;

                if (e.def.id == EnemyType.Swarmer) Kill(i);
                else Hurt(i, damage, Vector2.zero);
            }
        }

        // ИМПУЛЬС: Рой и Стрелы гибнут, остальные получают урон и отлетают наружу
        public void Pulse(float radius, float damage, float push)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                float r = e.position.magnitude;
                if (r > radius) continue;

                if (e.def.id == EnemyType.Swarmer || e.def.id == EnemyType.Arrow)
                {
                    Kill(i);
                    continue;
                }
                if (Hurt(i, damage, Vector2.zero)) continue;

                e.position *= (r + push) / r;
                // Паразит отцепляется и летит обратно, Стрелок заново идёт на позицию
                e.engaged = false;
                e.timer = 0f;
                Place(e);
            }
        }

        // возрождение: все враги рядом со звездой гибнут и роняют кристаллы
        public void Wipe(float radius)
        {
            for (int i = count - 1; i >= 0; i--)
                if (live[i].position.sqrMagnitude <= radius * radius) Kill(i);
        }

        public int Count => count;

        public int CountOf(EnemyType type)
        {
            int n = 0;
            for (int i = 0; i < count; i++)
                if (live[i].def.id == type) n++;
            return n;
        }

        // сколько врагов не дальше radius от звезды
        public int Within(float radius)
        {
            int n = 0;
            for (int i = 0; i < count; i++)
                if (live[i].position.sqrMagnitude <= radius * radius) n++;
            return n;
        }

        // шаг к центру, но не ближе stopRadius; возвращает новое расстояние до центра
        float Advance(Enemy e, float stopRadius, float dt)
        {
            float speed = e.def.speed * e.speedMul;
            if (e.def.id == EnemyType.Arrow) speed *= loop.Stats.arrowSpeedMul;

            float r = e.position.magnitude;
            float next = Mathf.MoveTowards(r, stopRadius, speed * dt);
            e.position *= next / r;
            return next;
        }

        // Улей лопается у звезды: кристаллов нет, вместо них Рой
        void Burst(int i)
        {
            Vector2 centre = live[i].position;
            Remove(i);

            Rng rng = loop.Run.simRng;
            for (int k = 0; k < config.hiveSwarmers; k++)
            {
                // корень из случайного числа даёт равномерный разброс по площади круга
                float r = config.hiveBurstSpread * Mathf.Sqrt(rng.Range(0f, 1f));
                float rad = rng.Range(0f, 360f) * Mathf.Deg2Rad;
                Spawn(EnemyType.Swarmer, centre + new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r), 1f);
            }
        }

        // урон прошёл: над врагом число; выживший белеет и вздрагивает прочь от точки from. true = враг убит
        bool Hurt(int i, float damage, Vector2 from)
        {
            Enemy e = live[i];
            e.hp -= damage;
            e.hurt = true;
            if (e.maxHp >= config.numberMinHp) fx.Number(Above(e), damage, NumberKind.Normal);
            if (e.hp <= 0f)
            {
                Kill(i);
                return true;
            }

            fx.Flash(e.flash);
            fx.Recoil(e.body, (e.position - from).normalized, config.recoilDistance);
            return false;
        }

        // точка над врагом: отсюда всплывает число урона
        static Vector2 Above(Enemy e)
        {
            return e.position + Vector2.up * e.def.radius;
        }

        void Kill(int i)
        {
            Enemy e = live[i];
            crystals.Spawn(e.position, e.def.crystals);
            // большой враг: осколков больше и экран вздрагивает
            bool big = e.def.radius >= config.bigRadius;
            fx.Shards(e.position, big ? config.killShards * config.bigKillShardMul : config.killShards);
            if (big) fx.Shake(config.bigKillShake, config.bigKillShakeTime);
            // мелочь гибнет без остановки, иначе стая Роя превращается в слайд-шоу
            if (e.def.hp >= config.hitStopMinBaseHp) fx.HitStop(config.hitStopTime);
            sfx.Play(SfxId.Kill);
            Remove(i);
        }

        // на место убранного встаёт последний живой; при обходе с конца он уже обработан
        void Remove(int i)
        {
            Enemy e = live[i];
            e.visual.gameObject.SetActive(false);
            count--;
            live[i] = live[count];

            int t = (int)e.def.id;
            free[t][freeCount[t]++] = e;
        }

        Enemy Create(EnemyDef def)
        {
            Visual visual = Instantiate(art.enemies[(int)def.id], root);
            visual.gameObject.SetActive(false);

            var e = new Enemy { visual = visual, transform = visual.transform, def = def, hitCooldown = new float[config.maxSatellites] };
            e.body = visual.Body.transform;
            e.flash = fx.Overlay(visual);
            e.hpRing = visual.GetComponentInChildren<HpRing>(true);
            e.cracks = visual.GetComponentInChildren<DamageStages>(true);
            if (def.behavior == EnemyBehavior.Attach) e.attachMarker = e.transform.Find(AttachMarkerName).gameObject;
            return e;
        }

        void Place(Enemy e)
        {
            e.transform.localPosition = e.position;
            // ось +Y картинки смотрит на звезду
            if (e.def.faceStar) e.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-e.position.y, -e.position.x) * Mathf.Rad2Deg - 90f);
            e.visual.SetAlpha(Mathf.Clamp01(e.age / config.spawnFadeIn));

            if (e.def.behavior == EnemyBehavior.Attach && e.attachMarker.activeSelf != e.engaged) e.attachMarker.SetActive(e.engaged);
        }

        // кольцо HP и трещины; кольцо есть только у врагов с большим запасом HP и зависит от настройки
        void Gauge(Enemy e, float dt)
        {
            float fraction = e.hp / e.maxHp;
            HpRingMode mode = (HpRingMode)loop.Save.Data.settings.hpRings;
            bool ring = e.maxHp >= config.hpRingMinHp && (mode == HpRingMode.Always || mode == HpRingMode.AfterHit && e.hurt);
            if (e.hpRing.gameObject.activeSelf != ring) e.hpRing.gameObject.SetActive(ring);
            if (ring) e.hpRing.Draw(e.def.radius + config.hpRingOffset, fraction, dt);
            e.cracks.Show(fraction);
        }

        public EnemyDef Def(EnemyType type)
        {
            EnemyDef[] defs = config.enemies;
            for (int i = 0; i < defs.Length; i++)
                if (defs[i].id == type) return defs[i];
            return null;
        }

        // круги попаданий в Scene view
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < count; i++) Gizmos.DrawWireSphere(live[i].position, live[i].def.radius);
        }
    }
}
