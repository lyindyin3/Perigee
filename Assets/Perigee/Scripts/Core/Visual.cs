using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;

namespace Perigee
{
    // Стоит на корне префаба. Код меняет вид только через него и только относительно того,
    // что задано в инспекторе: цвета Body и масштаба корня.
    public class Visual : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;

        Color baseColor;
        Vector3 baseScale;
        Color tint;
        Color flashColor;
        float alpha = 1f;
        // доля вспышки поверх цвета, твин ведёт её к нулю
        float flash;
        DOGetter<float> getFlash;
        DOSetter<float> setFlash;

        void Awake()
        {
            baseColor = body.color;
            baseScale = transform.localScale;
            tint = baseColor;
            getFlash = () => flash;
            setFlash = value =>
            {
                flash = value;
                Apply();
            };
        }

        public void SetAlpha(float a)
        {
            alpha = a;
            Apply();
        }

        // amount: 0 = свой цвет, 1 = color
        public void Tint(Color color, float amount = 1f)
        {
            tint = Color.Lerp(baseColor, color, amount);
            Apply();
        }

        public void Untint()
        {
            tint = baseColor;
            alpha = 1f;
            Apply();
        }

        public void Flash(Color color, float sec)
        {
            flashColor = color;
            flash = 1f;
            Apply();
            DOTween.Kill(this);
            DOTween.To(getFlash, setFlash, 0f, sec).SetTarget(this).SetLink(gameObject);
        }

        public void SetSizeMul(float m)
        {
            SetSizeMul(m, m);
        }

        public void SetSizeMul(float x, float y)
        {
            transform.localScale = new Vector3(baseScale.x * x, baseScale.y * y, baseScale.z);
        }

        void Apply()
        {
            Color color = Color.Lerp(tint, flashColor, flash);
            color.a *= alpha;
            body.color = color;
        }
    }
}
