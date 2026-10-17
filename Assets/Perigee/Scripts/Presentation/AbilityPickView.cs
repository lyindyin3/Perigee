using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    public class AbilityPickView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] AudioManager sfx;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text header;
        [SerializeField] GameObject[] slots;
        [SerializeField] Button[] buttons;
        [SerializeField] Image[] icons;
        [SerializeField] TMP_Text[] titles;
        [SerializeField] TMP_Text[] effectLines;
        [SerializeField] TMP_Text[] cooldownLines;
        // затемнение с замком и ценой поверх не купленной способности
        [SerializeField] GameObject[] locks;
        [SerializeField] TMP_Text[] prices;

        // картинки берутся из сцены: Ability<номер> показывает иконку способности с этим номером
        Sprite[] sceneIcons;
        float openTime;
        bool closing;

        // показывает предложение, которое перед этим собрал AbilitySystem (OfferAll или OfferRandom)
        public void Show()
        {
            if (sceneIcons == null)
            {
                sceneIcons = new Sprite[icons.Length];
                for (int i = 0; i < icons.Length; i++) sceneIcons[i] = icons[i].sprite;
            }

            Typing.Show(header, Strings.AbilityPickTitle);
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < abilities.OfferCount;
                slots[i].SetActive(used);
                if (!used) continue;

                AbilityDef def = abilities.Offer(i);
                icons[i].sprite = sceneIcons[(int)def.id];
                Typing.Show(titles[i], Strings.AbilityTitle[(int)def.id]);
                effectLines[i].text = Strings.AbilityText[(int)def.id];
                cooldownLines[i].text = string.Format(CultureInfo.InvariantCulture, Strings.CooldownFormat, def.cooldown * loop.Stats.abilityCooldownMul);

                // не купленную способность выбрать нельзя
                bool owned = abilities.Owned(def.id);
                buttons[i].interactable = owned;
                locks[i].SetActive(!owned);
                prices[i].text = string.Format(CultureInfo.InvariantCulture, Strings.PriceFormat, def.price);
            }

            openTime = Time.unscaledTime;
            closing = false;
            group.alpha = 1f;
            group.blocksRaycasts = false;
            gameObject.SetActive(true);
            sfx.Play(SfxId.CardOpen);
        }

        void Update()
        {
            // до конца задержки нажатия проходят сквозь экран, поэтому палец, зажатый до открытия, способность не выберет
            group.blocksRaycasts = !closing && Time.unscaledTime - openTime >= config.pickInputDelay;
        }

        // вызывают кнопки карточек, номер слота задан в сцене
        public void Pick(int slot)
        {
            if (closing) return;

            abilities.Pick(slot);
            sfx.Play(SfxId.CardPick);
            closing = true;
            group.blocksRaycasts = false;
            group.DOFade(0f, config.pickFadeOut).SetLink(gameObject).OnComplete(Close);
        }

        void Close()
        {
            gameObject.SetActive(false);
            loop.SetState(GameState.Playing);
        }
    }
}
