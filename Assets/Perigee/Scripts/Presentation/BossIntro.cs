using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Perigee
{
    // Приход босса: камера наезжает на него, время замедляется, крупное имя с подписью держится и гаснет, камера возвращается.
    public class BossIntro : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] Fx fx;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text tip;

        void Awake()
        {
            group.alpha = 0f;
        }

        public void ResetRun()
        {
            DOTween.Kill(this);
            group.alpha = 0f;
        }

        // position = где босс появился
        public void Show(EnemyType boss, Vector2 position)
        {
            title.text = Strings.BossTitle(boss);
            tip.text = Strings.BossTip(boss);
            fx.Focus(position * config.bossCamShift, config.bossZoom, config.bossCamTime);
            fx.Slow(config.bossIntroSlow, config.bossIntroSlowTime);

            DOTween.Kill(this);
            group.alpha = 0f;
            Sequence intro = DOTween.Sequence().SetTarget(this).SetLink(gameObject);
            intro.Append(group.DOFade(1f, config.bossIntroFade));
            intro.AppendInterval(config.bossIntroHold - 2f * config.bossIntroFade);
            intro.Append(group.DOFade(0f, config.bossIntroFade));
            intro.InsertCallback(config.bossIntroHold, Return);
        }

        void Return()
        {
            fx.Focus(Vector2.zero, 1f, config.bossCamTime);
        }
    }
}
