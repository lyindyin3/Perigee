using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Perigee
{
    public enum NumberKind { Normal, Crit, Blocked }

    // Hit-stop и тряска идут по настоящему времени, осколки, искры и числа урона двигает шаг симуляции.
    public class Fx : MonoBehaviour
    {
        // обязательная часть префаба врага и босса: белая копия Body для вспышки
        const string FlashOverlayName = "FlashOverlay";
        const int Tenths = 10;

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

        class Popup
        {
            public TMP_Text text;
            public Transform transform;
            // цвет и масштаб из префаба
            public Color baseColor;
            public Vector3 baseScale;
            public Color color;
            public Vector2 position;
            public float age;
            public bool live;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] Transform cam;
        [SerializeField] Transform root;

        readonly Pool shards = new Pool();
        readonly Pool sparks = new Pool();
        // числа урона идут по кругу: когда запас кончился, берётся самое старое
        Popup[] numbers;
        int nextNumber;
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

            numbers = new Popup[config.numberPool];
            nextNumber = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                TMP_Text text = Instantiate(art.damageNumber, root);
                text.gameObject.SetActive(false);
                numbers[i] = new Popup { text = text, transform = text.transform, baseColor = text.color, baseScale = text.transform.localScale };
            }
        }

        public void Tick(float dt)
        {
            Move(shards, config.shardTime, dt);
            Move(sparks, config.sparkTime, dt);
            MoveNumbers(dt);
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
            if (!loop.Save.Data.settings.shake) return;

            float now = Time.unscaledTime;
            if (now - shakeStart < config.shakeMinInterval) return;
            shakeStart = now;
            shakeAmp = amp;
            shakeTime = dur;
        }

        // осколки: от точки удара или от убитого врага; gold = золотые (гибель элиты)
        public void Shards(Vector2 position, int amount, bool gold = false)
        {
            Emit(shards, art.shard, position, amount, config.shardSpeed, gold);
        }

        // искры: удар не пробил броню; gold = золотые (появилась элита)
        public void Sparks(Vector2 position, int amount, bool gold = false)
        {
            Emit(sparks, art.spark, position, amount, config.sparkSpeed, gold);
        }

        // число урона: всплывает от position и гаснет
        public void Number(Vector2 position, float value, NumberKind kind)
        {
            if (!loop.Save.Data.settings.damageNumbers) return;

            Popup n = numbers[nextNumber];
            nextNumber = (nextNumber + 1) % numbers.Length;

            // десятые видны, только когда они есть: «3», «1.5»; крит с восклицательным знаком
            int tenths = Mathf.RoundToInt(value * Tenths);
            bool crit = kind == NumberKind.Crit;
            if (tenths % Tenths == 0) n.text.SetText(crit ? "{0}!" : "{0}", tenths / Tenths);
            else n.text.SetText(crit ? "{0}.{1}!" : "{0}.{1}", tenths / Tenths, tenths % Tenths);

            n.color = crit ? config.numberCritColor : kind == NumberKind.Blocked ? config.numberBlockedColor : n.baseColor;
            // чем больше урон, тем крупнее число
            float scale = config.numberScale * (1f + config.numberScalePerDamage * Mathf.Min(value, config.numberScaleMaxDamage));
            if (crit) scale *= config.numberCritScale;
            n.transform.localScale = n.baseScale * scale;

            n.position = position + Vector2.right * Random.Range(-config.numberJitter, config.numberJitter);
            n.age = 0f;
            n.live = true;
            n.transform.localPosition = n.position;
            n.text.color = n.color;
            n.text.gameObject.SetActive(true);
        }

        // белая копия Body для вспышки; картинку берёт у Body, чтобы владелец менял один спрайт
        public SpriteRenderer Overlay(Visual visual)
        {
            SpriteRenderer overlay = visual.Body.transform.Find(FlashOverlayName).GetComponent<SpriteRenderer>();
            overlay.sprite = visual.Body.sprite;
            return overlay;
        }

        // удар прошёл: враг на миг белеет
        public void Flash(SpriteRenderer overlay)
        {
            overlay.DOKill();
            Color color = overlay.color;
            color.a = config.hitFlashAlpha;
            overlay.color = color;
            overlay.DOFade(0f, config.hitFlashTime).SetLink(overlay.gameObject);
        }

        // отдача: вздрагивает Body, а не корень, поэтому позиция врага остаётся точной
        public void Recoil(Transform body, Vector2 direction, float distance)
        {
            // прошлый толчок доводим до конца, иначе сдвиг накапливается
            body.DOKill(true);
            body.DOPunchPosition(body.parent.InverseTransformDirection(direction) * distance, config.recoilTime).SetLink(body.gameObject);
        }

        void Update()
        {
            // тряска затухает к нулю
            float left = shakeTime - (Time.unscaledTime - shakeStart);
            Vector2 offset = left > 0f ? Random.insideUnitCircle * (shakeAmp * left / shakeTime) : Vector2.zero;
            cam.localPosition = camBase + (Vector3)offset;
        }

        void Emit(Pool pool, Visual prefab, Vector2 position, int amount, float speed, bool gold)
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
                p.visual.Tint(config.eliteGold, gold ? 1f : 0f);
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

        void MoveNumbers(float dt)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                Popup n = numbers[i];
                if (!n.live) continue;

                n.age += dt;
                if (n.age >= config.numberTime)
                {
                    n.live = false;
                    n.text.gameObject.SetActive(false);
                    continue;
                }

                float t = n.age / config.numberTime;
                n.transform.localPosition = n.position + Vector2.up * (config.numberRise * t);
                // число гаснет в конце жизни, начиная с доли numberFadeFrom
                Color color = n.color;
                color.a *= Mathf.Clamp01((1f - t) / (1f - config.numberFadeFrom));
                n.text.color = color;
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
