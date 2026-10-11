using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Perigee
{
    public class InputHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] GameLoop loop;

        readonly HashSet<int> pointers = new HashSet<int>();
        InputAction holdKey;

        public bool IsHolding => pointers.Count > 0 || holdKey.IsPressed();

        void Awake()
        {
            holdKey = new InputAction(binding: "<Keyboard>/space");
            holdKey.Enable();
        }

        void OnDestroy()
        {
            holdKey.Dispose();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (loop.State == GameState.Playing) pointers.Add(eventData.pointerId);
        }

        // Unity шлёт OnPointerUp нажатому объекту, даже если указатель с него ушёл
        public void OnPointerUp(PointerEventData eventData)
        {
            pointers.Remove(eventData.pointerId);
        }

        public void Clear()
        {
            pointers.Clear();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) pointers.Clear();
        }
    }
}
