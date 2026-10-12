using UnityEngine;

namespace Perigee
{
    // Кольцо фона, ближайшее к внутреннему спутнику, светится ярче остальных.
    public class RingHighlight : MonoBehaviour
    {
        [SerializeField] SatelliteSystem satellites;
        // радиус кольца = масштаб его объекта
        [SerializeField] SpriteRenderer[] rings;
        [SerializeField] float highlightAlpha = 0.9f;
        [SerializeField] float rate = 8f;

        float[] baseAlpha;

        void Awake()
        {
            baseAlpha = new float[rings.Length];
            for (int i = 0; i < rings.Length; i++) baseAlpha[i] = rings[i].color.a;
        }

        void Update()
        {
            // до первого забега спутников нет: все кольца в своей яркости
            int nearest = -1;
            if (satellites.Count > 0)
            {
                float r = satellites.OrbitRadius(0);
                float best = float.MaxValue;
                for (int i = 0; i < rings.Length; i++)
                {
                    float gap = Mathf.Abs(rings[i].transform.localScale.x - r);
                    if (gap >= best) continue;
                    best = gap;
                    nearest = i;
                }
            }

            for (int i = 0; i < rings.Length; i++)
            {
                Color color = rings[i].color;
                color.a = Mathf.Lerp(color.a, i == nearest ? highlightAlpha : baseAlpha[i], rate * Time.deltaTime);
                rings[i].color = color;
            }
        }
    }
}
