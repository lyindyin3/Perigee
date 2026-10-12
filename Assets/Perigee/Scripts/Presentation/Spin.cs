using UnityEngine;

namespace Perigee
{
    // Декор: вращает объект. Плюс = против часовой стрелки.
    public class Spin : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond;

        void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
        }
    }
}
