using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Плитка бестиария: значок и имя встреченного типа или чёрный силуэт и «???». Нажимается только встреченная.
    public class AtlasEntry : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        // рамка выбранной плитки
        [SerializeField] GameObject selection;

        public void Show(Sprite sprite, Color color, string name, bool seen, bool selected)
        {
            icon.sprite = sprite;
            icon.color = seen ? color : Color.black;
            title.text = seen ? name : Strings.Unknown;
            button.interactable = seen;
            selection.SetActive(selected);
        }
    }
}
