using UnityEngine;

namespace Perigee
{
    // Реликвии (GDD 6.7): выбор после гибели Вестника и после каждого босса Бесконечного, стаки до предела,
    // и свойства, которым нужны часы и счётчики: ОСКОЛОК КОМЕТЫ, ЩИТ ПРЕДКОВ, ПЕСОЧНЫЕ ЧАСЫ, ПОСЛЕДНИЙ ВЗДОХ.
    // Остальные свойства идут через Stats, как у карточек.
    public class RelicSystem : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] StarController star;
        [SerializeField] RelicPickView view;
        [SerializeField] BannerView banner;
        [SerializeField] AudioManager sfx;

        Rng relicRng;
        // индексы в config.relics: [0, offerCount)
        int[] eligible;
        int[] offer;
        int offerCount;
        // сколько выборов ждут своего шага: гибель Вестника и босса бывает посреди шага
        int pending;
        // сколько боссов Бесконечного уже дали выбор
        int bossesRewarded;
        // ОСКОЛОК КОМЕТЫ: удары, которые прошли, по каждому хиттеру
        int[] strikes;
        // ЩИТ ПРЕДКОВ: с этой секунды забега следующий удар по звезде гасится
        float aegisReady;
        // ПЕСОЧНЫЕ ЧАСЫ: секунды до замирания и сколько ещё оно длится
        float hourglass;
        float stasisLeft;
        bool lastBreathUsed;

        public int OfferCount => offerCount;

        // ПЕСОЧНЫЕ ЧАСЫ: враги сейчас в замедленном времени
        public bool Stasis => stasisLeft > 0f;

        void Awake()
        {
            eligible = new int[config.relics.Length];
            offer = new int[config.relics.Length];
            strikes = new int[satellites.MaxHitters];
        }

        public void ResetRun(int seed)
        {
            relicRng = new Rng(seed);
            offerCount = 0;
            pending = 0;
            bossesRewarded = 0;
            System.Array.Clear(strikes, 0, strikes.Length);
            aegisReady = 0f;
            hourglass = config.autoStasisInterval;
            stasisLeft = 0f;
            lastBreathUsed = false;
        }

        public RelicDef Offer(int slot)
        {
            return config.relics[offer[slot]];
        }

        // сколько стаков предложенной реликвии уже взято
        public int Stacks(int slot)
        {
            return loop.Run.relicStacks[offer[slot]];
        }

        // Вестник погиб (не дойдя до звезды): выбор откроется в конце шага
        public void Herald()
        {
            pending++;
        }

        // ОСКОЛОК КОМЕТЫ: удар хиттера k прошёл; каждый thirdHitEvery-й получает прибавку
        public float Strike(int k)
        {
            Stats stats = loop.Stats;
            if (!stats.Has(StatId.ThirdHit)) return 0f;
            strikes[k]++;
            return strikes[k] % config.thirdHitEvery == 0 ? stats.Sum(StatId.ThirdHit) : 0f;
        }

        // ЩИТ ПРЕДКОВ: true = этот удар по звезде погашен
        public bool Aegis()
        {
            RunState run = loop.Run;
            if (!loop.Stats.Has(StatId.Aegis) || run.time < aegisReady) return false;
            aegisReady = run.time + config.aegisInterval;
            return true;
        }

        public void Tick(float dt)
        {
            Stats stats = loop.Stats;
            RunState run = loop.Run;
            // Бесконечный: за каждого убитого босса выбор
            if (run.bossKills > bossesRewarded)
            {
                pending += run.bossKills - bossesRewarded;
                bossesRewarded = run.bossKills;
            }

            // замирание идёт по обычному времени
            stasisLeft = Mathf.Max(stasisLeft - dt, 0f);
            if (stats.Has(StatId.AutoStasis))
            {
                hourglass -= dt;
                if (hourglass <= 0f)
                {
                    hourglass += config.autoStasisInterval;
                    stasisLeft = config.autoStasisTime;
                    sfx.Play(SfxId.Stasis);
                }
            }

            // ПОСЛЕДНИЙ ВЗДОХ: раз за забег, когда HP впервые ниже порога, волна ИМПУЛЬСА
            if (stats.Has(StatId.LastBreath) && !lastBreathUsed && run.hp > 0f && run.hp < stats.starMaxHp * config.lastBreathHp)
            {
                lastBreathUsed = true;
                abilities.PulseWave(config.lastBreathRadius, config.lastBreathDamage);
                sfx.Play(SfxId.Pulse);
            }

            // за шаг открывается один выбор, остальные ждут следующих шагов
            if (pending > 0 && loop.State == GameState.Playing)
            {
                pending--;
                Open();
            }
        }

        void Open()
        {
            RelicDef[] relics = config.relics;
            RunState run = loop.Run;
            int n = 0;
            for (int i = 0; i < relics.Length; i++)
                if (run.relicStacks[i] < relics[i].maxStacks) eligible[n++] = i;

            // все реликвии на пределе: пыль вместо выбора
            if (n == 0)
            {
                run.dustRelics += config.relicNoneDust;
                banner.Show(string.Format(Strings.BossDustFormat, config.relicNoneDust), null, Color.white, config.bannerRelicHold);
                return;
            }

            // равные шансы, без повторов на одном экране
            offerCount = Mathf.Min(config.relicChoices, n);
            for (int k = 0; k < offerCount; k++)
            {
                int pick = relicRng.Range(k, n);
                int index = eligible[pick];
                eligible[pick] = eligible[k];
                eligible[k] = index;
                offer[k] = index;
            }
            loop.SetState(GameState.CardPick);
            view.Show();
        }

        public void Pick(int slot)
        {
            int index = offer[slot];
            RelicDef relic = config.relics[index];
            loop.Run.relicStacks[index]++;
            loop.RecomputeStats();
            // СЕРДЦЕВИНА лечит на столько же, на сколько поднимает максимум (как STAR_PLATING)
            for (int m = 0; m < relic.mods.Length; m++)
                if (relic.mods[m].stat == StatId.StarMaxHp) star.Heal(relic.mods[m].value);
        }
    }
}
