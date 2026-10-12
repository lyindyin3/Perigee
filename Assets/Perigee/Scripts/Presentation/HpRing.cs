using UnityEngine;

namespace Perigee
{
    // Кремовая дуга HP вокруг врага или босса. Цвет, толщина и материал заданы в LineRenderer, код ставит только точки.
    public class HpRing : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] LineRenderer line;

        Vector3[] points;
        // показанная доля HP: плавно догоняет настоящую
        float shown;

        void Awake()
        {
            points = new Vector3[config.hpRingPoints];
        }

        // новый враг: дуга полная
        public void Fill()
        {
            shown = 1f;
        }

        // от верхней точки по часовой стрелке; дуга убывает плавно, чтобы был виден укус каждого удара
        public void Draw(float radius, float fraction, float dt)
        {
            shown = Mathf.Lerp(shown, fraction, config.hpRingLerp * dt);
            // корень врага повёрнут к звезде, а дуга всегда начинается сверху
            transform.rotation = Quaternion.identity;

            int segments = points.Length - 1;
            int n = Mathf.CeilToInt(shown * segments);
            int count = n > 0 ? n + 1 : 0;
            for (int k = 0; k < count; k++)
            {
                float rad = Mathf.Min((float)k / segments, shown) * 2f * Mathf.PI;
                points[k] = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f) * radius;
            }
            line.positionCount = count;
            line.SetPositions(points);
        }
    }
}
