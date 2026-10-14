using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Строка списка достижений. Картинку и тексты ставит AchievementsView, остальное задано в префабе.
    public class AchievementRow : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text condition;
        [SerializeField] TMP_Text reward;
        // «12 / 25» у счётчиков, иначе пусто
        [SerializeField] TMP_Text progress;
        // золотая отметка выполненного
        [SerializeField] GameObject mark;

        public void Show(Sprite sprite, string name, string conditionLine, string rewardLine, string progressLine, bool done, float dimAlpha)
        {
            icon.sprite = sprite;
            // невыполненное: значок тусклый
            Color c = icon.color;
            c.a = done ? 1f : dimAlpha;
            icon.color = c;
            title.text = name;
            condition.text = conditionLine;
            reward.text = rewardLine;
            progress.text = progressLine;
            mark.SetActive(done);
        }
    }
}
