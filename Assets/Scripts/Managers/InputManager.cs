using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ChaosTower.Managers
{
    public class InputManager : MonoBehaviour
    {
        public static UnityEvent OnTapEvent = new UnityEvent();

        public static InputManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            bool tapped = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) tapped = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) tapped = true;

            if (tapped)
            {
                // Ignore if over UI
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                Debug.Log("InputManager: Tap Detected via " + (Mouse.current != null ? "Mouse" : "Touch"));
                OnTapEvent?.Invoke();
            }
        }
    }
}
