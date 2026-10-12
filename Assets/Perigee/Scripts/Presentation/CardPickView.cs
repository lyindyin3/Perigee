using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Perigee
{
    public class CardPickView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] CardSystem cards;
        [SerializeField] AudioManager sfx;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text header;
        // карточки в ряду: экземпляры префаба CardView
        [SerializeField] CardView[] views;
        // «Новые карточки (реклама)»: один раз за забег
        [SerializeField] GameObject rerollButton;
        [SerializeField] TMP_Text rerollLabel;

        float openTime;
        bool closing;

        public void Show()
        {
            header.text = Strings.CardPickTitle;
            rerollLabel.text = Strings.NewCardsAd;
            Fill();

            openTime = Time.unscaledTime;
            closing = false;
            group.alpha = 1f;
            group.blocksRaycasts = false;
            gameObject.SetActive(true);
            sfx.Play(SfxId.CardOpen);
        }

        void Update()
        {
            // до конца задержки нажатия проходят сквозь экран, поэтому палец, зажатый до открытия, карточку не выберет
            group.blocksRaycasts = !closing && Time.unscaledTime - openTime >= config.pickInputDelay;
        }

        // вызывают кнопки карточек, номер слота задан в сцене
        public void Pick(int slot)
        {
            if (closing) return;

            cards.Pick(slot);
            sfx.Play(SfxId.CardPick);
            closing = true;
            group.blocksRaycasts = false;
            group.DOFade(0f, config.pickFadeOut).SetLink(gameObject).OnComplete(Close);
        }

        // кнопка под карточками: реклама, потом другая тройка
        public void Reroll()
        {
            if (closing) return;

            loop.Rewarded(Rerolled);
        }

        void Rerolled(bool rewarded)
        {
            if (!rewarded) return;

            cards.Reroll();
            Fill();
        }

        void Fill()
        {
            for (int i = 0; i < views.Length; i++)
            {
                bool used = i < cards.OfferCount;
                views[i].gameObject.SetActive(used);
                // точки показывают стаки после выбора
                if (used) views[i].Show(cards.Offer(i), cards.Stacks(i) + 1);
            }
            rerollButton.SetActive(!cards.Rerolled);
        }

        void Close()
        {
            gameObject.SetActive(false);
            loop.SetState(GameState.Playing);
        }
    }
}
