using UnityEngine;

namespace Perigee
{
    // Красная дуга нагрева вокруг спутника. Цвет, толщина и материал заданы в LineRenderer, код ставит только точки.
    public class HeatArc : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] LineRenderer line;

        Vector3[] points;

        void Awake()
        {
            points = new Vector3[config.heatArcPoints];
        }

        // от верхней точки по часовой стрелке, полный круг при нагреве 1
        public void Draw(float heat, float time)
        {
            int segments = points.Length - 1;
            int n = Mathf.CeilToInt(heat * segments);
            int count = n > 0 ? n + 1 : 0;
            for (int k = 0; k < count; k++)
            {
                float rad = Mathf.Min((float)k / segments, heat) * 2f * Mathf.PI;
                points[k] = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f) * config.heatArcRadius;
            }
            line.positionCount = count;
            line.SetPositions(points);
            // у самого перегрева дуга мигает
            line.enabled = heat < config.heatFlickerFrom || Mathf.Repeat(time * config.heatFlickerHz, 1f) < 0.5f;
        }
    }
}
