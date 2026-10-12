using TMPro;
using UnityEngine;

namespace Perigee
{
    public class PauseView : MonoBehaviour
    {
        [SerializeField] GameLoop loop;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text resumeLabel;
        [SerializeField] TMP_Text restartLabel;
        [SerializeField] TMP_Text menuLabel;

        void Awake()
        {
            title.text = Strings.Pause;
            resumeLabel.text = Strings.Resume;
            restartLabel.text = Strings.Restart;
            menuLabel.text = Strings.Menu;
        }

        public void Resume()
        {
            loop.SetState(GameState.Playing);
        }

        public void Restart()
        {
            loop.StartRun();
        }

        public void Menu()
        {
            loop.ToMenu();
        }
    }
}
