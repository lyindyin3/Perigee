using UnityEngine;
using UnityEngine.InputSystem;

namespace Perigee
{
    // Декор: слой фона плавно уходит в сторону, противоположную указателю (мышь или палец).
    // Дальним слоям ставят маленький depth, ближним большой; без указателя слой возвращается на место.
    public class Parallax : MonoBehaviour
    {
        // сдвиг в мировых единицах, когда указатель у края экрана
        [SerializeField] float depth = 0.3f;
        // скорость догоняния, 1/с
        [SerializeField] float follow = 3f;

        Vector3 start;
        Vector2 shift;

        void Awake()
        {
            start = transform.localPosition;
        }

        void Update()
        {
            // указатель в долях экрана от центра: -1..1 по каждой оси
            Vector2 target = Vector2.zero;
            Pointer pointer = Pointer.current;
            // мышь ведёт слой всегда, палец только пока касается экрана
            if (pointer is Mouse || (pointer != null && pointer.press.isPressed))
            {
                Vector2 p = pointer.position.ReadValue();
                target = new Vector2(p.x / Screen.width, p.y / Screen.height) * 2f - Vector2.one;
                target = Vector2.ClampMagnitude(target, 1f) * -depth;
            }
            shift = Vector2.Lerp(shift, target, 1f - Mathf.Exp(-follow * Time.unscaledDeltaTime));
            transform.localPosition = start + (Vector3)shift;
        }
    }
}
