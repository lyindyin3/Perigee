using UnityEngine;

namespace Perigee
{
    // Тьма Сердца Тьмы: чёрная картинка с мягкой прозрачной дырой. Масштаб делает радиус дыры равным радиусу света;
    // всё, что лежит ниже маски по сортировке, за дырой не видно.
    public class DarknessMask : MonoBehaviour
    {
        [Tooltip("Радиус прозрачной дыры картинки Body при масштабе 1, в единицах мира")]
        [SerializeField] float holeRadius = 1f;
        [Tooltip("Все чёрные части маски: Body и рамка вокруг него, закрывающая остальной экран")]
        [SerializeField] SpriteRenderer[] parts;

        Vector3 baseScale;
        float[] baseAlpha;

        void Awake()
        {
            baseScale = transform.localScale;
            baseAlpha = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) baseAlpha[i] = parts[i].color.a;
        }

        // radius = радиус света; alpha = доля непрозрачности из инспектора (НОЧНОЕ ЗРЕНИЕ делает тьму прозрачнее)
        public void Show(float radius, float alpha)
        {
            transform.localScale = baseScale * (radius / holeRadius);
            for (int i = 0; i < parts.Length; i++)
            {
                Color color = parts[i].color;
                color.a = baseAlpha[i] * alpha;
                parts[i].color = color;
            }
        }
    }
}
