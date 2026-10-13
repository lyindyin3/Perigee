using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Узел дерева чертежей в Мастерской. Картинку и тексты ставит MenuView, остальное задано в префабе.
    public class BlueprintNode : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text pips;
        // затемнение с замком: узел ещё закрыт
        [SerializeField] GameObject lockShade;
        // обводка выбранного узла
        [SerializeField] GameObject outline;

        public void Show(Sprite sprite, string name, string levelPips, bool locked, bool selected)
        {
            icon.sprite = sprite;
            title.text = name;
            pips.text = levelPips;
            lockShade.SetActive(locked);
            outline.SetActive(selected);
        }
    }
}
