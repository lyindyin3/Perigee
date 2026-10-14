using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Иконка реликвии в ряду HUD над иконками карточек: картинка и число стаков. Ряд и логику реликвий строит M21.
    public class HudRelicIcon : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text stacks;

        public void Show(Sprite sprite, int stackCount)
        {
            icon.sprite = sprite;
            stacks.SetText("{0}", stackCount);
        }
    }
}
