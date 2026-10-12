using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    public class CardPickView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] CardSystem cards;
        [SerializeField] AudioManager sfx;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text header;
        [SerializeField] GameObject[] slots;
        [SerializeField] Image[] icons;
        [SerializeField] TMP_Text[] titles;
        [SerializeField] TMP_Text[] plusLines;
        [SerializeField] TMP_Text[] minusLines;
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
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < cards.OfferCount;
                slots[i].SetActive(used);
                if (!used) continue;

                CardDef card = cards.Offer(i);
                icons[i].sprite = art.cardIcons[(int)card.id];
                titles[i].text = Strings.CardTitle[(int)card.id];
                plusLines[i].text = Line(card.plus);
                minusLines[i].text = Line(card.minus);
            }
            rerollButton.SetActive(!cards.Rerolled);
        }

        void Close()
        {
            gameObject.SetActive(false);
            loop.SetState(GameState.Playing);
        }

        string Line(StatMod mod)
        {
            // у МАЯКА в строке стоит период волны, а не число стаков
            float value = mod.stat == StatId.Beacon ? config.beaconInterval : mod.value;
            return string.Format(CultureInfo.InvariantCulture, Strings.StatFormat[(int)mod.stat], value);
        }
    }
}
