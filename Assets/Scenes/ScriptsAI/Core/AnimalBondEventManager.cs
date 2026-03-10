using UnityEngine;
using System.Collections.Generic;

public static class AnimalBondEventManager
{
    static readonly List<AnimalBondSystem> _animals = new List<AnimalBondSystem>();
    static float _globalCooldown = 0f;

    // 전체 이벤트 너무 남발 방지
    const float GLOBAL_COOLDOWN_SECONDS = 45f;

    // 조건을 충족한 상황에서 "랜덤 발생" 확률
    const float TRIGGER_CHANCE = 0.35f; // 35% (원하면 조절)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        _animals.Clear();
        _globalCooldown = 0f;
    }

    public static void Register(AnimalBondSystem a)
    {
        if (!a) return;
        if (!_animals.Contains(a)) _animals.Add(a);
    }

    public static void Unregister(AnimalBondSystem a)
    {
        if (!a) return;
        _animals.Remove(a);
    }

    public static void Tick(float dt)
    {
        if (_globalCooldown > 0f) _globalCooldown -= dt;
    }

    public static void TryTriggerRandomAlphaEvent()
    {
        if (_globalCooldown > 0f) return;
        if (_animals.Count == 0) return;

        if (Random.value > TRIGGER_CHANCE) return;

        // 후보: 알파 조건 충족 + 현재 hold 중이 아닌 동물
        List<AnimalBondSystem> candidates = new List<AnimalBondSystem>();
        for (int i = 0; i < _animals.Count; i++)
        {
            var a = _animals[i];
            if (!a) continue;
            if (!a.AlphaConditionsMet) continue;
            if (a.isHolding) continue;
            candidates.Add(a);
        }

        if (candidates.Count == 0) return;

        var selected = candidates[Random.Range(0, candidates.Count)];
        selected.StartHold();

        _globalCooldown = GLOBAL_COOLDOWN_SECONDS;
        Debug.Log($"[BondEvent] Random Alpha Event triggered for: {selected.name}");
    }
}