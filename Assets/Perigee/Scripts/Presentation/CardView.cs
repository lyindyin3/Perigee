using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Карточка на экране выбора. Рамку, кайму и иконку код берёт из ArtSet, тексты из Strings; остальное задано в префабе.
    public class CardView : MonoBehaviour
    {
        [SerializeField] ArtSet art;
        [SerializeField] Image frame;
        // золотая кайма: видна у редких карточек
        [SerializeField] GameObject rareRim;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text pips;
        [SerializeField] TMP_Text plusLine;
        [SerializeField] TMP_Text minusLine;

        // stacks = сколько стаков станет после выбора
        public void Show(CardDef card, int stacks)
        {
            int id = (int)card.id;
            frame.sprite = art.cardFrames[(int)card.color];
            rareRim.SetActive(card.rarity == CardRarity.Rare);
            icon.sprite = art.cardIcons[id];
            title.text = Strings.CardTitle[id];
            pips.text = Strings.Pips(stacks, card.maxStacks);
            plusLine.text = Strings.CardPlus[id];
            minusLine.text = Strings.CardMinus[id];
        }
    }
}
