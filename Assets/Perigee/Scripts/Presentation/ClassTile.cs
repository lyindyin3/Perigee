using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Плитка класса звезды: на экране эр (выбор) и на вкладке ЗВЁЗДЫ Мастерской (покупка). Тексты ставит MenuView.
    // Плитке выбора нужны lockOverlay, lockText и outline, плитке Мастерской — text, stats и label.
    public class ClassTile : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        // выбор: корень плитки; Мастерская: кнопка покупки
        [SerializeField] Button button;
        [SerializeField] GameObject lockOverlay;
        [SerializeField] TMP_Text lockText;
        [SerializeField] GameObject outline;
        [SerializeField] TMP_Text text;
        [SerializeField] TMP_Text stats;
        [SerializeField] TMP_Text label;

        // экран эр: закрытый класс тёмный с условием и не нажимается
        public void ShowPick(Sprite sprite, Color tint, string name, bool locked, string lockLine, bool selected)
        {
            Show(sprite, tint, name);
            lockOverlay.SetActive(locked);
            lockText.text = lockLine;
            outline.SetActive(selected);
            button.interactable = !locked;
        }

        // Мастерская: state = «Есть», цена или условие; кнопка нажимается, только когда покупка возможна
        public void ShowShop(Sprite sprite, Color tint, string name, string textLine, string statsLine, string state, bool canBuy)
        {
            Show(sprite, tint, name);
            text.text = textLine;
            stats.text = statsLine;
            label.text = state;
            button.interactable = canBuy;
        }

        void Show(Sprite sprite, Color tint, string name)
        {
            icon.sprite = sprite;
            icon.color = tint;
            title.text = name;
        }
    }
}
