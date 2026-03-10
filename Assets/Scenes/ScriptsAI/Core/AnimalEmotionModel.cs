using UnityEngine;

public class AnimalEmotionModel : MonoBehaviour
{
    [Header("State (Read Only)")]
    [Range(0, 100)] public float Trust = 0f;
    [Range(0, 100)] public float Anxiety = 0f;

    // ✅ 기존 시스템(AnimalMemorySnapshot 등) 호환용 필드들
    [Range(0, 100)] public float Curiosity = 0f;
    [Range(0, 100)] public float HandFear = 0f;

    [Header("Noise Memory (Read Only / Used by Snapshot)")]
    public float NoiseMemory01 = 0f;
    public float NoiseMemory02 = 0f;

    [Header("Anxiety Gain (when player perceived)")]
    public float anxietyGainPerSec_WhenSeePlayer = 18f;
    public float anxietyGainPerSec_WhenNearButNotSeen = 8f;
    public float nearDistance = 4f;
    public float nearBonusGainPerSec = 10f;

    [Header("Stimulus Bonus")]
    public float loudNoiseBonus = 25f;
    public float lightShockBonus = 35f;

    [Header("Decay")]
    public float anxietyDecayPerSec = 12f;
    public float curiosityDecayPerSec = 6f;
    public float handFearDecayPerSec = 10f;
    public float noiseMemoryDecayPerSec = 8f;

    [Header("Clamp")]
    public float maxValue = 100f;

    /// <summary>
    /// 공용 Tick: Perception 결과 + 플레이어 자극으로 감정 갱신
    /// </summary>
    public void Tick(float dt, bool canSeePlayer, float distToPlayer, PlayerStimulusSource stimulus)
    {
        // --- base decays ---
        Anxiety = Mathf.Max(0f, Anxiety - anxietyDecayPerSec * dt);
        Curiosity = Mathf.Max(0f, Curiosity - curiosityDecayPerSec * dt);
        HandFear = Mathf.Max(0f, HandFear - handFearDecayPerSec * dt);

        // Noise memories decay (snapshot 호환용)
        NoiseMemory01 = Mathf.Max(0f, NoiseMemory01 - noiseMemoryDecayPerSec * dt);
        NoiseMemory02 = Mathf.Max(0f, NoiseMemory02 - noiseMemoryDecayPerSec * dt);

        // --- anxiety baseline gain (핵심: 0 고정 방지) ---
        if (canSeePlayer)
        {
            Anxiety += anxietyGainPerSec_WhenSeePlayer * dt;

            if (distToPlayer <= nearDistance)
                Anxiety += nearBonusGainPerSec * dt;
        }
        else
        {
            if (distToPlayer <= nearDistance)
                Anxiety += anxietyGainPerSec_WhenNearButNotSeen * dt;
        }

        // --- stimulus bonus ---
        if (stimulus != null)
        {
            if (stimulus.JustMadeLoudNoise)
            {
                Anxiety += loudNoiseBonus;
                NoiseMemory01 = Mathf.Min(maxValue, NoiseMemory01 + loudNoiseBonus * 0.6f);
                NoiseMemory02 = Mathf.Min(maxValue, NoiseMemory02 + loudNoiseBonus * 0.3f);
            }

            if (stimulus.LightShock)
            {
                Anxiety += lightShockBonus;
                NoiseMemory01 = Mathf.Min(maxValue, NoiseMemory01 + lightShockBonus * 0.6f);
                NoiseMemory02 = Mathf.Min(maxValue, NoiseMemory02 + lightShockBonus * 0.3f);
            }
        }

        // clamp
        Anxiety = Mathf.Clamp(Anxiety, 0f, maxValue);
        Curiosity = Mathf.Clamp(Curiosity, 0f, maxValue);
        HandFear = Mathf.Clamp(HandFear, 0f, maxValue);
        Trust = Mathf.Clamp(Trust, 0f, maxValue);
    }
}