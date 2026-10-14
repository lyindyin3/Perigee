using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Строка испытания. Картинку и тексты ставит MenuView, остальное задано в префабе.
    public class ChallengeRow : MonoBehaviour
    {
        [SerializeField] TMP_Text number;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text era;
        [SerializeField] TMP_Text modifier;
        [SerializeField] TMP_Text reward;
        // картинка облика-награды; у награды пылью её нет
        [SerializeField] Image rewardPicture;
        // «Пройдено» или «Откроется в эре N»
        [SerializeField] TMP_Text status;
        [SerializeField] GameObject playButton;
        [SerializeField] TMP_Text playLabel;

        // playable = испытание открыто, кнопка «Играть» видна
        public void Show(int index, string name, string eraLine, string modifierLine, string rewardLine, string statusLine, bool playable)
        {
            number.SetText("{0}", index);
            title.text = name;
            era.text = eraLine;
            modifier.text = modifierLine;
            reward.text = rewardLine;
            rewardPicture.gameObject.SetActive(false);
            status.text = statusLine;
            playButton.SetActive(playable);
            playLabel.text = Strings.ChallengePlay;
        }

        // награда = облик спутника: картинка и цвет Body его префаба
        public void ShowSkin(SpriteRenderer body)
        {
            rewardPicture.color = body.color;
            ShowSkin(body.sprite);
        }

        // награда = облик звезды: её тело; цвет остаётся тем, что задан в префабе строки
        public void ShowSkin(Sprite sprite)
        {
            rewardPicture.sprite = sprite;
            rewardPicture.gameObject.SetActive(true);
        }
    }
}
