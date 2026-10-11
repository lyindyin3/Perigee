using UnityEngine;

namespace Perigee
{
    public class EnemySystem : MonoBehaviour
    {
        class Enemy
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public EnemyDef def;
            public Vector2 position;
            public float hp;
            public float speedMul;
            public float age;
            public float[] hitCooldown;
        }

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] StarController star;
        [SerializeField] Transform root;

        // живые враги лежат в начале массива: [0, count)
        Enemy[] pool;
        int count;

        void Awake()
        {
            pool = new Enemy[config.maxEnemies];
            Sprite sprite = ShapeFactory.Triangle();
            for (int i = 0; i < pool.Length; i++)
            {
                SpriteRenderer renderer = ShapeFactory.NewRenderer("Enemy", root, sprite, config.red, config.sortEnemies, config.spriteMaterial);
                renderer.gameObject.SetActive(false);
                pool[i] = new Enemy { transform = renderer.transform, renderer = renderer, hitCooldown = new float[config.maxSatellites] };
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
            System.Array.Clear(e.hitCooldown, 0, e.hitCooldown.Length);
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

                e.position = Vector2.MoveTowards(e.position, Vector2.zero, e.def.speed * e.speedMul * dt);

                float reach = config.starRadius + e.def.radius;
                if (e.position.sqrMagnitude <= reach * reach)
                {
                    star.Damage(e.def.starDamage);
                    Remove(i);
                    continue;
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

                float reach = config.satRadius + e.def.radius;
                for (int k = 0; k < satellites.Count; k++)
                {
                    if (e.hitCooldown[k] > 0f) continue;
                    if ((satellites.Position(k) - e.position).sqrMagnitude > reach * reach) continue;

                    e.hitCooldown[k] = config.hitCooldown;
                    float damage = satellites.Damage(k);
                    // броня: удар слабее брони урона не наносит
                    if (damage < e.def.armor) continue;

                    e.hp -= damage;
                    if (e.hp <= 0f)
                    {
                        Remove(i);
                        break;
                    }
                }
            }
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

        void Place(Enemy e)
        {
            e.transform.localPosition = e.position;
            // вершина треугольника смотрит на звезду
            e.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-e.position.y, -e.position.x) * Mathf.Rad2Deg - 90f);
            e.transform.localScale = new Vector3(e.def.radius, e.def.radius, 1f);

            Color color = config.red;
            color.a = Mathf.Clamp01(e.age / config.spawnFadeIn);
            e.renderer.color = color;
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
