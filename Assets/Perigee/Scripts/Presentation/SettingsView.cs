using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Настройки: открываются из главного меню и из паузы. Каждая правка сразу уходит в сохранение.
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] GameLoop loop;
        [SerializeField] MenuView menu;
        [SerializeField] ConfirmDialog confirm;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text sfxLabel;
        [SerializeField] Slider sfxSlider;
        [SerializeField] TMP_Text droneLabel;
        [SerializeField] Slider droneSlider;
        [SerializeField] TMP_Text shakeLabel;
        [SerializeField] Toggle shakeToggle;
        [SerializeField] TMP_Text numbersLabel;
        [SerializeField] Toggle numbersToggle;
        // «Полоски HP врагов» и «Фон»: кнопка листает варианты по кругу, на ней написан выбранный
        [SerializeField] TMP_Text ringsLabel;
        [SerializeField] TMP_Text ringsValue;
        [SerializeField] TMP_Text backgroundLabel;
        [SerializeField] TMP_Text backgroundValue;
        // «Пройти обучение снова» и «Сбросить прогресс»: только из главного меню
        [SerializeField] GameObject progressButtons;
        [SerializeField] TMP_Text replayLabel;
        [SerializeField] TMP_Text resetLabel;
        [SerializeField] TMP_Text versionLabel;
        [SerializeField] TMP_Text closeLabel;
        // простой фон прячет полосы фона и лучи звезды и останавливает вращение колец
        [SerializeField] GameObject backgroundBars;
        [SerializeField] GameObject starRays;
        [SerializeField] Transform rings;

        Action replayTutorial;
        Action resetProgress;

        void Awake()
        {
            replayTutorial = DoReplayTutorial;
            resetProgress = DoResetProgress;

            title.text = Strings.Settings;
            sfxLabel.text = Strings.SfxVolume;
            droneLabel.text = Strings.DroneVolume;
            shakeLabel.text = Strings.Shake;
            numbersLabel.text = Strings.DamageNumbers;
            ringsLabel.text = Strings.HpRings;
            backgroundLabel.text = Strings.Background;
            replayLabel.text = Strings.ReplayTutorial;
            resetLabel.text = Strings.ResetProgress;
            versionLabel.text = string.Format(Strings.VersionFormat, Application.version);
            closeLabel.text = Strings.Close;
        }

        public void Open()
        {
            SettingsData settings = Data;
            // значения встают без вызова обработчиков: это не правка игрока
            sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
            droneSlider.SetValueWithoutNotify(settings.droneVolume);
            shakeToggle.SetIsOnWithoutNotify(settings.shake);
            numbersToggle.SetIsOnWithoutNotify(settings.damageNumbers);
            // посреди забега прогресс не сбрасывают и обучение не начинают
            progressButtons.SetActive(loop.State == GameState.Menu);
            gameObject.SetActive(true);
            ShowChoices();
        }

        public void Close()
        {
            confirm.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        // простой фон действует сразу и с первого кадра игры: GameLoop зовёт это на старте
        public void Apply()
        {
            bool full = !Data.simpleBackground;
            backgroundBars.SetActive(full);
            starRays.SetActive(full);
            // вращение колец фона = декор Spin на них: сколько бы его ни было
            Spin[] spins = rings.GetComponentsInChildren<Spin>(true);
            for (int i = 0; i < spins.Length; i++) spins[i].enabled = full;
        }

        // ползунки и галочки: значение приходит из сцены (dynamic)
        public void SetSfx(float value)
        {
            Data.sfxVolume = value;
            loop.Save.Save();
        }

        public void SetDrone(float value)
        {
            Data.droneVolume = value;
            loop.Save.Save();
        }

        public void SetShake(bool value)
        {
            Data.shake = value;
            loop.Save.Save();
        }

        public void SetNumbers(bool value)
        {
            Data.damageNumbers = value;
            loop.Save.Save();
        }

        public void NextRings()
        {
            Data.hpRings = (Data.hpRings + 1) % Strings.HpRingModes.Length;
            loop.Save.Save();
            ShowChoices();
        }

        public void NextBackground()
        {
            Data.simpleBackground = !Data.simpleBackground;
            loop.Save.Save();
            Apply();
            ShowChoices();
        }

        public void ReplayTutorial()
        {
            confirm.Show(Strings.ConfirmTutorial, replayTutorial);
        }

        public void ResetProgress()
        {
            confirm.Show(Strings.ConfirmReset, resetProgress);
        }

        SettingsData Data => loop.Save.Data.settings;

        void ShowChoices()
        {
            ringsValue.text = Strings.HpRingModes[Data.hpRings];
            backgroundValue.text = Data.simpleBackground ? Strings.BackgroundSimple : Strings.BackgroundFull;
        }

        // следующий забег из меню снова будет обучением
        void DoReplayTutorial()
        {
            loop.Save.Data.tutorialDone = false;
            loop.Save.Save();
            Close();
            menu.ShowMain();
        }

        void DoResetProgress()
        {
            loop.Save.Reset();
            Close();
            menu.ShowMain();
        }
    }
}
