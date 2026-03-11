using UnityEngine;

/// <summary>
/// 저등급 몬스터 시각 연출 전용.
/// - 색상
/// - 떨림
/// - 스케일(윈드업 압축/공격 펀치)
/// </summary>
[DisallowMultipleComponent]
public class LowMonsterPresentation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LowMonsterAI ai;
    [SerializeField] private LowMonsterProfileSO profileOverride;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Transform visualRoot;

    [Header("Debug")]
    [SerializeField] private bool logPresentation = false;

    private LowMonsterProfileSO _profile;
    private MaterialPropertyBlock _mpb;
    private Vector3 _baseLocalPos;
    private Vector3 _baseLocalScale;

    private Color _currentColor = Color.white;
    private float _attackPulse;
    private float _windup01;

    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        if (!ai) ai = GetComponent<LowMonsterAI>();
        _profile = profileOverride != null ? profileOverride : (ai ? ai.Profile : null);

        if (!targetRenderer)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (!visualRoot)
            visualRoot = targetRenderer ? targetRenderer.transform : transform;

        _mpb = new MaterialPropertyBlock();

        _baseLocalPos = visualRoot.localPosition;
        _baseLocalScale = visualRoot.localScale;

        if (ai != null)
        {
            ai.OnStateChanged += HandleStateChanged;
            ai.OnAttackWindupStarted += HandleWindupStart;
            ai.OnAttackTriggered += HandleAttackTriggered;
        }
    }

    private void OnDestroy()
    {
        if (ai != null)
        {
            ai.OnStateChanged -= HandleStateChanged;
            ai.OnAttackWindupStarted -= HandleWindupStart;
            ai.OnAttackTriggered -= HandleAttackTriggered;
        }
    }

    private void Update()
    {
        if (ai == null) return;
        if (_profile == null) _profile = ai.Profile;
        if (_profile == null) return;

        AnimateColor();
        AnimateBody();
    }

    private void AnimateColor()
    {
        Color target = GetStateColor(ai.CurrentState);
        _currentColor = Color.Lerp(_currentColor == default ? target : _currentColor, target, 1f - Mathf.Exp(-_profile.presentationLerpSpeed * Time.deltaTime));

        if (targetRenderer == null) return;

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorId, _currentColor);
        targetRenderer.SetPropertyBlock(_mpb);
    }

    private void AnimateBody()
    {
        if (visualRoot == null) return;

        float dt = Time.deltaTime;

        if (ai.CurrentState == LowMonsterState.AttackWindup)
        {
            _windup01 = Mathf.MoveTowards(_windup01, 1f, dt / Mathf.Max(0.01f, _profile.attackWindupTime));
        }
        else
        {
            _windup01 = Mathf.MoveTowards(_windup01, 0f, dt * 4f);
        }

        _attackPulse = Mathf.MoveTowards(_attackPulse, 0f, dt * 4f);

        float shakeAmp = 0f;
        if (ai.CurrentState == LowMonsterState.Chase)
            shakeAmp = _profile.chaseShakeAmplitude;
        else if (ai.CurrentState == LowMonsterState.AttackWindup)
            shakeAmp = _profile.chaseShakeAmplitude * _profile.windupShakeBoost;
        else if (ai.CurrentState == LowMonsterState.Retreat)
            shakeAmp = _profile.retreatShakeAmplitude;

        float noiseX = (Mathf.PerlinNoise(Time.time * _profile.shakeSpeed, 0f) - 0.5f) * 2f;
        float noiseZ = (Mathf.PerlinNoise(0f, Time.time * _profile.shakeSpeed) - 0.5f) * 2f;

        Vector3 shakeOffset = new Vector3(noiseX, 0f, noiseZ) * shakeAmp;

        float bobAmp = _profile.idleBobAmplitude;
        float bobSpeed = 2.1f;
        if (ai.CurrentState == LowMonsterState.Chase)
        {
            bobAmp = _profile.chaseHopAmplitude;
            bobSpeed = _profile.chaseHopSpeed;
        }

        float bob = Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobAmp;
        Vector3 targetPos = _baseLocalPos + shakeOffset + Vector3.up * bob;

        float windupScaleY = Mathf.Lerp(1f, _profile.windupSquashY, _windup01);
        float windupScaleXZ = Mathf.Lerp(1f, _profile.windupStretchXZ, _windup01);
        float attackPunch = Mathf.Lerp(1f, _profile.attackPunchScale, _attackPulse);

        Vector3 targetScale = new Vector3(
            _baseLocalScale.x * windupScaleXZ * attackPunch,
            _baseLocalScale.y * windupScaleY / attackPunch,
            _baseLocalScale.z * windupScaleXZ * attackPunch
        );

        float lerpT = 1f - Mathf.Exp(-_profile.presentationLerpSpeed * dt);
        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPos, lerpT);
        visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, lerpT);
    }

    private void HandleStateChanged(LowMonsterState prev, LowMonsterState next)
    {
        if (logPresentation)
            Debug.Log($"[LowMonsterPresentation] {name}: {prev} -> {next}", this);
    }

    private void HandleWindupStart()
    {
        _windup01 = Mathf.Max(_windup01, 0.05f);
    }

    private void HandleAttackTriggered()
    {
        _attackPulse = 1f;
    }

    private Color GetStateColor(LowMonsterState state)
    {
        switch (state)
        {
            case LowMonsterState.Patrol: return _profile.patrolColor;
            case LowMonsterState.Alert: return _profile.alertColor;
            case LowMonsterState.Chase: return _profile.chaseColor;
            case LowMonsterState.AttackWindup: return _profile.attackWindupColor;
            case LowMonsterState.Attack: return _profile.attackColor;
            case LowMonsterState.Cooldown: return _profile.cooldownColor;
            case LowMonsterState.Retreat: return _profile.retreatColor;
            default: return _profile.idleColor;
        }
    }
}
