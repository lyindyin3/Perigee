using UnityEngine;

namespace Perigee
{
    public class EnemySystem : MonoBehaviour
    {
        class Enemy
        {
            public Transform transform;
            public SpriteRenderer body;
            public SpriteRenderer detail;
            public SpriteRenderer marker;
            public Color detailColor;
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
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] StarController star;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] Transform root;

        // живые враги лежат в начале массива: [0, count)
        Enemy[] pool;
        int count;

        Sprite triangle;
        Sprite spike;
        Sprite tail;
        Sprite diamond;
        Sprite bastion;
        Sprite bastionTrim;
        Sprite circle;
        Sprite legs;
        Sprite shooter;
        Sprite rect;
        Sprite hive;

        void Awake()
        {
            triangle = ShapeFactory.Triangle();
            spike = ShapeFactory.Spike();
            tail = ShapeFactory.Stripes(3, config.arrowTailFill);
            diamond = ShapeFactory.Diamond();
            bastion = ShapeFactory.Bastion(config.bastionTrim, config.bastionRivetLength, config.bastionRivetWidth, config.bastionRivetStep);
            bastionTrim = ShapeFactory.Wedge(0f, config.bastionTrim);
            circle = ShapeFactory.Circle();
            legs = ShapeFactory.Rays(6, config.parasiteBody, config.parasiteLegFill);
            shooter = ShapeFactory.Circle(0f, config.shooterWall);
            rect = ShapeFactory.Rect();
            hive = ShapeFactory.Hive(config.hiveHoleRadius, config.hiveHoleOffset);
            Sprite markerRing = ShapeFactory.Circle(0f, config.parasiteMarkerWall);

            pool = new Enemy[config.maxEnemies];
            for (int i = 0; i < pool.Length; i++)
            {
                Transform enemy = new GameObject("Enemy").transform;
                enemy.SetParent(root, false);
                pool[i] = new Enemy
                {
                    transform = enemy,
                    body = ShapeFactory.NewRenderer("Body", enemy, triangle, config.red, config.sortEnemies, config.spriteMaterial),
                    detail = ShapeFactory.NewRenderer("Detail", enemy, triangle, config.red, config.sortEnemies, config.spriteMaterial),
                    marker = ShapeFactory.NewRenderer("Marker", enemy, markerRing, config.red, config.sortFx, config.spriteMaterial),
                    hitCooldown = new float[config.maxSatellites],
                };
                enemy.gameObject.SetActive(false);
            }
        }

        public void ResetRun()
        {
            for (int i = 0; i < count; i++) pool[i].transform.gameObject.SetActive(false);
            count = 0;
        }

        public void Spawn(EnemyType type, Vector2 position, float speedMul)
        {
            if (count == pool.Length) return;

            EnemyDef def = Def(type);
            Enemy e = pool[count++];
            e.def = def;
            e.position = position;
            // эра усиливает только врагов с базовым hp от eraHpMinBaseHp
            e.hp = def.hp >= config.eraHpMinBaseHp ? def.hp * loop.Era.hpMul : def.hp;
            e.speedMul = speedMul;
            e.age = 0f;
            e.engaged = false;
            e.timer = 0f;
            System.Array.Clear(e.hitCooldown, 0, e.hitCooldown.Length);
            Dress(e);
            Place(e);
            e.transform.gameObject.SetActive(true);
        }

        public void Tick(float dt)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = pool[i];
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
                            star.Damage(Mathf.Min(config.parasiteDrainBase + config.parasiteDrainGrowth * e.timer, config.parasiteDrainMax) * dt);
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
                Enemy e = pool[i];
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
                    if (damage < e.def.armor && !abilities.Active(AbilityId.Surge)) continue;

