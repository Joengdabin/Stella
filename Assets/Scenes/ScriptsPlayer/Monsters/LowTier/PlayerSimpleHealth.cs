using UnityEngine;

/// <summary>
/// 프로토타입용 단순 체력/피격 컴포넌트.
/// 몬스터가 공격 성공 시 데미지/넉백 전달.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSimpleHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Knockback")]
    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField] private CharacterController targetCharacterController;
    [SerializeField, Min(0f)] private float characterControllerKnockbackDuration = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    [SerializeField] private bool logDamage = true;

    private Vector3 _ccKnockbackVelocity;
    private float _ccKnockbackTimer;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        if (!targetRigidbody)
            targetRigidbody = GetComponent<Rigidbody>();

        if (!targetCharacterController)
            targetCharacterController = GetComponent<CharacterController>();

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void Update()
    {
        if (targetCharacterController == null) return;
        if (_ccKnockbackTimer <= 0f) return;

        _ccKnockbackTimer -= Time.deltaTime;
        targetCharacterController.Move(_ccKnockbackVelocity * Time.deltaTime);
        _ccKnockbackVelocity = Vector3.Lerp(_ccKnockbackVelocity, Vector3.zero, 10f * Time.deltaTime);
    }

    public void ApplyDamage(float damage, Vector3 hitDirection, float knockbackForce)
    {
        if (IsDead) return;

        float finalDamage = Mathf.Max(0f, damage);
        currentHealth = Mathf.Clamp(currentHealth - finalDamage, 0f, maxHealth);

        Vector3 dir = hitDirection;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            dir = -transform.forward;
        dir.Normalize();

        ApplyKnockback(dir, knockbackForce);

        if (logDamage)
            Debug.Log($"[PlayerSimpleHealth] Hit! -{finalDamage}, hp={currentHealth}/{maxHealth}", this);
    }

    private void ApplyKnockback(Vector3 dir, float force)
    {
        float f = Mathf.Max(0f, force);

        if (targetRigidbody != null && !targetRigidbody.isKinematic)
        {
            targetRigidbody.AddForce(dir * f, ForceMode.Impulse);
            return;
        }

        if (targetCharacterController != null)
        {
            _ccKnockbackVelocity = dir * f;
            _ccKnockbackTimer = characterControllerKnockbackDuration;
        }
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        GUI.Box(new Rect(10, 160, 280, 60), "PlayerSimpleHealth");
        GUI.Label(new Rect(20, 185, 260, 20), $"HP: {currentHealth:F1}/{maxHealth:F1}");
    }
}
