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
        [SerializeField] Director director;
        [SerializeField] StarController star;

        public GameState State { get; private set; }
        public Stats Stats { get; } = new Stats();
        public RunState Run { get; } = new RunState();
        public EraConfig Era => config.eras[eraIndex];

        int eraIndex;
        float accumulator;
        InputAction restartKey;

        void Awake()
        {
            restartKey = new InputAction(binding: "<Keyboard>/r");
            restartKey.Enable();
        }

        void OnDestroy()
        {
            restartKey.Dispose();
        }

        void Start()
        {
            StartRun();
        }

        public void StartRun()
        {
            Stats.Reset(Era);
            Run.Reset(config, Random.Range(0, int.MaxValue));
            satellites.ResetRun();
            enemies.ResetRun();
            director.ResetRun(Run.seed);
            accumulator = 0f;
            SetState(GameState.Playing);
        }

        public void SetState(GameState next)
        {
            if (State == GameState.Playing && next != GameState.Playing) input.Clear();
            State = next;
        }

        void Update()
        {
            // поле радиуса H целиком видно и в альбомной, и в портретной ориентации
            cam.orthographicSize = config.H * Mathf.Max(1f, 1f / cam.aspect);
            cam.backgroundColor = config.background;

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
            Run.time += dt;
            satellites.Tick(dt);
            enemies.Tick(dt);
            enemies.Collide();
            director.Tick(dt);
            star.Tick();
        }
    }
}
