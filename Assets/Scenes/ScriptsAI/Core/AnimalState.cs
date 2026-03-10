// Assets/Scenes/ScriptsAI/Core/AnimalState.cs
public enum AnimalState
{
    Idle,
    Wander,

    Observe,
    ObserveFar,        // ✅ RabbitObstacleLayer 호환

    Approach,
    ApproachCautious,  // ✅ RabbitObstacleLayer 호환

    Eat,
    
    BondHold, // 유대 이벤트: 귀환 거부 / 잠깐 더 머무르기
    
    Freeze,
    FreezeStartle,
    FreezeRecheck,

    Retreat,
    Flee,
    FleeDespawn,

    Nuzzle
}
