using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 모바일에서 퀵슬롯 버튼(또는 화면 특정 영역)의 탭/롱프레스 입력을 라우팅.
/// 지금은 UI가 없으니, 나중에 UI 버튼에 이 이벤트를 연결하면 됨.
/// PC에서는 테스트용 키로도 호출 가능.
/// </summary>
public class QuickSlotTouchRouter : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float longPressTime = 0.35f;

    [Header("Events")]
    public UnityEvent onQuickSlotTap;        // 비었을 때: 인벤 열기
    public UnityEvent onQuickSlotLongPress;  // 교체: 인벤 열기

    [Header("PC test keys")]
    [SerializeField] private KeyCode pcTapKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode pcLongKey = KeyCode.Alpha2;

    private bool _pressing;
    private float _pressStart;
    private bool _longFired;

    void Update()
    {
        // PC 테스트
        if (Input.GetKeyDown(pcTapKey)) onQuickSlotTap?.Invoke();
        if (Input.GetKeyDown(pcLongKey)) onQuickSlotLongPress?.Invoke();

        // 모바일: 화면 어디든 입력으로 처리하면 카메라랑 충돌하니,
        // 실제로는 "퀵슬롯 UI 버튼 영역"에서만 이 라우터가 호출되게 연결하는 게 정석.
        // 지금은 훅만 준비.
    }

    // UI Button의 EventTrigger(PointerDown/Up)로 연결할 함수들
    public void UI_PointerDown()
    {
        _pressing = true;
        _pressStart = Time.unscaledTime;
        _longFired = false;
    }

    public void UI_PointerUp()
    {
        if (!_pressing) return;
        _pressing = false;

        if (!_longFired)
            onQuickSlotTap?.Invoke();
    }

    void LateUpdate()
    {
        if (!_pressing || _longFired) return;

        if (Time.unscaledTime - _pressStart >= longPressTime)
        {
            _longFired = true;
            onQuickSlotLongPress?.Invoke();
        }
    }
}