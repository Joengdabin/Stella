using UnityEngine;

/// <summary>
/// Low-tier 몬스터가 읽는 플레이어 위협/랜턴 입력 규약.
/// 실제 플레이어 시스템 연결 시 이 인터페이스 구현체만 교체하면 됨.
/// </summary>
public interface ILowMonsterThreatSource
{
    Transform ThreatTransform { get; }
    bool IsLanternOn { get; }
    float LanternFearMultiplier { get; }
}
