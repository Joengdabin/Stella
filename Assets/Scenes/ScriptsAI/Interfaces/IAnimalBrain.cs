using UnityEngine;

public interface IAnimalBrain
{
    // 공용 상태 (ReactiveMover가 읽음)
    AnimalState CurrentAnimalState { get; }

    // 지각 정보 (ReactiveMover가 읽음)
    bool CanSeePlayer { get; }
    Vector3 PlayerPosition { get; }
    float DistToPlayer { get; }

    // ✅ 공용 업데이트(핵심)
    void Tick(float dt);
}