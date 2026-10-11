using UnityEngine;

namespace Perigee
{
    public class CrystalSystem : MonoBehaviour
    {
        class Crystal
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 position;
            public float age;
        }

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] Transform root;

        // живые кристаллы лежат в начале массива: [0, count)
        Crystal[] pool;
        int count;

        float Life => config.crystalLife * loop.Stats.crystalLifeMul;

        void Awake()
        {
            pool = new Crystal[config.maxCrystals];
            Sprite sprite = ShapeFactory.Diamond();
            for (int i = 0; i < pool.Length; i++)
            {
                SpriteRenderer renderer = ShapeFactory.NewRenderer("Crystal", root, sprite, config.gold, config.sortCrystals, config.spriteMaterial);
                renderer.gameObject.SetActive(false);
                pool[i] = new Crystal { transform = renderer.transform, renderer = renderer };
            }
        }

        public void ResetRun()
        {
            for (int i = 0; i < count; i++) pool[i].transform.gameObject.SetActive(false);
            count = 0;
        }

        public void Spawn(Vector2 centre, int amount)
        {
            Rng rng = loop.Run.simRng;
            float life = Life;
            for (int k = 0; k < amount && count < pool.Length; k++)
            {
                // корень из случайного числа даёт равномерный разброс по площади круга
                float r = config.crystalSpread * Mathf.Sqrt(rng.Range(0f, 1f));
                float rad = rng.Range(0f, 360f) * Mathf.Deg2Rad;

                Crystal c = pool[count++];
                c.position = centre + new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
                c.age = 0f;
                Place(c, life);
                c.transform.gameObject.SetActive(true);
            }
        }

        public void Tick(float dt)
        {
            float life = Life;
            float pickup = satellites.Radius + config.crystalRadius;
            float magnet = loop.Stats.magnetRadius;

            for (int i = count - 1; i >= 0; i--)
            {
                Crystal c = pool[i];
                c.age += dt;
                if (c.age >= life)
                {
                    Remove(i);
                    continue;
                }

                int nearest = 0;
                float nearestSq = float.MaxValue;
                for (int k = 0; k < satellites.Count; k++)
                {
                    float sq = (satellites.Position(k) - c.position).sqrMagnitude;
                    if (sq >= nearestSq) continue;
                    nearestSq = sq;
                    nearest = k;
                }

                if (nearestSq <= magnet * magnet)
                {
                    Vector2 target = satellites.Position(nearest);
                    c.position = Vector2.MoveTowards(c.position, target, config.magnetSpeed * dt);
                    nearestSq = (target - c.position).sqrMagnitude;
                }

                if (nearestSq <= pickup * pickup)
                {
                    loop.Run.dust++;
                    loop.Run.xp++;
                    Remove(i);
                    continue;
                }
                Place(c, life);
            }
        }

        // на место убранного встаёт последний живой; при обходе с конца он уже обработан
        void Remove(int i)
        {
            Crystal c = pool[i];
            c.transform.gameObject.SetActive(false);
            count--;
            pool[i] = pool[count];
            pool[count] = c;
        }

        void Place(Crystal c, float life)
        {
            c.transform.localPosition = c.position;
            c.transform.localRotation = Quaternion.Euler(0f, 0f, c.age * config.crystalSpin);
            c.transform.localScale = new Vector3(config.crystalRadius, config.crystalRadius, 1f);
            // в последние секунды жизни кристалл мигает
            c.renderer.enabled = life - c.age > config.crystalBlinkTime || Mathf.Repeat(c.age * config.crystalBlinkHz, 1f) < 0.5f;
        }
    }
}
