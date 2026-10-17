using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Perigee
{
    public enum GameState { Menu, AbilityPick, Playing, Paused, CardPick, Defeat, Victory, Result, Story, Deck }

    public class GameLoop : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] Camera cam;
        // большая тусклая картинка эры за кольцами фона
        [SerializeField] SpriteRenderer backdrop;
        [SerializeField] InputHold input;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] EnemySystem enemies;
        [SerializeField] BossSystem boss;
        [SerializeField] EraRuleSystem eraRule;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] Director director;
        [SerializeField] StarController star;
        [SerializeField] CardSystem cards;
        [SerializeField] ComboSystem combo;
        [SerializeField] RelicSystem relics;
        [SerializeField] RunEventSystem events;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] ArmorRing armorRing;
        [SerializeField] Fx fx;
        [SerializeField] ScreenFx screenFx;
        [SerializeField] BossIntro bossIntro;
        [SerializeField] AudioManager sfx;
        [SerializeField] Hud hud;
        [SerializeField] TutorialHints hints;
        [SerializeField] BannerView banner;
        [SerializeField] PauseView pauseView;
        // «Твои карточки»: пауза с карточками и реликвиями забега
        [SerializeField] DeckView deckView;
        [SerializeField] AbilityPickView abilityPickView;
        [SerializeField] MenuView menuView;
        [SerializeField] ReviveView reviveView;
        [SerializeField] ResultView resultView;
        [SerializeField] SettingsView settingsView;
        [SerializeField] AchievementToast toast;
        [SerializeField] StoryView storyView;

        public GameState State { get; private set; }
        public Stats Stats { get; } = new Stats();
        public RunState Run { get; } = new RunState();
        public ISaveService Save { get; private set; }
        public Blueprints Blueprints { get; private set; }
        public Shop Shop { get; private set; }
        public Progress Progress { get; private set; }
        public Achievements Achievements { get; private set; }
        public EraDef Era => config.eras[eraIndex];
        // звезда: класс забега, её радиус сейчас, фаза дыхания
        public StarController Star => star;
        // номер эры с единицы: по нему отбираются карточки эр
        public int EraNumber => eraIndex + 1;
        // СТАЗИС: враги, их таймеры и боссы живут в замедленном времени
        // ПЕСОЧНЫЕ ЧАСЫ замедляют так же
        public float EnemyTimeScale => abilities.Active(AbilityId.Stasis) || relics.Stasis ? config.stasisSlow : 1f;
        // секунда забега, на которой приходит босс; в «Марафоне» позже
        public float BossTime => Run.Has(RunModifier.Marathon) ? config.marathonBossTime : config.bossTime;
        // музыка забега: при живом боссе его тема, иначе музыка эры
        // замедление от эффектов (приход и гибель босса): множитель шага симуляции
        public float FxTimeScale => fx.TimeScale;
        public MusicId RunMusic => boss.Alive ? (EraNumber == config.finalBossEra ? MusicId.FinalBoss : MusicId.Boss) : Era.music;

        IAdService ads;
        ILeaderboardService leaderboard;
        IPlatformService platform;
        int eraIndex;
        RunMode mode;
        // номер испытания в config.challenges, пока режим Challenge
        int challenge;
        float accumulator;
        float victoryTimer;
        // забег сброшен, но ещё не шёл: первый вход в Playing играет разгон
        bool starting;
        // вкладка свёрнута или окно без фокуса
        bool away;
        // настоящее время последних показов рекламы
        float lastInterstitial = float.NegativeInfinity;
        float lastRewarded = float.NegativeInfinity;
        Action<bool> rewardedDone;
        Action<bool> onRewardedClosed;
        Action<bool> onRevive;
        Action onStoryDone;
        Action showResult;
        // сюжет сейчас: финал (иначе вступление)
        bool storyEnding;
        InputAction restartKey;
        InputAction pauseKey;
        InputAction deckKey;

        void Awake()
        {
            Save = new SaveService(config);
            Blueprints = new Blueprints(config, Save);
            Shop = new Shop(config, Save);
            Progress = new Progress(config, Save, Shop);
            Achievements = new Achievements(config, art, Save, Blueprints, toast, sfx);
            ads = new StubAdService();
            leaderboard = new StubLeaderboardService();
            platform = new StubPlatformService();
            // язык: выбранный в настройках, а до выбора язык браузера
            int language = Save.Data.settings.language;
            Loc.Set(language >= 0 ? (Loc.Lang)language : Loc.FromCode(platform.Language()));
            onRewardedClosed = RewardedClosed;
            onRevive = Revived;
            showResult = ShowResult;

            restartKey = new InputAction(binding: "<Keyboard>/r");
            restartKey.Enable();

            pauseKey = new InputAction(type: InputActionType.Button);
            pauseKey.AddBinding("<Keyboard>/escape");
            pauseKey.AddBinding("<Keyboard>/p");
            pauseKey.Enable();

            deckKey = new InputAction(binding: "<Keyboard>/tab");
            deckKey.Enable();
        }

        void OnDestroy()
        {
            restartKey.Dispose();
            pauseKey.Dispose();
            deckKey.Dispose();
        }

        void Start()
        {
            // звезда и кольцо брони видны за меню и читают Run и Stats ещё до первого забега
            Run.Reset(config, 0);
            RecomputeStats();
            Run.hp = Stats.starMaxHp;
            settingsView.Apply();
            ToMenu();
            platform.GameReady();
        }

        // из паузы и с экрана итогов
        public void ToMenu()
        {
            if (State == GameState.Result) Interstitial();
            SetState(GameState.Menu);
        }

        // забег из меню: сначала выбор способности слота A. День и обучение всегда идут в первой эре, испытание в своей.
        // challengeIndex = номер в config.challenges, нужен только режиму Challenge
        public void StartFromMenu(int era, RunMode runMode, int challengeIndex)
        {
            mode = runMode;
            challenge = challengeIndex;
            if (runMode == RunMode.Daily || !Save.Data.tutorialDone) eraIndex = 0;
            else if (runMode == RunMode.Challenge) eraIndex = config.challenges[challenge].era - 1;
            else eraIndex = era;
            ResetRun();
            // обучение: слот A уже занят; «Без способностей»: слотов нет. Экран выбора не нужен
            if (Run.tutorial || Run.Has(RunModifier.NoAbilities))
            {
                SetState(GameState.Playing);
                return;
            }

            SetState(GameState.AbilityPick);
            abilities.OfferAll();
            abilityPickView.Show();
        }

        // Retry и «Заново» из паузы: экран выбора пропускается; слот A, эра, режим и модификаторы остаются прежними
        public void StartRun()
        {
            if (State == GameState.Result) Interstitial();
            ResetRun();
            SetState(GameState.Playing);
        }

        void ResetRun()
        {
            // зерно дневного забега = дата UTC
            Run.Reset(config, mode == RunMode.Daily ? Progress.Today() : UnityEngine.Random.Range(0, int.MaxValue));
            Run.mode = mode;
            Run.challenge = challenge;
            // обучение = любой забег, пока ни один не доигран до итогов
            Run.tutorial = !Save.Data.tutorialDone;
            Run.mods = Modifiers();
            // класс звезды: выбранный в меню, в обучении всегда Жёлтый
            Run.starClass = Run.tutorial ? StarClassId.Yellow : Shop.StarClass;
            relics.ResetRun(Run.seed + Rng.RelicOffset);
            abilities.ResetRun();
            if (Run.Has(RunModifier.NoAbilities)) abilities.Clear();
            eraRule.ResetRun();
            events.ResetRun();
            if (Run.tutorial) abilities.Assign(AbilitySystem.SlotA, AbilityId.Pulse);
            // звезда первой: от её класса зависят радиус, HP и rMin, которые читают остальные
            star.ResetRun();
            RecomputeStats();
            Run.hp = Stats.starMaxHp;
            satellites.ResetRun();
            enemies.ResetRun();
            boss.ResetRun();
            crystals.ResetRun();
            combo.ResetRun();
            director.ResetRun(Run.seed);
            cards.ResetRun(Run.seed + Rng.CardOffset);
            fx.ResetRun();
            screenFx.ResetRun();
            bossIntro.ResetRun();
            hints.ResetRun();
            banner.ResetRun();
            // фон в цвете эры; картинка эры за кольцами, если она задана
            cam.backgroundColor = Era.background;
            backdrop.sprite = art.eraBackdrops[eraIndex].sprite;
            backdrop.enabled = backdrop.sprite != null;
            accumulator = 0f;
            starting = true;
        }

        // модификаторы забега: у Дня один на дату, у испытания свои; в обучении их нет
        RunModifier Modifiers()
        {
            if (Run.tutorial) return RunModifier.None;
            if (mode == RunMode.Daily) return Progress.DailyMod;
            return mode == RunMode.Challenge ? config.challenges[challenge].mods : RunModifier.None;
        }

        public void SetState(GameState next)
        {
            if (State == GameState.Playing && next != GameState.Playing)
            {
                input.Clear();
                platform.GameplayStop();
            }
            if (State != GameState.Playing && next == GameState.Playing) platform.GameplayStart();
            if (next == GameState.Playing && starting)
            {
                starting = false;
                screenFx.Warp();
                star.Punch(config.startStarPunch, config.startStarPunchTime);
            }

            State = next;
            // настройки открываются поверх меню и паузы и уходят вместе с ними
            settingsView.Close();
            hud.gameObject.SetActive(next != GameState.Menu && next != GameState.Story);
            pauseView.gameObject.SetActive(next == GameState.Paused);
            deckView.gameObject.SetActive(next == GameState.Deck);
            menuView.gameObject.SetActive(next == GameState.Menu);
            reviveView.gameObject.SetActive(next == GameState.Defeat);
            resultView.gameObject.SetActive(next == GameState.Result);

            if (next == GameState.Menu || next == GameState.Result) sfx.PlayMusic(MusicId.Menu);
            else if (next == GameState.Playing) sfx.PlayMusic(RunMusic);
            else if (next == GameState.Story) sfx.PlayMusic(MusicId.Story);
        }

        // вступление или финал; после последнего слайда или «Пропустить» он отмечается просмотренным и зовётся done
        public void ShowStory(bool ending, Action done)
        {
            storyEnding = ending;
            onStoryDone = done;
            SetState(GameState.Story);
            storyView.Show(ending, StoryDone);
        }

        void StoryDone()
        {
            if (storyEnding) Save.Data.endingSeen = true;
            else Save.Data.introSeen = true;
            Save.Save();
            onStoryDone();
        }

        // вибрация телефона, если она не выключена в настройках
        public void Vibrate(int ms)
        {
            if (Save.Data.settings.vibration) platform.Vibrate(ms);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing || State == GameState.Paused || State == GameState.Deck) sfx.Play(SfxId.UiClick);
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused || State == GameState.Deck) SetState(GameState.Playing);
        }

        // Tab или иконка карточки в HUD: пауза с карточками забега; повторный Tab продолжает забег
        public void ToggleDeck()
        {
            if (State == GameState.Playing || State == GameState.Deck) sfx.Play(SfxId.UiClick);
            if (State == GameState.Playing) SetState(GameState.Deck);
            else if (State == GameState.Deck) SetState(GameState.Playing);
        }

        public void RecomputeStats()
        {
            Stats.Recompute(config, Era, star.Class, star.Bright, Run.mods, Run.cardStacks, Run.relicStacks, Save.Data.blueprints, abilities, satellites.Slinging, eraRule.Sailing);
            // Солнечная буря: удар и нагрев сильнее, пока она идёт
            if (events.Storm)
            {
                Stats.powerMul *= config.stormPowerMul;
                Stats.heatMul *= config.stormHeatMul;
            }
            armorRing.Refresh();
        }

        // босс эры убит: короткое празднование, потом итоги
        public void Win()
        {
            victoryTimer = config.victoryTime;
            SetState(GameState.Victory);
            sfx.Play(SfxId.Win);
        }

        // звезда погасла: возрождение предлагается один раз за забег, во второй раз сразу итоги
        public void Lose()
        {
            sfx.Play(SfxId.Lose);
            if (Run.revived) Finish(false);
            else SetState(GameState.Defeat);
        }

        // из ReviveView: реклама за возрождение
        public void Revive()
        {
            Rewarded(onRevive);
        }

        // из ReviveView: отказ от возрождения
        public void GiveUp()
        {
            Finish(false);
        }

        // реклама за награду: пока она открыта, звук молчит; done(true) = досмотрена
        public void Rewarded(Action<bool> done)
        {
            rewardedDone = done;
            AudioListener.pause = true;
            ads.ShowRewarded(onRewardedClosed);
        }

        void RewardedClosed(bool rewarded)
        {
            AudioListener.pause = away;
            lastRewarded = Time.realtimeSinceStartup;
            rewardedDone(rewarded);
        }

        void Revived(bool rewarded)
        {
            if (!rewarded)
            {
                Finish(false);
                return;
            }

            Run.revived = true;
            Run.hp = Stats.starMaxHp * config.reviveHpFraction;
            Run.immuneUntil = Run.time + config.reviveImmuneTime;
            enemies.Wipe(config.reviveKillRadius);
            satellites.Cool();
            sfx.Play(SfxId.Revive);
            SetState(GameState.Playing);
        }

        // реклама между забегами: при уходе с экрана итогов, не в первых забегах и не слишком часто
        void Interstitial()
        {
            float now = Time.realtimeSinceStartup;
            if (Save.Data.runs < config.interstitialMinRuns || Run.time < config.interstitialMinRunTime) return;
            if (now - lastInterstitial < config.interstitialMinInterval || now - lastRewarded < config.interstitialAfterRewarded) return;

            lastInterstitial = now;
            // у ShowInterstitial нет ответа: реклама считается закрытой, когда вызов вернулся
            AudioListener.pause = true;
            ads.ShowInterstitial();
            AudioListener.pause = away;
        }

        // окно потеряло фокус или вкладка свёрнута: пауза и тишина; после возврата звук идёт снова, пауза остаётся
        void OnApplicationFocus(bool focused)
        {
            SetAway(!focused);
        }

        void OnApplicationPause(bool paused)
        {
            SetAway(paused);
        }

        void SetAway(bool value)
        {
            away = value;
            if (away && State == GameState.Playing) SetState(GameState.Paused);
            AudioListener.pause = away;
        }

        void Update()
        {
            // поле радиуса H целиком видно и в альбомной, и в портретной ориентации
            cam.orthographicSize = config.H * Mathf.Max(1f, 1f / cam.aspect) * fx.Zoom;

            if (pauseKey.WasPressedThisFrame()) TogglePause();
            if (deckKey.WasPressedThisFrame()) ToggleDeck();
            if (State == GameState.Result && restartKey.WasPressedThisFrame()) StartRun();
            if (State == GameState.Victory)
            {
                // симуляция стоит, поэтому празднование идёт по настоящему времени
                victoryTimer -= Time.unscaledDeltaTime;
                // осколки гибели босса доигрывают по настоящему времени в замедлении
                fx.Tick(Time.unscaledDeltaTime * fx.TimeScale);
                if (victoryTimer <= 0f) Finish(true);
            }
            // hit-stop: симуляция стоит, время за него не копится
            if (State != GameState.Playing || fx.HitStopped) return;

            float step = 1f / config.simHz;
            accumulator += Mathf.Min(Time.deltaTime, config.maxFrameDelta);
            for (int i = 0; i < config.maxStepsPerFrame && accumulator >= step && State == GameState.Playing && !fx.HitStopped; i++)
            {
                accumulator -= step;
                // замедление укорачивает шаг: кадров столько же, время забега идёт медленнее
                Step(step * fx.TimeScale);
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
            Run.maxSatellites = Mathf.Max(Run.maxSatellites, satellites.Count);
            abilities.Tick(dt);
            eraRule.Tick(dt);
            events.Tick(dt);
            enemies.Tick(dt);
            boss.Tick(dt);
            enemies.Collide();
            boss.Collide();
            combo.Tick(dt);
            crystals.Tick(dt);
            director.Tick(dt);
            // победа важнее поражения, поражение важнее экрана карточек
            if (State == GameState.Playing) star.Tick(dt);
            if (State == GameState.Playing) cards.Tick(dt);
            // экран карточек важнее экрана реликвий: выбор реликвии ждёт шага, в котором игра идёт
            relics.Tick(dt);
            fx.Tick(dt);
            if (Run.tutorial) hints.Tick(dt);
        }

        // итоги забега: пыль, рекорды, новая эра и награды уходят в сохранение, потом открывается экран Result
        void Finish(bool victory)
        {
            Run.victory = victory;
            // незаконченная серия тоже идёт в лучшую серию сохранения
            combo.Break();

            Progress.Finish(Run, eraIndex, Stats);
            Achievements.Check(Run, eraIndex);
            leaderboard.Submit(Run.score);
            // первая победа над Тьмой: финал сюжета перед итогами
            if (victory && EraNumber == config.finalBossEra && !Save.Data.endingSeen) ShowStory(true, showResult);
            else ShowResult();
        }

        void ShowResult()
        {
            SetState(GameState.Result);
        }
    }
}
