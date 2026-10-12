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
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text header;
        [SerializeField] GameObject[] slots;
        [SerializeField] Image[] icons;
        [SerializeField] TMP_Text[] titles;
        [SerializeField] TMP_Text[] effectLines;
        [SerializeField] TMP_Text[] cooldownLines;

        float openTime;
        bool closing;

        // показывает предложение, которое перед этим собрал AbilitySystem (OfferAll или OfferRandom)
        public void Show()
        {
            header.text = Strings.AbilityPickTitle;
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < abilities.OfferCount;
                slots[i].SetActive(used);
                if (!used) continue;

                AbilityDef def = abilities.Offer(i);
                icons[i].sprite = art.abilityIcons[(int)def.id];
                titles[i].text = Strings.AbilityTitle[(int)def.id];
                effectLines[i].text = string.Format(CultureInfo.InvariantCulture, Strings.AbilityEffect[(int)def.id], def.duration * loop.Stats.abilityDurationMul, Value(def.id));
                cooldownLines[i].text = string.Format(CultureInfo.InvariantCulture, Strings.CooldownFormat, def.cooldown * loop.Stats.abilityCooldownMul);
            }

            openTime = Time.unscaledTime;
            closing = false;
            group.alpha = 1f;
            group.blocksRaycasts = false;
            gameObject.SetActive(true);
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
            closing = true;
            group.blocksRaycasts = false;
            group.DOFade(0f, config.pickFadeOut).SetLink(gameObject).OnComplete(Close);
        }

        void Close()
        {
            gameObject.SetActive(false);
            loop.SetState(GameState.Playing);
        }

        // число в строке эффекта: {1} в Strings.AbilityEffect
        float Value(AbilityId id)
        {
            switch (id)
            {
                case AbilityId.Grow: return config.growRadiusMul;
                case AbilityId.Pulse: return config.pulseDamage;
                case AbilityId.Surge: return config.surgePowerMul;
                default: return 0f;
            }
        }
    }
}
