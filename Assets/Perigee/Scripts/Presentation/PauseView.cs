using TMPro;
using UnityEngine;

namespace Perigee
{
    public class PauseView : MonoBehaviour
    {
        [SerializeField] GameLoop loop;
        [SerializeField] SettingsView settings;
        [SerializeField] AudioManager sfx;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text resumeLabel;
        [SerializeField] TMP_Text restartLabel;
        [SerializeField] TMP_Text settingsLabel;
        [SerializeField] TMP_Text menuLabel;

        void Awake()
        {
            Loc.Changed += Localize;
            Localize();
        }

        void OnDestroy()
        {
            Loc.Changed -= Localize;
        }

        void Localize()
        {
            title.text = Strings.Pause;
            resumeLabel.text = Strings.Resume;
            restartLabel.text = Strings.Restart;
            settingsLabel.text = Strings.Settings;
            menuLabel.text = Strings.Menu;
        }

        public void Resume()
        {
            sfx.Play(SfxId.UiClick);
            loop.SetState(GameState.Playing);
        }

        public void Restart()
        {
            sfx.Play(SfxId.UiClick);
            loop.StartRun();
        }

        public void Settings()
        {
            sfx.Play(SfxId.UiClick);
            settings.Open();
        }

        public void Menu()
        {
            sfx.Play(SfxId.UiClick);
            loop.ToMenu();
        }
    }
}
