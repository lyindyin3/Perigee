using UnityEngine;
using UnityEngine.InputSystem;

namespace Perigee
{
    public enum GameState { Menu, AbilityPick, Playing, Paused, CardPick, Defeat, Victory, Result }

    public class GameLoop : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] Camera cam;
        [SerializeField] InputHold input;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] EnemySystem enemies;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] Director director;
        [SerializeField] StarController star;
        [SerializeField] CardSystem cards;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] ArmorRing armorRing;
        [SerializeField] PauseView pauseView;
        [SerializeField] AbilityPickView abilityPickView;

        public GameState State { get; private set; }
        public Stats Stats { get; } = new Stats();
        public RunState Run { get; } = new RunState();
        public EraConfig Era => config.eras[eraIndex];

        int eraIndex;
        float accumulator;
        InputAction restartKey;
        InputAction pauseKey;

        void Awake()
        {
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
            // меню появится в M6: до него игра открывается так, как будто в меню нажали «Играть»
            StartFromMenu();
        }

        // забег из меню: сначала выбор способности слота A
        public void StartFromMenu()
        {
            StartRun();
            SetState(GameState.AbilityPick);
            abilities.OfferAll();
            abilityPickView.Show();
        }

        // Retry: экран выбора пропускается, слот A остаётся прежним
        public void StartRun()
        {
            Run.Reset(config, Random.Range(0, int.MaxValue));
            abilities.ResetRun();
            RecomputeStats();
            satellites.ResetRun();
            enemies.ResetRun();
            crystals.ResetRun();
            director.ResetRun(Run.seed);
            cards.ResetRun(Run.seed + Rng.CardOffset);
            accumulator = 0f;
            SetState(GameState.Playing);
        }

        public void SetState(GameState next)
        {
            if (State == GameState.Playing && next != GameState.Playing) input.Clear();
            State = next;
            pauseView.gameObject.SetActive(next == GameState.Paused);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void RecomputeStats()
        {
            Stats.Recompute(config, Era, Run.cardStacks, abilities);
            armorRing.Refresh();
        }

        void Update()
        {
            // поле радиуса H целиком видно и в альбомной, и в портретной ориентации
            cam.orthographicSize = config.H * Mathf.Max(1f, 1f / cam.aspect);

            if (pauseKey.WasPressedThisFrame()) TogglePause();
            if (State == GameState.Defeat && restartKey.WasPressedThisFrame()) StartRun();
            if (State != GameState.Playing) return;

            float step = 1f / config.simHz;
            accumulator += Mathf.Min(Time.deltaTime, config.maxFrameDelta);
            for (int i = 0; i < config.maxStepsPerFrame && accumulator >= step && State == GameState.Playing; i++)
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
            enemies.Collide();
            crystals.Tick(dt);
            director.Tick(dt);
            star.Tick();
            // поражение важнее экрана карточек
            if (State == GameState.Playing) cards.Tick(dt);
        }
    }
}
