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
            // пока лежит: секунды с появления; после подбора: секунды хлопка
            public float age;
            public bool picked;
            // ЖАТВА: летит к ближайшему хиттеру с любого расстояния
            public bool harvested;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] StarController star;
        [SerializeField] ComboSystem combo;
        [SerializeField] AudioManager sfx;
        [SerializeField] ScreenFx screenFx;
        [SerializeField] Transform root;

        // живые кристаллы лежат в начале массива: [0, count), за ними запас выключенных: [count, created)
        Crystal[] pool;
        int count;
        int created;
        // дробная пыль от множителя комбо копится до целой
        float dustBank;
        float harvestTimer;

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
            dustBank = 0f;
            harvestTimer = 0f;
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
                c.picked = false;
                c.harvested = false;
                c.visual.SetSizeMul(1f);
                Place(c, life);
                c.visual.gameObject.SetActive(true);
            }
        }

        public void Tick(float dt)
        {
            float life = Life;
            float pickup = satellites.HitterRadius + config.crystalRadius;
            float magnet = loop.Stats.magnetRadius;
            int hitters = satellites.HitterCount;
            // Тяжёлая звезда: кристаллы тонут в ней; ГРАВИЗАХВАТ их держит
            bool sink = loop.Era.rule == EraRule.Heavy && !loop.Stats.AnchorField;
            // ЖАТВА: раз в harvestInterval все лежащие кристаллы летят к ближайшему хиттеру
            bool harvest = false;
            if (loop.Stats.Has(StatId.Harvest))
            {
                harvestTimer += dt;
                harvest = harvestTimer >= config.harvestInterval;
                if (harvest) harvestTimer -= config.harvestInterval;
            }

            for (int i = count - 1; i >= 0; i--)
            {
                Crystal c = pool[i];
                c.age += dt;
                if (c.picked)
                {
                    // хлопок: подобранный кристалл раздувается и гаснет
                    float t = c.age / config.crystalPopTime;
                    if (t >= 1f)
                    {
                        Remove(i);
                        continue;
                    }
                    c.visual.SetSizeMul(Mathf.Lerp(1f, config.crystalPopScale, t));
                    c.visual.SetAlpha(1f - t);
                    continue;
                }
                if (c.age >= life)
                {
                    Remove(i);
                    continue;
                }
                if (harvest) c.harvested = true;

                int nearest = 0;
                float nearestSq = float.MaxValue;
                for (int k = 0; k < hitters; k++)
                {
                    // оглушённый или ещё не появившийся спутник и его тень кристаллы не берут и не притягивают
                    if (!satellites.HitterActive(k)) continue;
                    float sq = (satellites.HitterPosition(k) - c.position).sqrMagnitude;
                    if (sq >= nearestSq) continue;
                    nearestSq = sq;
                    nearest = k;
                }

                // все хиттеры оглушены: ЖАТВЕ некуда тянуть
                if (nearestSq <= magnet * magnet || c.harvested && nearestSq < float.MaxValue)
                {
                    Vector2 target = satellites.HitterPosition(nearest);
                    c.position = Vector2.MoveTowards(c.position, target, config.magnetSpeed * dt);
                    nearestSq = (target - c.position).sqrMagnitude;
                }
                else if (sink)
                {
                    // тонет только кристалл, который не тянет магнит; дойдя до звезды, пропадает
                    float r = c.position.magnitude;
                    float next = r - config.crystalSink * dt;
                    if (next <= loop.Star.Radius)
                    {
                        Remove(i);
                        continue;
                    }
                    c.position *= next / r;
                }

                if (nearestSq <= pickup * pickup)
                {
                    // комбо умножает опыт и пыль за кристалл в момент подбора
                    float mul = combo.Mul;
                    dustBank += mul;
                    int whole = (int)dustBank;
                    dustBank -= whole;
                    loop.Run.dust += whole;
                    loop.Run.xp += loop.Stats.xpPerCrystalMul * mul;
                    Tide();
                    c.picked = true;
                    c.age = 0f;
                    sfx.Play(SfxId.Crystal);
                    screenFx.CrystalFly(c.position);
                    continue;
                }
                Place(c, life);
            }
        }

        // ПРИЛИВ (и КРИСТАЛЛ ЖИЗНИ): каждые tideEvery подобранных кристаллов лечат звезду за каждый стак
        void Tide()
        {
            RunState run = loop.Run;
            run.crystalsPicked++;
            float stacks = loop.Stats.Sum(StatId.TideHeal);
            if (stacks > 0f && run.crystalsPicked % config.tideEvery == 0) star.Heal(config.tideHeal * stacks);
        }

        // колодец аномалии: лежащие кристаллы в круге radius сдвигаются к centre на step
        public void Pull(Vector2 centre, float radius, float step)
        {
            for (int i = 0; i < count; i++)
            {
                Crystal c = pool[i];
                if (c.picked || (c.position - centre).sqrMagnitude > radius * radius) continue;
                c.position = Vector2.MoveTowards(c.position, centre, step);
            }
        }

        // ответ Nearest, когда кристаллов рядом нет

        public const int None = -1;

        // номер ближайшего лежащего кристалла не дальше radius от точки; годен, пока ни один кристалл не убран
        public int Nearest(Vector2 from, float radius)
        {
            int nearest = None;
            float nearestSq = radius * radius;
            for (int i = 0; i < count; i++)
            {
                if (pool[i].picked) continue;
                float sq = (pool[i].position - from).sqrMagnitude;
                if (sq > nearestSq) continue;
                nearestSq = sq;
                nearest = i;
            }
            return nearest;
        }

        public Vector2 Position(int i)
        {
            return pool[i].position;
        }

        // Пожиратель съел кристалл: ни пыли, ни опыта
        public void Eat(int i)
        {
            Remove(i);
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
