using UnityEngine;

namespace Perigee
{
    // Декор: плавно раздувает объект от своего размера до scale и обратно.
    public class Pulse : MonoBehaviour
    {
        [SerializeField] float scale = 1.03f;
        [SerializeField] float hz = 1.5f;

        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
        }

        void Update()
        {
            float wave = 0.5f - 0.5f * Mathf.Cos(Time.time * hz * 2f * Mathf.PI);
            transform.localScale = baseScale * Mathf.Lerp(1f, scale, wave);
        }
    }
}
