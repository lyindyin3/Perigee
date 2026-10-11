using System.Globalization;
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
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text header;
        [SerializeField] GameObject[] slots;
        [SerializeField] TMP_Text[] titles;
        [SerializeField] TMP_Text[] plusLines;
        [SerializeField] TMP_Text[] minusLines;

        float openTime;
        bool closing;

        public void Show()
        {
            header.text = Strings.CardPickTitle;
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < cards.OfferCount;
                slots[i].SetActive(used);
                if (!used) continue;

                CardDef card = cards.Offer(i);
                titles[i].text = Strings.CardTitle[(int)card.id];
                plusLines[i].text = Line(card.plus);
                minusLines[i].text = Line(card.minus);
            }

            openTime = Time.unscaledTime;
            closing = false;
            group.alpha = 1f;
            group.blocksRaycasts = false;
            gameObject.SetActive(true);
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
            closing = true;
            group.blocksRaycasts = false;
            group.DOFade(0f, config.pickFadeOut).SetLink(gameObject).OnComplete(Close);
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
