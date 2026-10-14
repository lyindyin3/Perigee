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
        // замедление времени симуляции до настоящей секунды slowEnd
        float slowScale = 1f;
        float slowEnd;
        // наезд камеры: сдвиг к точке и множитель размера кадра
        Vector2 focus;
        float zoom = 1f;

        public bool HitStopped => Time.unscaledTime < hitStopEnd;

        // множитель шага симуляции (GameLoop.FxTimeScale): 1, пока нет замедления
        public float TimeScale => Time.unscaledTime < slowEnd ? slowScale : 1f;

        // множитель orthographicSize камеры
        public float Zoom => zoom;

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

            // замедление и наезд прошлого забега не переходят в новый
            slowEnd = 0f;
            DOTween.Kill(this);
            focus = Vector2.zero;
            zoom = 1f;

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

        // крит: hit-stop длиннее на sec, даже если идёт другой
        public void ExtraStop(float sec)
        {
            float now = Time.unscaledTime;
            hitStopEnd = Mathf.Max(hitStopEnd, now) + sec;
        }

        // время симуляции идёт со скоростью scale sec настоящих секунд
        public void Slow(float scale, float sec)
        {
            slowScale = scale;
            slowEnd = Time.unscaledTime + sec;
        }

        // камера за sec сдвигается на offset и меняет размер кадра в mul раз; (0, 1) = обратно
        public void Focus(Vector2 offset, float mul, float sec)
        {
            DOTween.Kill(this);
            DOTween.To(() => focus, v => focus = v, offset, sec).SetTarget(this).SetLink(gameObject);
            DOTween.To(() => zoom, v => zoom = v, mul, sec).SetTarget(this).SetLink(gameObject);
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
            Emit(shards, art.shard, position, amount, config.shardSpeed, gold, Random.Range(0f, 360f), 360f);
        }

        // осколки удара: конусом hitShardCone по направлению от хиттера к врагу
        public void Shards(Vector2 position, int amount, Vector2 direction)
        {
            float cone = config.hitShardCone;
            float start = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - cone * 0.5f;
            // по краям конуса тоже летят осколки: шаг делится на amount − 1
            Emit(shards, art.shard, position, amount, config.shardSpeed, false, start, amount > 1 ? cone * amount / (amount - 1) : 0f);
        }

        // искры: удар не пробил броню; gold = золотые (появилась элита)
        public void Sparks(Vector2 position, int amount, bool gold = false)
        {
            Emit(sparks, art.spark, position, amount, config.sparkSpeed, gold, Random.Range(0f, 360f), 360f);
        }

        // число урона: всплывает от position и гаснет
        public void Number(Vector2 position, float value, NumberKind kind)
        {
            bool crit = kind == NumberKind.Crit;
            if (crit) ExtraStop(config.critHitStop);
            if (!loop.Save.Data.settings.damageNumbers) return;

            Popup n = numbers[nextNumber];
            nextNumber = (nextNumber + 1) % numbers.Length;

            // десятые видны, только когда они есть: «3», «1.5»; крит с восклицательным знаком
            int tenths = Mathf.RoundToInt(value * Tenths);
            if (tenths % Tenths == 0) n.text.SetText(crit ? "{0}!" : "{0}", tenths / Tenths);
            else n.text.SetText(crit ? "{0}.{1}!" : "{0}.{1}", tenths / Tenths, tenths % Tenths);

            n.color = crit ? config.numberCritColor : kind == NumberKind.Blocked ? config.numberBlockedColor : n.baseColor;
            // чем больше урон, тем крупнее число
            float scale = config.numberScale * (1f + config.numberScalePerDamage * Mathf.Min(value, config.numberScaleMaxDamage));
            if (crit) scale *= config.numberCritScale;
            n.transform.DOKill();
            n.transform.localScale = n.baseScale * scale;
            // крит подпрыгивает
            if (crit) n.transform.DOPunchScale(n.transform.localScale * config.critNumberPunch, config.critNumberPunchTime).SetLink(n.text.gameObject);

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
            cam.localPosition = camBase + (Vector3)(focus + offset);
        }

        // разлёт поровну по дуге arc от угла start; зерно симуляции эффекты не трогают
        void Emit(Pool pool, Visual prefab, Vector2 position, int amount, float speed, bool gold, float start, float arc)
        {
            for (int k = 0; k < amount; k++)
            {
                if (pool.live == pool.items.Count) pool.items.Add(Create(prefab));
                Piece p = pool.items[pool.live++];

                float angle = start + k * arc / amount;
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