                    e.hp -= damage;
                    if (e.hp <= 0f)
                    {
                        Kill(i);
                        break;
                    }
                }
            }
        }

        // волна от звезды: Рой гибнет сразу, остальные получают урон
        public void Blast(float radius, float damage)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = pool[i];
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
                Enemy e = pool[i];
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
            Vector2 centre = pool[i].position;
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
            crystals.Spawn(pool[i].position, pool[i].def.crystals);
            Remove(i);
        }

        // на место убранного встаёт последний живой; при обходе с конца он уже обработан
        void Remove(int i)
        {
            Enemy e = pool[i];
            e.transform.gameObject.SetActive(false);
            count--;
            pool[i] = pool[count];
            pool[count] = e;
        }

        // Собирает вид врага из тела и детали. Локальная ось Y врага смотрит на звезду.
        void Dress(Enemy e)
        {
            float r = e.def.radius;
            e.body.transform.localRotation = Quaternion.identity;
            e.body.transform.localScale = new Vector3(r, r, 1f);
            e.detail.enabled = false;
            e.marker.enabled = false;

            switch (e.def.id)
            {
                case EnemyType.Swarmer:
                    e.body.sprite = triangle;
                    break;

                case EnemyType.Arrow:
                    e.body.sprite = spike;
                    e.body.transform.localScale = new Vector3(config.arrowWidth * 0.5f, config.arrowLength * 0.5f, 1f);
                    // три линии скорости позади
                    Detail(e, tail, config.red, -(config.arrowLength + config.arrowTailLength) * 0.5f, config.arrowWidth * 0.5f, config.arrowTailLength * 0.5f);
                    break;

                case EnemyType.Bolt:
                    e.body.sprite = diamond;
                    break;

                case EnemyType.Bastion:
                    e.body.sprite = bastion;
                    Detail(e, bastionTrim, config.silver, 0f, r, r);
                    // у клина вершина снизу, а смотреть она должна на звезду
                    e.body.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                    e.detail.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                    break;

                case EnemyType.Parasite:
                    e.body.sprite = circle;
                    e.body.transform.localScale = new Vector3(r * config.parasiteBody, r * config.parasiteBody, 1f);
                    Detail(e, legs, config.red, 0f, r, r);
                    break;

                case EnemyType.Shooter:
                    e.body.sprite = shooter;
                    // ствол торчит из кольца в сторону звезды
                    Detail(e, rect, config.red, r, config.shooterBarrelWidth * 0.5f, config.shooterBarrelLength * 0.5f);
                    break;

                case EnemyType.Hive:
                    e.body.sprite = hive;
                    break;
            }
        }

        void Detail(Enemy e, Sprite sprite, Color color, float offsetY, float halfWidth, float halfLength)
        {
            e.detail.sprite = sprite;
            e.detail.enabled = true;
            e.detailColor = color;
            e.detail.transform.localPosition = new Vector3(0f, offsetY, 0f);
            e.detail.transform.localRotation = Quaternion.identity;
            e.detail.transform.localScale = new Vector3(halfWidth, halfLength, 1f);
        }

        void Place(Enemy e)
        {
            e.transform.localPosition = e.position;
            e.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-e.position.y, -e.position.x) * Mathf.Rad2Deg - 90f);

            float alpha = Mathf.Clamp01(e.age / config.spawnFadeIn);
            Color color = config.red;
            color.a = alpha;
            e.body.color = color;
            color = e.detailColor;
            color.a = alpha;
            e.detail.color = color;

            if (e.def.behavior != EnemyBehavior.Attach) return;

            // пульсирующее красное кольцо вокруг присосавшегося Паразита
            float pulse = 1f + config.parasiteMarkerPulse * Mathf.Sin(e.timer * config.parasiteMarkerPulseHz * 2f * Mathf.PI);
            e.marker.enabled = e.engaged;
            e.marker.transform.localScale = new Vector3(config.parasiteMarkerRadius * pulse, config.parasiteMarkerRadius * pulse, 1f);
        }

        EnemyDef Def(EnemyType type)
        {
            EnemyDef[] defs = config.enemies;
            for (int i = 0; i < defs.Length; i++)
                if (defs[i].id == type) return defs[i];
            return null;
        }
    }
}
