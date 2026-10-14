using UnityEngine;

namespace Perigee
{
    // События забега (GDD 5.2): на секундах eventTimes с шансом eventChance одно из ещё не сыгранных.
    // Бурю и прилив читают Stats и EnemySystem, Затмение включает тьму EraRuleSystem, Нашествие ставит Director.
    public class RunEventSystem : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] EnemySystem enemies;
        [SerializeField] Director director;
        [SerializeField] EraRuleSystem eraRule;
        [SerializeField] BannerView banner;
        [SerializeField] AudioManager sfx;
        // золотые лучи поверх фона, пока идёт буря
        [SerializeField] GameObject stormFx;

        // бит на каждый RunEventType: уже было в этом забеге
        int played;
        // номер следующей секунды события: eventTimes по кругу, в Бесконечном каждый круг на цикл позже
        int next;
        float left;
        int meteorsLeft;
        float meteorTimer;
        float meteorAngle;

        // событие идёт: его имя стоит под таймером
        public bool Active => left > 0f;
        public RunEventType Current { get; private set; }

        public bool Storm => Active && Current == RunEventType.Storm;

        // Кристальный прилив: во столько раз больше кристаллов за убийство
        public int CrystalMul => Active && Current == RunEventType.CrystalTide ? config.tideCrystalMul : 1;

        public void ResetRun()
        {
            played = 0;
            next = 0;
            left = 0f;
            meteorsLeft = 0;
            stormFx.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (Active)
            {
                left -= dt;
                if (!Active) End();
            }
            Meteors(dt);

            RunState run = loop.Run;
            if (run.tutorial) return;
            float at = NextTime();
            if (run.time < at) return;

            next++;
            // в Забеге, Дне и испытании после прихода босса событий нет
            if (run.mode != RunMode.Endless && at >= loop.BossTime) return;
            Rng rng = director.SpawnRng;
            if (rng.Range(0f, 1f) >= config.eventChance) return;

            // равные шансы среди не сыгранных; Затмения не бывает там, где тьма и так всегда
            int count = 0;
            for (int t = 0; t < Types; t++)
                if (Open(t)) count++;
            if (count == 0) return;
            int pick = rng.Range(0, count);
            for (int t = 0; t < Types; t++)
            {
                if (!Open(t)) continue;
                if (pick-- == 0)
                {
                    Begin((RunEventType)t);
                    return;
                }
            }
        }

        static readonly int Types = System.Enum.GetValues(typeof(RunEventType)).Length;

        bool Open(int type)
        {
            if ((played & 1 << type) != 0) return false;
            return type != (int)RunEventType.Eclipse || loop.Era.rule != EraRule.Darkness;
        }

        float NextTime()
        {
            float[] times = config.eventTimes;
            if (loop.Run.mode != RunMode.Endless && next >= times.Length) return float.PositiveInfinity;
            return times[next % times.Length] + config.endlessLoopTime * (next / times.Length);
        }

        void Begin(RunEventType type)
        {
            played |= 1 << (int)type;
            Current = type;
            int i = (int)type;
            banner.Show(string.Format(Strings.EventBannerFormat, Strings.EventTitle[i], Strings.EventText[i]), null, Color.white, config.bannerEventHold);
            sfx.Play(SfxId.EventStart);

            Rng rng = director.SpawnRng;
            switch (type)
            {
                case RunEventType.MeteorShower:
                    meteorsLeft = config.meteorCount;
                    meteorTimer = 0f;
                    meteorAngle = rng.Range(0f, 360f);
                    left = config.meteorCount * config.meteorInterval;
                    break;
                case RunEventType.GoldenComet:
                    // скорость подобрана так, чтобы хорда внутри круга появления заняла goldCometTime
                    float r = config.spawnRadius;
                    float chord = 2f * Mathf.Sqrt(r * r - config.goldCometMiss * config.goldCometMiss);
                    enemies.Spawn(EnemyType.GoldComet, Polar(rng.Range(0f, 360f), r), chord / (config.goldCometTime * enemies.Def(EnemyType.GoldComet).speed));
                    left = config.goldCometTime;
                    break;
                case RunEventType.Storm:
                    left = config.stormTime;
                    stormFx.SetActive(true);
                    break;
                case RunEventType.CrystalTide:
                    left = config.tideTime;
                    break;
                case RunEventType.Invasion:
                    director.Invade();
                    // само нашествие мгновенное: имя под таймером держится, пока виден баннер
                    left = config.bannerEventHold;
                    break;
                case RunEventType.Eclipse:
                    left = config.eclipseTime;
                    eraRule.Eclipse(true);
                    break;
            }
        }

        void End()
        {
            if (Current == RunEventType.Storm) stormFx.SetActive(false);
            if (Current == RunEventType.Eclipse) eraRule.Eclipse(false);
        }

        // метеоры по одному раз в meteorInterval, все с одной стороны
        void Meteors(float dt)
        {
            if (meteorsLeft == 0) return;
            meteorTimer -= dt;
            while (meteorTimer <= 0f && meteorsLeft > 0)
            {
                meteorTimer += config.meteorInterval;
                meteorsLeft--;
                float angle = meteorAngle + director.SpawnRng.Range(-config.meteorSpread, config.meteorSpread);
                enemies.Spawn(EnemyType.Meteor, Polar(angle, config.spawnRadius), 1f);
            }
        }

        static Vector2 Polar(float angle, float r)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
        }
    }
}
