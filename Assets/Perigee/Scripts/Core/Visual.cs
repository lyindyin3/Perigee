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
        // множитель поверх цвета из инспектора: цвет эры у звезды
        Color mul = Color.white;
        Color tint;
        Color flashColor;
        float alpha = 1f;
        // доля вспышки поверх цвета, твин ведёт её к нулю
        float flash;
        DOGetter<float> getFlash;
        DOSetter<float> setFlash;
        // толчок: масштаб поверх SetSizeMul раздувается на punch·sin(πt) и возвращается
        Vector2 size = Vector2.one;
        float punch;
        float punchT;
        DOGetter<float> getPunch;
        DOSetter<float> setPunch;

        public SpriteRenderer Body => body;

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
            getPunch = () => punchT;
            setPunch = value =>
            {
                punchT = value;
                Scale();
            };
        }

        public void SetAlpha(float a)
        {
            alpha = a;
            Apply();
        }

        // свой цвет умножается на color, пока не придёт другой множитель
        public void Multiply(Color color)
        {
            mul = color;
            tint = baseColor * mul;
            Apply();
        }

        // amount: 0 = свой цвет, 1 = color
        public void Tint(Color color, float amount = 1f)
        {
            tint = Color.Lerp(baseColor * mul, color, amount);
            Apply();
        }

        public void Untint()
        {
            tint = baseColor * mul;
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
            size = new Vector2(x, y);
            Scale();
        }

        // объект на миг раздувается на долю amount и за sec возвращается к своему размеру
        public void Punch(float amount, float sec)
        {
            punch = amount;
            DOTween.Kill(transform);
            setPunch(0f);
            DOTween.To(getPunch, setPunch, 1f, sec).SetEase(Ease.Linear).SetTarget(transform).SetLink(gameObject);
        }

        void Scale()
        {
            float m = 1f + punch * Mathf.Sin(punchT * Mathf.PI);
            transform.localScale = new Vector3(baseScale.x * size.x * m, baseScale.y * size.y * m, baseScale.z);
        }

        void Apply()
        {
            Color color = Color.Lerp(tint, flashColor, flash);
            color.a *= alpha;
            body.color = color;
        }
    }
}
