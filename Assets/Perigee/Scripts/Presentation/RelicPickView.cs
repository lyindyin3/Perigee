using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Perigee
{
    // Экран реликвий: CardView в виде реликвии, без замен.
    public class RelicPickView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] RelicSystem relics;
        [SerializeField] AudioManager sfx;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text header;
        [SerializeField] CardView[] views;

        float openTime;
        bool closing;

        public void Show()
        {
            Typing.Show(header, Strings.RelicPickTitle);
            for (int i = 0; i < views.Length; i++)
            {
                bool used = i < relics.OfferCount;
                views[i].gameObject.SetActive(used);
                if (!used) continue;

                RelicDef relic = relics.Offer(i);
                int id = (int)relic.id;
                // точки показывают стаки после выбора
                views[i].ShowRelic(art.relicIcons[id].sprite, Strings.RelicTitle[id], Strings.RelicText[id], relics.Stacks(i) + 1, relic.maxStacks);
            }

            openTime = Time.unscaledTime;
            closing = false;
            group.alpha = 1f;
            group.blocksRaycasts = false;
            gameObject.SetActive(true);
            sfx.Play(SfxId.Relic);
        }

        void Update()
        {
            // до конца задержки нажатия проходят сквозь экран, как на экране карточек
            group.blocksRaycasts = !closing && Time.unscaledTime - openTime >= config.pickInputDelay;
        }

        // вызывают кнопки карточек, номер слота задан в сцене
        public void Pick(int slot)
        {
            if (closing) return;

            relics.Pick(slot);
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
