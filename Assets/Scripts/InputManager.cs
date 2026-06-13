using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask placementLayermask;

    private Vector3 lastPosition;

    public event Action OnClicked, OnReleased, OnExit;

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Update()
    {
        bool began = false;
        bool ended = false;

        if (Touch.activeTouches.Count > 0)
        {
            var phase = Touch.activeTouches[0].phase;
            if (phase == UnityEngine.InputSystem.TouchPhase.Began) began = true;
            if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                phase == UnityEngine.InputSystem.TouchPhase.Canceled) ended = true;
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) began = true;
            if (Mouse.current.leftButton.wasReleasedThisFrame) ended = true;
        }

        if (began) OnClicked?.Invoke();
        if (ended) OnReleased?.Invoke();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            OnExit?.Invoke();
    }

    public bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

#if UNITY_ANDROID || UNITY_IOS
    if (Touch.activeTouches.Count > 0)
    {
        // New Input System touch IDs need PointerEventData check
        var touchPos = Touch.activeTouches[0].screenPosition;
        var pointerData = new UnityEngine.EventSystems.PointerEventData(EventSystem.current)
        {
            position = touchPos
        };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Count > 0;
    }
#endif

        return EventSystem.current.IsPointerOverGameObject();
    }

    public Vector3 GetSelectedMapPosition()
    {
        Vector2 screenPos;

        if (Touch.activeTouches.Count > 0)
            screenPos = Touch.activeTouches[0].screenPosition;
        else if (Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();
        else
            return lastPosition;

        Ray ray = sceneCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100, placementLayermask))
        {
            Debug.Log($"Raycast hit: {hit.collider.name}");
            lastPosition = hit.point;
        }
        else
        {
            Debug.Log("Raycast missed");
        }

        return lastPosition;
    }
}