using TMPro;
using UnityEngine;

namespace Perigee
{
    // Предложение возрождения: виден в состоянии Defeat.
    public class ReviveView : MonoBehaviour
    {
        [SerializeField] GameLoop loop;
        [SerializeField] AudioManager sfx;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text reviveLabel;
        [SerializeField] TMP_Text declineLabel;

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
            title.text = Strings.ReviveTitle;
            reviveLabel.text = Strings.ReviveAd;
            declineLabel.text = Strings.GiveUp;
        }

        public void Revive()
        {
            sfx.Play(SfxId.UiClick);
            loop.Revive();
        }

        public void Decline()
        {
            sfx.Play(SfxId.UiClick);
            loop.GiveUp();
        }
    }
}
