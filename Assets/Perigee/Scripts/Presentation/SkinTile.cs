using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Плитка облика в Мастерской. Картинки и тексты ставит MenuView, остальное задано в префабе.
    public class SkinTile : MonoBehaviour
    {
        [SerializeField] Image picture;
        // лучи за картинкой: видны только у обликов звезды
        [SerializeField] Image rays;
        [SerializeField] TMP_Text title;
        [SerializeField] Button button;
        [SerializeField] TMP_Text label;

        // облик спутника: картинка и цвет Body его префаба
        public void Show(SpriteRenderer body, string name, string state, bool active)
        {
            picture.sprite = body.sprite;
            picture.color = body.color;
            rays.gameObject.SetActive(false);
            Show(name, state, active);
        }

        // облик звезды: тело и лучи; их цвета остаются теми, что заданы в префабе плитки
        public void Show(StarSkin skin, string name, string state, bool active)
        {
            picture.sprite = skin.body;
            rays.sprite = skin.rays;
            rays.gameObject.SetActive(true);
            Show(name, state, active);
        }

        // state = подпись кнопки: выбран, выбрать, цена или условие; active = кнопку можно нажать
        void Show(string name, string state, bool active)
        {
            title.text = name;
            label.text = state;
            button.interactable = active;
        }
    }
}
