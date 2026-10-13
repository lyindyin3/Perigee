using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Строка способности в Мастерской. Картинку и тексты ставит MenuView, остальное задано в префабе.
    public class ShopRow : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text effect;
        // длительность и перезарядка
        [SerializeField] TMP_Text timing;
        [SerializeField] Button buyButton;
        [SerializeField] TMP_Text buyLabel;

        // state = «Есть» или «Купить · цена»; кнопка нажимается, только когда покупка возможна
        public void Show(Sprite sprite, string name, string effectLine, string timingLine, string state, bool canBuy)
        {
            icon.sprite = sprite;
            title.text = name;
            effect.text = effectLine;
            timing.text = timingLine;
            buyLabel.text = state;
            buyButton.interactable = canBuy;
        }
    }
}
