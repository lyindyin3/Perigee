using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Эффекты поверх поля: вспышка экрана, красная виньетка, разгон на старте, кольца уровня и удара по звезде,
    // кристаллы, летящие к счётчику пыли. Всё идёт по настоящему времени.
    public class ScreenFx : MonoBehaviour
    {
        // кольцо или разгон: растёт от from до to за time и гаснет
        class Grow
        {
            public Visual visual;
            public float start = float.NegativeInfinity;
            public float time;
            public float from;
            public float to;
        }

        class Flyer
        {
            public RectTransform rect;
            public Vector3 from;
            public float start;
            public bool live;
        }

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] Hud hud;
        [SerializeField] Camera cam;
        // белая картинка на весь экран; цвет вспышки задаёт код
        [SerializeField] Image screenFlash;
        [SerializeField] Image dangerVignette;
        [SerializeField] Visual warp;
        [SerializeField] Visual levelUpRing;
        [SerializeField] Visual starHitRing;
        // счётчик пыли в HUD: туда летят кристаллы
        [SerializeField] RectTransform dustCounter;
        // запас значков кристаллов: больше живых не бывает
        [SerializeField] RectTransform[] crystalFlies;

        Grow warpGrow;
        Grow levelGrow;
        Grow hitGrow;
        Flyer[] flyers;

        void Awake()
        {
            warpGrow = new Grow { visual = warp };
            levelGrow = new Grow { visual = levelUpRing };
            hitGrow = new Grow { visual = starHitRing };
            flyers = new Flyer[crystalFlies.Length];
            for (int i = 0; i < flyers.Length; i++) flyers[i] = new Flyer { rect = crystalFlies[i] };
            Clear(screenFlash);
            Clear(dangerVignette);
        }

        public void ResetRun()
        {
            for (int i = 0; i < flyers.Length; i++)
            {
                flyers[i].live = false;
                flyers[i].rect.gameObject.SetActive(false);
            }
        }

        // весь экран вспыхивает цветом color с прозрачностью alpha и гаснет за sec
        public void Flash(Color color, float alpha, float sec)
        {
            screenFlash.DOKill();
            color.a = alpha;
            screenFlash.color = color;
            screenFlash.DOFade(0f, sec).SetLink(screenFlash.gameObject);
        }

        // старт забега: полосы разгона растут от звезды и гаснут
        public void Warp()
        {
            Begin(warpGrow, config.warpFrom, config.warpTo, config.warpTime);
        }

        // новый уровень: золотое кольцо от звезды до края поля
        public void LevelUp()
        {
            Begin(levelGrow, loop.Star.Radius, config.levelUpRingRadius, config.levelUpRingTime);
        }

        // удар по звезде: красное кольцо от её края
        public void StarHit()
        {
            float r = loop.Star.Radius;
            Begin(hitGrow, r, r + config.starHitRingGrow, config.starHitRingTime);
        }

        // подобран кристалл: значок летит дугой из его точки к счётчику; если все значки в полёте, этот не летит
        public void CrystalFly(Vector2 world)
        {
            for (int i = 0; i < flyers.Length; i++)
            {
                Flyer f = flyers[i];
                if (f.live) continue;
                f.live = true;
                f.start = Time.unscaledTime;
                // холст поверх экрана: позиция UI = точка экрана в пикселях
                f.from = cam.WorldToScreenPoint(world);
                f.rect.position = f.from;
                f.rect.gameObject.SetActive(true);
                return;
            }
        }

        void Begin(Grow g, float from, float to, float time)
        {
            g.start = Time.unscaledTime;
            g.from = from;
            g.to = to;
            g.time = time;
            g.visual.gameObject.SetActive(true);
            Animate(g);
        }

        void Update()
        {
            Animate(warpGrow);
            Animate(levelGrow);
            Animate(hitGrow);
            Vignette();
            Fly();
        }

        void Animate(Grow g)
        {
            if (!g.visual.gameObject.activeSelf) return;
            float t = (Time.unscaledTime - g.start) / g.time;
            if (t >= 1f)
            {
                g.visual.gameObject.SetActive(false);
                return;
            }
            g.visual.SetSizeMul(Mathf.Lerp(g.from, g.to, t));
            g.visual.SetAlpha(1f - t);
        }

        // ниже порога HP края экрана пульсируют красным, ниже второго порога ярче
        void Vignette()
        {
            float hp = loop.Run.hp / loop.Stats.starMaxHp;
            float peak = hp < config.vignetteCriticalHp ? config.vignetteCriticalAlpha : hp < config.vignetteLowHp ? config.vignetteLowAlpha : 0f;
            float wave = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * config.vignetteHz * 2f * Mathf.PI);
            Color color = dangerVignette.color;
            color.a = peak * wave;
            dangerVignette.color = color;
        }

        void Fly()
        {
            Vector3 target = dustCounter.position;
            // высота дуги в пикселях холста переводится в пиксели экрана
            float arc = config.crystalFlyArc * dustCounter.lossyScale.y;
            for (int i = 0; i < flyers.Length; i++)
            {
                Flyer f = flyers[i];
                if (!f.live) continue;
                float t = (Time.unscaledTime - f.start) / config.crystalFlyTime;
                if (t >= 1f)
                {
                    f.live = false;
                    f.rect.gameObject.SetActive(false);
                    hud.PunchDust();
                    continue;
                }
                // кривая Безье через точку над серединой пути
                Vector3 mid = (f.from + target) * 0.5f + Vector3.up * arc;
                f.rect.position = Vector3.Lerp(Vector3.Lerp(f.from, mid, t), Vector3.Lerp(mid, target, t), t);
            }
        }

        static void Clear(Image image)
        {
            Color color = image.color;
            color.a = 0f;
            image.color = color;
        }
    }
}
