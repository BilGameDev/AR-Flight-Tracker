using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Viridian.Utils
{
    public class UIDebugger : MonoBehaviour
    {
        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void Update()
        {
            bool clicked = false;
            Vector2 pos = default;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pos = Mouse.current.position.ReadValue();
                clicked = true;
            }

            if (!clicked && Touch.activeFingers.Count > 0)
            {
                foreach (var finger in Touch.activeFingers)
                {
                    if (finger.currentTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        pos = finger.screenPosition;
                        clicked = true;
                        break;
                    }
                }
            }

            if (!clicked) return;
            if (EventSystem.current == null) return;

            var pointerData = new PointerEventData(EventSystem.current) { position = pos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                Debug.Log(
                    $"[UIDebugger] #{i}: {r.gameObject.name}  " +
                    $"(depth={r.depth}, layer={r.gameObject.layer}, " +
                    $"type={r.module.GetType().Name})",
                    r.gameObject);
            }

            if (results.Count == 0)
                Debug.Log("[UIDebugger] No UI hit at " + pos);
        }
    }
}
