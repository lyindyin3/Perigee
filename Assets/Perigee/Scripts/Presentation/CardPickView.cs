using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    public class CardPickView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] CardSystem cards;
        [SerializeField] AudioManager sfx;
        [SerializeField] ScreenFx screenFx;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text header;
        // ряд карточек (GridLayoutGroup) и карточки в нём: экземпляры префаба CardView
        [SerializeField] RectTransform row;
        [SerializeField] CardView[] views;
        // иконки карточек в HUD: туда улетает выбранная
        [SerializeField] RectTransform hudCards;
        // «Заменить (N)»: бесплатная замена чертежа ВЫБОР, видна, пока замены остались
        [SerializeField] GameObject freeRerollButton;
        [SerializeField] TMP_Text freeRerollLabel;
        // «Новые карточки (реклама)»: один раз за забег
        [SerializeField] GameObject rerollButton;
        [SerializeField] TMP_Text rerollLabel;

        float openTime;
        bool closing;

        public void Show()
        {
            header.text = Strings.CardPickTitle;
            rerollLabel.text = Strings.NewCardsAd;
            gameObject.SetActive(true);
            // карточки вылетают, когда кольцо нового уровня дошло до края поля
            Fill(config.levelUpRingTime);

            openTime = Time.unscaledTime;
            closing = false;
            group.alpha = 1f;
            group.blocksRaycasts = false;
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

            // эволюция: сначала слияние значков на карточке, потом карточка улетает
            bool evolution = cards.Offer(slot).color == CardColor.Evolution;
            cards.Pick(slot);
            if (evolution)
            {
                views[slot].Merge();
                screenFx.Flash(config.eliteGold, config.evoScreenFlash, config.evoScreenFlashTime);
                sfx.Play(SfxId.Evolution);
            }
            else sfx.Play(SfxId.CardPick);
            closing = true;
            group.blocksRaycasts = false;

            // выбранная подпрыгивает и улетает к иконкам HUD, уменьшаясь; остальные падают и гаснут
            float delay = evolution ? config.evoMergeTime + config.evoFlashTime : 0f;
            for (int i = 0; i < cards.OfferCount; i++)
            {
                Transform card = views[i].transform;
                card.DOKill(true);
                if (i != slot)
                {
                    views[i].Drop();
                    continue;
                }
                Sequence fly = DOTween.Sequence().SetTarget(card).SetLink(gameObject);
                fly.AppendInterval(delay);
                fly.Append(card.DOPunchScale(Vector3.one * config.cardPickPunch, config.cardPickPunchTime));
                fly.Append(card.DOMove(hudCards.position, config.cardFlyTime));
                fly.Join(card.DOScale(config.cardFlyScale, config.cardFlyTime));
            }
            // экран гаснет вместе с полётом
            group.DOFade(0f, config.pickFadeOut).SetDelay(delay + config.cardPickPunchTime).SetLink(gameObject).OnComplete(Close);
        }

        // кнопка под карточками: другие карточки бесплатно
        public void FreeReroll()
        {
            if (closing) return;

            sfx.Play(SfxId.UiClick);
            cards.FreeReroll();
            Fill(0f);
        }

        // кнопка под карточками: реклама, потом другие карточки
        public void Reroll()
        {
            if (closing) return;

            sfx.Play(SfxId.UiClick);
            loop.Rewarded(Rerolled);
        }

        void Rerolled(bool rewarded)
        {
            if (!rewarded) return;

            cards.Reroll();
            Fill(0f);
        }

        // карточки встают в ряд и через delay вылетают снизу по очереди, переворачиваясь
        void Fill(float delay)
        {
            for (int i = 0; i < views.Length; i++)
            {
                bool used = i < cards.OfferCount;
                Transform card = views[i].transform;
                card.DOKill();
                card.localScale = Vector3.one;
                views[i].gameObject.SetActive(used);
                // точки показывают стаки после выбора
                if (used) views[i].Show(cards.Offer(i), cards.Stacks(i) + 1);
            }
            freeRerollButton.SetActive(cards.FreeRerolls > 0);
            freeRerollLabel.text = string.Format(Strings.FreeRerollFormat, cards.FreeRerolls);
            rerollButton.SetActive(!cards.Rerolled);

            // места карточек в ряду считает раскладка; с них карточки и стартуют вниз
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            for (int i = 0; i < cards.OfferCount; i++)
            {
                Transform card = views[i].transform;
                Vector3 home = card.localPosition;
                card.localPosition = home + Vector3.down * config.cardInOffset;
                card.localScale = new Vector3(0f, 1f, 1f);
                float start = delay + i * config.cardInStagger;
                card.DOLocalMove(home, config.cardInTime).SetDelay(start).SetEase(Ease.OutBack).SetLink(gameObject);
                card.DOScaleX(1f, config.cardInTime).SetDelay(start).SetLink(gameObject);
            }
        }

        void Close()
        {
            gameObject.SetActive(false);
            loop.SetState(GameState.Playing);
        }
    }
}
