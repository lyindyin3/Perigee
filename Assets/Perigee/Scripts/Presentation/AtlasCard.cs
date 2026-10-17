using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Клетка вкладки КАРТОЧКИ: маленькая CardView или рубашка с «?». У рубашки эволюции видны значки её рецепта.
    public class AtlasCard : MonoBehaviour
    {
        [SerializeField] ArtSet art;
        [SerializeField] CardView view;
        [SerializeField] GameObject back;
        // рецепт на рубашке эволюции
        [SerializeField] GameObject recipe;
        [SerializeField] Image sourceA;
        [SerializeField] Image sourceB;

        public void ShowCard(CardDef card, bool seen)
        {
            view.gameObject.SetActive(seen);
            back.SetActive(!seen);
            if (seen)
            {
                view.Show(card, 0);
                return;
            }
            bool evolution = card.color == CardColor.Evolution;
            recipe.SetActive(evolution);
            if (!evolution) return;
            sourceA.sprite = art.cardIcons[(int)card.sourceA].sprite;
            sourceB.sprite = art.cardIcons[(int)card.sourceB].sprite;
        }

        // «Твои карточки» на паузе: взятая карточка с её стаками
        public void ShowTaken(CardDef card, int stacks)
        {
            view.gameObject.SetActive(true);
            back.SetActive(false);
            view.Show(card, stacks);
        }

        public void ShowTakenRelic(RelicDef relic, int stacks)
        {
            view.gameObject.SetActive(true);
            back.SetActive(false);
            int id = (int)relic.id;
            view.ShowRelic(art.relicIcons[id].sprite, Strings.RelicTitle[id], Strings.RelicText[id], stacks, relic.maxStacks);
        }

        public void ShowRelic(RelicDef relic, bool seen)
        {
            view.gameObject.SetActive(seen);
            back.SetActive(!seen);
            recipe.SetActive(false);
            if (!seen) return;
            int id = (int)relic.id;
            view.ShowRelic(art.relicIcons[id].sprite, Strings.RelicTitle[id], Strings.RelicText[id], 0, relic.maxStacks);
        }
    }
}
