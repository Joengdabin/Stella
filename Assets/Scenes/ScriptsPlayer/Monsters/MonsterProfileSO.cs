using UnityEngine;

public enum MonsterTier
{
    Low,   // 등불에 쫓겨남
    High   // 등불 무시
}

[CreateAssetMenu(menuName = "Stella/Monster Profile", fileName = "MonsterProfile_")]
public class MonsterProfileSO : ScriptableObject
{
    [Header("Identity")]
    public MonsterTier tier = MonsterTier.Low;

    [Header("Perception")]
    public float detectRange = 10f;
    public float loseRange = 14f;

    [Header("Movement")]
    public float chaseSpeed = 3.5f;
    public float fleeSpeed = 5.0f;

    [Header("Attack")]
    public float attackRange = 1.6f;
    public float attackCooldown = 2.0f;

    [Header("Lantern Reaction (Low-tier only)")]
    public int fleeAtLanternLevel = 3;      // 등불 레벨이 이 이상이면 도망
    public float fleeReactRange = 10f;      // 이 거리 안에서만 반응
    public float fleeDuration = 2.5f;       // 도망 유지 시간
}