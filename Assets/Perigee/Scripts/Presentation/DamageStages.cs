using UnityEngine;

namespace Perigee
{
    // Трещины на враге: чем меньше HP, тем дальше стадия. Картинки стадий назначает владелец.
    public class DamageStages : MonoBehaviour
    {
        const int Hidden = -1;

        [SerializeField] SpriteRenderer view;
        // от лёгких повреждений к тяжёлым; пустой список ничего не показывает
        [SerializeField] Sprite[] stages;

        int shown = Hidden;

        void Awake()
        {
            view.enabled = false;
        }

        // fraction = hp / maxHp
        public void Show(float fraction)
        {
            int stage = Mathf.FloorToInt((1f - fraction) * (stages.Length + 1)) - 1;
            if (stage == shown) return;

            shown = stage;
            view.enabled = stage > Hidden;
            if (stage > Hidden) view.sprite = stages[stage];
        }
    }
}
