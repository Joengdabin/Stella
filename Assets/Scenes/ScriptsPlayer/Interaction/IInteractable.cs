using UnityEngine;

public interface IInteractable
{
    /// <summary>플레이어가 상호작용 "가능한 상태"인지(거리/조건 등)</summary>
    bool CanInteract(InteractorContext ctx);

    /// <summary>상호작용 실행</summary>
    void Interact(InteractorContext ctx);

    /// <summary>화면에 보여줄 짧은 문구(예: "줍기", "열기")</summary>
    string GetPrompt(InteractorContext ctx);

    /// <summary>대상을 대표하는 위치(하이라이트/디버그용)</summary>
    Transform GetTransform();
}

/// <summary>
/// 상호작용에 필요한 공용 정보(확장 쉬움)
/// </summary>
public struct InteractorContext
{
    public Transform interactor; // player
    public Camera camera;
    public Vector3 hitPoint;
    public Collider hitCollider;
}