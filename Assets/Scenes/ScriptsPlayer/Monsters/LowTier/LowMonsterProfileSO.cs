using UnityEngine;

[CreateAssetMenu(menuName = "Stella/Monsters/Low Monster Profile", fileName = "LowMonsterProfile_")]
public class LowMonsterProfileSO : ScriptableObject
{
    [Header("Identity")]
    public string monsterName = "Low Monster";

    [Header("Perception")]
    [Min(0f)] public float detectRange = 10f;
    [Min(0f)] public float loseRange = 14f;
    [Min(0f)] public float attackRange = 1.5f;
    [Range(0f, 360f)] public float viewAngle = 130f;

    [Header("Patrol")]
    [Min(0f)] public float patrolRadius = 6f;
    [Min(0f)] public float patrolWaitTime = 1.4f;

    [Header("State Timing")]
    [Min(0f)] public float alertHoldTime = 0.35f;

    [Header("Always Move")]
    [Min(0f)] public float alwaysMoveMinDistance = 0.4f;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 2.2f;
    [Min(0f)] public float chaseSpeed = 3.8f;
    [Min(0f)] public float retreatSpeed = 4.6f;
    [Min(0f)] public float turnSpeed = 10f;

    [Header("Combat")]
    [Min(0f)] public float attackWindupTime = 0.75f;
    [Min(0f)] public float attackCooldownTime = 1.5f;
    [Min(0f)] public float attackActiveTime = 0.12f;

    [Min(0f)] public float attackDamage = 10f;
    [Min(0f)] public float knockbackForce = 6f;
    [Min(0f)] public float attackReach = 1.6f;
    [Min(0f)] public float attackDashDistance = 2.2f;


    [Header("Lantern Reaction")]
    [Min(0f)] public float lanternFearDistance = 6f;
    [Min(0f)] public float retreatDuration = 1.4f;
    [Min(0f)] public float retreatDistance = 5f;

    [Header("Simple Readability")]
    [Min(0f)] public float chaseHopAmplitude = 0.2f;
    [Min(0f)] public float chaseHopSpeed = 9f;
    [Min(0f)] public float windupShakeBoost = 1.6f;

    [Header("Presentation Colors")]
    public Color idleColor = new Color(0.65f, 0.75f, 0.9f);
    public Color patrolColor = new Color(0.7f, 0.8f, 0.95f);
    public Color alertColor = new Color(1f, 0.85f, 0.3f);
    public Color chaseColor = new Color(1f, 0.45f, 0.25f);
    public Color attackWindupColor = new Color(1f, 0.2f, 0.2f);
    public Color attackColor = new Color(1f, 0.15f, 0.15f);
    public Color cooldownColor = new Color(0.85f, 0.45f, 0.6f);
    public Color retreatColor = new Color(0.55f, 0.35f, 1f);

    [Header("Presentation Strength")]
    [Min(0f)] public float idleBobAmplitude = 0.03f;
    [Min(0f)] public float chaseShakeAmplitude = 0.06f;
    [Min(0f)] public float retreatShakeAmplitude = 0.1f;
    [Min(0f)] public float shakeSpeed = 18f;
    [Min(0f)] public float windupSquashY = 0.72f;
    [Min(0f)] public float windupStretchXZ = 1.18f;
    [Min(0f)] public float attackPunchScale = 1.15f;
    [Min(0f)] public float presentationLerpSpeed = 10f;

    [Header("Debug")]
    public bool showDebug = true;
    public bool logStateChanges = false;
}
