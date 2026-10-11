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

        void Awake()
        {
            title.text = Strings.Pause;
            resumeLabel.text = Strings.Resume;
            restartLabel.text = Strings.Restart;
        }

        public void Resume()
        {
            loop.SetState(GameState.Playing);
        }

        public void Restart()
        {
            loop.StartRun();
        }
    }
}
