using UnityEngine;

namespace Perigee
{
    public class CrystalSystem : MonoBehaviour
    {
        class Crystal
        {
            public Visual visual;
            public Transform transform;
            public Vector2 position;
            public float age;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] Transform root;

        // живые кристаллы лежат в начале массива: [0, count), за ними запас выключенных: [count, created)
        Crystal[] pool;
        int count;
        int created;

        float Life => config.crystalLife * loop.Stats.crystalLifeMul;

        void Awake()
        {
            pool = new Crystal[config.maxCrystals];
        }

        public void ResetRun()
        {
            // пул собирается заново из префаба: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            count = 0;
            created = 0;
            while (created < config.crystalPrewarm) Create();
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

                if (count == created) Create();
                Crystal c = pool[count++];
                c.position = centre + new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
                c.age = 0f;
                Place(c, life);
                c.visual.gameObject.SetActive(true);
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
                    // оглушённый или ещё не появившийся спутник кристаллы не берёт и не притягивает
                    if (!satellites.Active(k)) continue;
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

        void Create()
        {
            Visual visual = Instantiate(art.crystal, root);
            visual.gameObject.SetActive(false);
            pool[created++] = new Crystal { visual = visual, transform = visual.transform };
        }

        // на место убранного встаёт последний живой; при обходе с конца он уже обработан
        void Remove(int i)
        {
            Crystal c = pool[i];
            c.visual.gameObject.SetActive(false);
            count--;
            pool[i] = pool[count];
            pool[count] = c;
        }

        void Place(Crystal c, float life)
        {
            c.transform.localPosition = c.position;
            // в последние секунды жизни кристалл мигает
            bool visible = life - c.age > config.crystalBlinkTime || Mathf.Repeat(c.age * config.crystalBlinkHz, 1f) < 0.5f;
            c.visual.SetAlpha(visible ? 1f : 0f);
        }

        // круги попаданий в Scene view
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < count; i++) Gizmos.DrawWireSphere(pool[i].position, config.crystalRadius);
        }
    }
}
