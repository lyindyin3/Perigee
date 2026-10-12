using UnityEngine;

namespace Perigee
{
    // Декор: прозрачность картинки плавно ходит между min и max.
    public class PulseAlpha : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] float min;
        [SerializeField] float max = 1f;
        [SerializeField] float hz = 1.5f;

        void Update()
        {
            Color color = target.color;
            color.a = Mathf.Lerp(min, max, 0.5f + 0.5f * Mathf.Cos(Time.time * hz * 2f * Mathf.PI));
            target.color = color;
        }
    }
}
