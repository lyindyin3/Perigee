using System.Collections.Generic;
using UnityEngine;

namespace Perigee
{
    // Hit-stop и тряска идут по настоящему времени, осколки и искры двигает шаг симуляции.
    public class Fx : MonoBehaviour
    {
        class Piece
        {
            public Visual visual;
            public Transform transform;
            public Vector2 position;
            public Vector2 velocity;
            public float age;
        }

        class Pool
        {
            // живые лежат в начале списка: [0, live), за ними запас выключенных
            public readonly List<Piece> items = new List<Piece>();
            public int live;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] Transform cam;
        [SerializeField] Transform root;

        readonly Pool shards = new Pool();
        readonly Pool sparks = new Pool();
        Vector3 camBase;
        float hitStopStart = float.NegativeInfinity;
        float hitStopEnd;
        float shakeStart = float.NegativeInfinity;
        float shakeAmp;
        float shakeTime;

        public bool HitStopped => Time.unscaledTime < hitStopEnd;

        void Awake()
        {
            camBase = cam.localPosition;
        }

        public void ResetRun()
        {
            // пулы собираются заново из префабов: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            Prewarm(shards, art.shard, config.shardPrewarm);
            Prewarm(sparks, art.spark, config.sparkPrewarm);
        }

        public void Tick(float dt)
        {
            Move(shards, config.shardTime, dt);
            Move(sparks, config.sparkTime, dt);
        }

        public void HitStop(float sec)
        {
            float now = Time.unscaledTime;
            if (now - hitStopStart < config.hitStopMinInterval) return;
            hitStopStart = now;
            hitStopEnd = now + sec;
        }

        public void Shake(float amp, float dur)
        {
            float now = Time.unscaledTime;
            if (now - shakeStart < config.shakeMinInterval) return;
            shakeStart = now;
            shakeAmp = amp;
            shakeTime = dur;
        }

        // осколки: от точки удара или от убитого врага
        public void Shards(Vector2 position, int amount)
        {
            Emit(shards, art.shard, position, amount, config.shardSpeed);
        }

        // искры: удар не пробил броню
        public void Sparks(Vector2 position, int amount)
        {
            Emit(sparks, art.spark, position, amount, config.sparkSpeed);
        }

        void Update()
        {
            // тряска затухает к нулю
            float left = shakeTime - (Time.unscaledTime - shakeStart);
            Vector2 offset = left > 0f ? Random.insideUnitCircle * (shakeAmp * left / shakeTime) : Vector2.zero;
            cam.localPosition = camBase + (Vector3)offset;
        }

        void Emit(Pool pool, Visual prefab, Vector2 position, int amount, float speed)
        {
            // разлёт поровну по кругу от случайного угла; зерно симуляции эффекты не трогают
            float start = Random.Range(0f, 360f);
            for (int k = 0; k < amount; k++)
            {
                if (pool.live == pool.items.Count) pool.items.Add(Create(prefab));
                Piece p = pool.items[pool.live++];

                float angle = start + k * 360f / amount;
                float rad = angle * Mathf.Deg2Rad;
                p.position = position;
                p.velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
                p.age = 0f;
                p.transform.localPosition = position;
                // ось +Y картинки смотрит по ходу полёта
                p.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                p.visual.SetAlpha(1f);
                p.visual.gameObject.SetActive(true);
            }
        }

        void Move(Pool pool, float life, float dt)
        {
            for (int i = pool.live - 1; i >= 0; i--)
            {
                Piece p = pool.items[i];
                p.age += dt;
                if (p.age >= life)
                {
                    // на место убранного встаёт последний живой; при обходе с конца он уже обработан
                    p.visual.gameObject.SetActive(false);
                    pool.live--;
                    pool.items[i] = pool.items[pool.live];
                    pool.items[pool.live] = p;
                    continue;
                }

                p.position += p.velocity * dt;
                p.transform.localPosition = p.position;
                p.visual.SetAlpha(1f - p.age / life);
            }
        }

        void Prewarm(Pool pool, Visual prefab, int amount)
        {
            pool.items.Clear();
            pool.live = 0;
            for (int i = 0; i < amount; i++) pool.items.Add(Create(prefab));
        }

        Piece Create(Visual prefab)
        {
            Visual visual = Instantiate(prefab, root);
            visual.gameObject.SetActive(false);
            return new Piece { visual = visual, transform = visual.transform };
        }
    }
}
