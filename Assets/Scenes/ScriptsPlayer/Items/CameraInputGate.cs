using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CameraInputGate : MonoBehaviour
{
    [Header("Assign your camera control script here")]
    [SerializeField] private MonoBehaviour cameraControllerScript; // 너가 만든 카메라 스크립트 드래그

    [Header("Region")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float rightRegionStartNormalizedX = 0.5f;

    private bool _blocked;
    private int _blockFingerId = -1;

    void Update()
    {
        if (!cameraControllerScript) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseGate();
#else
        HandleTouchGate();
#endif
    }

    bool IsInRightRegion(Vector2 screenPos)
    {
        float nx = screenPos.x / Screen.width;
        return nx >= rightRegionStartNormalizedX;
    }

    bool IsPointerOverUI(int fid)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(fid);
    }

    void HandleTouchGate()
    {
        if (_blocked)
        {
            // blockFinger가 끝나면 해제
            bool stillExists = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.fingerId != _blockFingerId) continue;

                stillExists = true;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    Unblock();
                    return;
                }
            }

            if (!stillExists) Unblock();
            return;
        }

        // 새 터치가 오른쪽 영역에서 시작하면 카메라 잠금
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase != TouchPhase.Began) continue;

            if (!IsInRightRegion(t.position)) continue;
            if (IsPointerOverUI(t.fingerId)) continue;

            Block(t.fingerId);
            break;
        }
    }

    void HandleMouseGate()
    {
        Vector2 pos = Input.mousePosition;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0) && IsInRightRegion(pos))
            Block(-1);

        if (_blocked && Input.GetMouseButtonUp(0))
            Unblock();
    }

    void Block(int fid)
    {
        _blocked = true;
        _blockFingerId = fid;
        cameraControllerScript.enabled = false;
    }

    void Unblock()
    {
        _blocked = false;
        _blockFingerId = -1;
        cameraControllerScript.enabled = true;
    }
}