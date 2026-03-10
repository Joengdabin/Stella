using System;
using UnityEngine;

public class AnimalPettable : MonoBehaviour
{
    [Header("Refs (optional)")]
    public AnimalBondSystem bond;           // 있으면 사용(지금은 직접 참조 안 함)
    public AnimalEmotionModel emotion;      // 있으면 사용

    [Header("Pet Range")]
    public float petRange = 1.6f;

    [Header("Accept/Reject")]
    public float acceptBondThreshold = 30f;
    public float rejectCooldown = 1.2f;
    public float spamPenalty = 0.25f;

    [Header("Bond Hook (temporary)")]
    [Range(0, 100)]
    public float debugBondValue = 0f;
    public float debugBondGainOnSuccess = 3f;

    public event Action<AnimalPettable, Transform> OnPetAccepted;
    public event Action<AnimalPettable, Transform> OnPetRejected;

    float _nextTryTime;
    float _spam;

    void Awake()
    {
        if (!bond) bond = GetComponent<AnimalBondSystem>();
        if (!emotion) emotion = GetComponent<AnimalEmotionModel>();
    }

    public bool CanPetNow => Time.time >= _nextTryTime;

    public bool TryPet(Transform player)
    {
        if (!CanPetNow) return false;

        // 사거리 체크(플레이어가 있으면 더 안전)
        if (player)
        {
            Vector3 a = transform.position; a.y = 0f;
            Vector3 p = player.position; p.y = 0f;
            float dist = Vector3.Distance(a, p);
            if (dist > petRange) return false;
        }

        float b = debugBondValue;
        float trust = emotion ? emotion.Trust : 0f;

        float t01 = Mathf.InverseLerp(0f, acceptBondThreshold, b);
        float baseChance = Mathf.Lerp(0.15f, 0.95f, t01);
        baseChance += trust * 0.01f;

        baseChance -= _spam * spamPenalty;

        bool accept = UnityEngine.Random.value < Mathf.Clamp01(baseChance);

        if (accept)
        {
            _spam = 0f;
            debugBondValue = Mathf.Clamp(debugBondValue + debugBondGainOnSuccess, 0f, 100f);

            OnPetAccepted?.Invoke(this, player);
            return true;
        }
        else
        {
            _spam = Mathf.Clamp01(_spam + 0.35f);
            _nextTryTime = Time.time + rejectCooldown;

            OnPetRejected?.Invoke(this, player);
            return false;
        }
    }
}