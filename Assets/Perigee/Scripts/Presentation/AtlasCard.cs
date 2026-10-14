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
