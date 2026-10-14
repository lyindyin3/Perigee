using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Карточка эры на экране эр. Картинку и тексты ставит MenuView, остальное задано в префабе.
    public class EraCard : MonoBehaviour
    {
        [SerializeField] Image picture;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text rule;
        [SerializeField] TMP_Text boss;
        [SerializeField] TMP_Text enemies;
        [SerializeField] TMP_Text best;
        // золотая отметка: в эре уже побеждали
        [SerializeField] GameObject wonMark;
        // затемнение с замком и условием поверх закрытой эры; оно же не пропускает нажатие
        [SerializeField] GameObject lockOverlay;
        [SerializeField] TMP_Text lockText;
        // рамка выбранной карточки
        [SerializeField] GameObject outline;

        public void Show(Sprite sprite, string name, string ruleLine, string bossLine, string enemiesLine, string bestLine, bool won, bool locked, string lockLine, bool selected)
        {
            picture.sprite = sprite;
            title.text = name;
            rule.text = ruleLine;
            boss.text = bossLine;
            enemies.text = enemiesLine;
            best.text = bestLine;
            wonMark.SetActive(won);
            lockOverlay.SetActive(locked);
            lockText.text = lockLine;
            outline.SetActive(selected);
        }
    }
}
