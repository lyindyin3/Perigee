using UnityEngine;

namespace Perigee
{
    // Декор: объект едет вдоль оси X родителя и через length возвращается в начало.
    // Две копии картинки длиной length, поставленные встык, дают бесконечную полосу.
    public class Scroll : MonoBehaviour
    {
        [SerializeField] float speed = 0.4f;
        [SerializeField] float length = 32f;

        Vector3 start;
        float offset;

        void Awake()
        {
            start = transform.localPosition;
        }

        void Update()
        {
            offset = Mathf.Repeat(offset + speed * Time.deltaTime, length);
            transform.localPosition = start + Vector3.right * offset;
        }
    }
}
