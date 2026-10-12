using UnityEngine;

namespace Perigee
{
    // Декор: при каждом включении объекта стирает старый след, чтобы он не тянулся с прошлого места.
    public class TrailReset : MonoBehaviour
    {
        [SerializeField] TrailRenderer trail;

        void OnEnable()
        {
            trail.Clear();
        }
    }
}
