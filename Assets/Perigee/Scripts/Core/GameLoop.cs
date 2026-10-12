using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Perigee
{
    public enum GameState { Menu, AbilityPick, Playing, Paused, CardPick, Defeat, Victory, Result }

    public class GameLoop : MonoBehaviour
    {
        // зерно дневного забега = дата UTC в таком виде
        const string DateFormat = "yyyyMMdd";

        [SerializeField] GameConfig config;
        [SerializeField] Camera cam;
        [SerializeField] InputHold input;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] EnemySystem enemies;
        [SerializeField] BossComet boss;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] Director director;
        [SerializeField] StarController star;
        [SerializeField] CardSystem cards;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] ArmorRing armorRing;
        [SerializeField] Fx fx;
        [SerializeField] AudioManager sfx;
        [SerializeField] Hud hud;
        [SerializeField] PauseView pauseView;
        [SerializeField] AbilityPickView abilityPickView;
        [SerializeField] MenuView menuView;
        [SerializeField] ResultView resultView;

        public GameState State { get; private set; }
        public Stats Stats { get; } = new Stats();
        public RunState Run { get; } = new RunState();
        public ISaveService Save { get; private set; }
        public Blueprints Blueprints { get; private set; }
        public EraConfig Era => config.eras[eraIndex];

        int eraIndex;
        bool daily;
        float accumulator;
        float victoryTimer;
        InputAction restartKey;
        InputAction pauseKey;

        void Awake()
        {
            Save = new SaveService(config.blueprints.Length);
            Blueprints = new Blueprints(config, Save);

            restartKey = new InputAction(binding: "<Keyboard>/r");
            restartKey.Enable();

            pauseKey = new InputAction(type: InputActionType.Button);
            pauseKey.AddBinding("<Keyboard>/escape");
            pauseKey.AddBinding("<Keyboard>/p");
            pauseKey.Enable();
        }

        void OnDestroy()
        {
            restartKey.Dispose();
            pauseKey.Dispose();
        }

        void Start()
        {
            // звезда и кольцо брони видны за меню и читают Run и Stats ещё до первого забега
            Run.Reset(config, 0);
            RecomputeStats();
            Run.hp = Stats.starMaxHp;
            ToMenu();
        }

        // из паузы и с экрана итогов
        public void ToMenu()
        {
            SetState(GameState.Menu);
        }

        // забег из меню: сначала выбор способности слота A. Дневной забег всегда идёт в первой эре.
        public void StartFromMenu(int era, bool dailyRun)
        {
            eraIndex = dailyRun ? 0 : era;
            daily = dailyRun;
            StartRun();
            SetState(GameState.AbilityPick);
            abilities.OfferAll();
            abilityPickView.Show();
        }

        // Retry: экран выбора пропускается; слот A, эра и режим остаются прежними
        public void StartRun()
        {
            Run.Reset(config, daily ? Today() : UnityEngine.Random.Range(0, int.MaxValue));
            abilities.ResetRun();
            RecomputeStats();
            Run.hp = Stats.starMaxHp;
            satellites.ResetRun();
            enemies.ResetRun();
            boss.ResetRun();
            crystals.ResetRun();
            director.ResetRun(Run.seed);
            cards.ResetRun(Run.seed + Rng.CardOffset);
            fx.ResetRun();
            accumulator = 0f;
            SetState(GameState.Playing);
        }

        public void SetState(GameState next)
        {
            if (State == GameState.Playing && next != GameState.Playing) input.Clear();
            State = next;
            hud.gameObject.SetActive(next != GameState.Menu);
            pauseView.gameObject.SetActive(next == GameState.Paused);
            menuView.gameObject.SetActive(next == GameState.Menu);
            resultView.gameObject.SetActive(next == GameState.Result);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void RecomputeStats()
        {
            Stats.Recompute(config, Era, Run.cardStacks, Save.Data.blueprints, abilities);
            armorRing.Refresh();
        }

        // Комета убита: короткое празднование, потом итоги
        public void Win()
        {
            victoryTimer = config.victoryTime;
            SetState(GameState.Victory);
            sfx.Play(SfxId.Win);
        }

        // предложение возрождения появится в M8: пока поражение сразу ведёт к итогам
        public void Lose()
        {
            SetState(GameState.Defeat);
            sfx.Play(SfxId.Lose);
            Finish(false);
        }

        void Update()
        {
            // поле радиуса H целиком видно и в альбомной, и в портретной ориентации
            cam.orthographicSize = config.H * Mathf.Max(1f, 1f / cam.aspect);

            if (pauseKey.WasPressedThisFrame()) TogglePause();
            if (State == GameState.Result && restartKey.WasPressedThisFrame()) StartRun();
            if (State == GameState.Victory)
            {
                // симуляция стоит, поэтому празднование идёт по настоящему времени
                victoryTimer -= Time.unscaledDeltaTime;
                if (victoryTimer <= 0f) Finish(true);
            }
            // hit-stop: симуляция стоит, время за него не копится
            if (State != GameState.Playing || fx.HitStopped) return;

            float step = 1f / config.simHz;
            accumulator += Mathf.Min(Time.deltaTime, config.maxFrameDelta);
            for (int i = 0; i < config.maxStepsPerFrame && accumulator >= step && State == GameState.Playing && !fx.HitStopped; i++)
            {
                accumulator -= step;
                Step(step);
            }
            // остаток сверх лимита шагов отбрасываем, иначе долг копится на медленных кадрах
            accumulator = Mathf.Min(accumulator, step);
        }

        void Step(float dt)
        {
            // пересчёт каждый шаг: правка конфига в инспекторе сразу попадает в Stats
            RecomputeStats();
            Run.time += dt;
            satellites.Tick(dt);
            abilities.Tick(dt);
            enemies.Tick(dt);
            boss.Tick(dt);
            enemies.Collide();
            boss.Collide();
            crystals.Tick(dt);
            director.Tick(dt);
            // победа важнее поражения, поражение важнее экрана карточек
            if (State == GameState.Playing) star.Tick();
            if (State == GameState.Playing) cards.Tick(dt);
            fx.Tick(dt);
        }

        // итоги забега: пыль, рекорды и новая эра уходят в сохранение, потом открывается экран Result
        void Finish(bool victory)
        {
            SaveData data = Save.Data;
            Run.victory = victory;

            // пыль: собранные кристаллы и премия за победу; первый за дату дневной забег её удваивает
            Run.earned = Run.dust + (victory ? config.victoryDust : 0);
            if (daily && data.dailyBonusDate != Run.seed)
            {
                Run.earned *= config.dailyDustMul;
                data.dailyBonusDate = Run.seed;
            }
            data.dust += Run.earned;

            Run.recordGap = data.bestTime - Run.time;
            data.bestTime = Mathf.Max(data.bestTime, Run.time);

            int score = Run.dust + Mathf.RoundToInt(config.scorePerSecond * Run.time) + (victory ? config.scoreVictory : 0);
            if (daily) data.bestDaily = Mathf.Max(data.bestDaily, score);

            // победа в последней открытой эре открывает следующую
            Run.unlockedEra = 0;
            if (victory && eraIndex == data.erasUnlocked - 1 && data.erasUnlocked < config.eras.Length)
            {
                Run.unlockedEra = data.erasUnlocked;
                data.erasUnlocked++;
            }

            data.runs++;
            if (!victory) data.defeats++;
            data.tutorialDone = true;
            Save.Save();
            SetState(GameState.Result);
        }

        static int Today()
        {
            return int.Parse(DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }
    }
}
