using System.Globalization;
using TMPro;
using UnityEngine;

namespace Perigee
{
    // Золотая нить между двумя спутниками в резонансе. Цвет, толщина и шрифт заданы в префабе, код ставит только точки.
    public class ResonanceThread : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] LineRenderer line;
        [SerializeField] TMP_Text label;

        Vector3[] points;
        // боковой сдвиг точек нити, у концов всегда 0
        float[] offsets;

        void Awake()
        {
            points = new Vector3[config.threadSegments + 1];
            offsets = new float[points.Length];
            line.positionCount = points.Length;
        }

        // множитель на подписи: SatelliteSystem ставит его, когда создаёт нить
        public void SetMultiplier(float mul)
        {
            label.text = string.Format(CultureInfo.InvariantCulture, Strings.MulFormat, mul);
        }

        public void Draw(Vector2 a, Vector2 b, bool reroll)
        {
            int segments = points.Length - 1;
            Vector2 normal = Vector2.Perpendicular(b - a).normalized;
            for (int k = 0; k <= segments; k++)
            {
                if (reroll && k > 0 && k < segments) offsets[k] = Random.Range(-config.threadJitter, config.threadJitter);
                points[k] = Vector2.Lerp(a, b, (float)k / segments) + normal * offsets[k];
            }
            line.SetPositions(points);
            label.transform.localPosition = (a + b) * 0.5f + normal * config.threadLabelOffset;
            gameObject.SetActive(true);
        }
    }
}
