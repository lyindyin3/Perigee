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
            public GameObject attachMarker;
            public EnemyDef def;
            public Vector2 position;
            public float hp;
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
            e.hp = def.hp >= config.eraHpMinBaseHp ? def.hp * loop.Era.hpMul : def.hp;
            e.speedMul = speedMul;
            e.age = 0f;
            e.engaged = false;
            e.timer = 0f;
            System.Array.Clear(e.hitCooldown, 0, e.hitCooldown.Length);
            Place(e);
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
                    // броня: удар слабее брони урона не наносит; ВСПЛЕСК броню не замечает
                    if (damage < e.def.armor && !abilities.Active(AbilityId.Surge))
                    {
                        fx.Sparks(satellites.Position(k), config.clinkSparks);
                        fx.HitStop(config.clinkHitStop);
                        sfx.Play(SfxId.Clink);
                        continue;
                    }

                    e.hp -= damage;
                    if (e.hp <= 0f)
                    {
                        Kill(i);
                        break;
                    }
                    fx.Shards(satellites.Position(k), config.hitShards);
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

                if (e.def.id == EnemyType.Swarmer) e.hp = 0f;
                else e.hp -= damage;
                if (e.hp <= 0f) Kill(i);
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

                if (e.def.id == EnemyType.Swarmer || e.def.id == EnemyType.Arrow) e.hp = 0f;
                else e.hp -= damage;
                if (e.hp <= 0f)
                {
                    Kill(i);
                    continue;
                }

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

        void Kill(int i)
        {
            Enemy e = live[i];
            crystals.Spawn(e.position, e.def.crystals);
            fx.Shards(e.position, config.killShards);
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
