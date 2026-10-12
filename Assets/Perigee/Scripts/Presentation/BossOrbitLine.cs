using UnityEngine;

namespace Perigee
{
    // Петля следующего эллипса Кометы. Цвет, толщина, пунктир и материал заданы в LineRenderer, код ставит только точки.
    public class BossOrbitLine : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] LineRenderer line;

        Vector3[] points;

        void Awake()
        {
            points = new Vector3[config.bossOrbitLinePoints];
        }

        // a, b = полуоси, e = эксцентриситет, phi = поворот эллипса в градусах; звезда в фокусе
        public void Show(float a, float b, float e, float phi)
        {
            float rad = phi * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            for (int k = 0; k < points.Length; k++)
            {
                float anomaly = 2f * Mathf.PI * k / points.Length;
                float x = a * (Mathf.Cos(anomaly) - e);
                float y = b * Mathf.Sin(anomaly);
                points[k] = new Vector3(x * cos - y * sin, x * sin + y * cos, 0f);
            }
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.enabled = true;
        }

        public void Hide()
        {
            line.enabled = false;
        }
    }
}
