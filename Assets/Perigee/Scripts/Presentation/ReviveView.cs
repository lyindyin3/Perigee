using TMPro;
using UnityEngine;

namespace Perigee
{
    // Предложение возрождения: виден в состоянии Defeat.
    public class ReviveView : MonoBehaviour
    {
        [SerializeField] GameLoop loop;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text reviveLabel;
        [SerializeField] TMP_Text declineLabel;

        void Awake()
        {
            title.text = Strings.ReviveTitle;
            reviveLabel.text = Strings.ReviveAd;
            declineLabel.text = Strings.GiveUp;
        }

        public void Revive()
        {
            loop.Revive();
        }

        public void Decline()
        {
            loop.GiveUp();
        }
    }
}
