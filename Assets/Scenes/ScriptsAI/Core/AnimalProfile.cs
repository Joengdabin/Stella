using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stella/Animal Profile", fileName = "AnimalProfile")]
public class AnimalProfile : ScriptableObject
{
    [Header("Identity")]
    public string animalId = "Rabbit";

    [Header("Core Temperament (0~2 typical)")]
    [Range(0f, 2f)] public float fearSensitivity = 1.0f;     // 위협(소리/급접근/빛)에 Anxiety가 얼마나 잘 오르는지
    [Range(0f, 2f)] public float trustSensitivity = 1.0f;    // 안전할 때 Trust가 얼마나 잘 오르는지
    [Range(0f, 2f)] public float curiositySensitivity = 1.0f;// 호기심 증가율
    [Range(0f, 2f)] public float calmRecovery = 1.0f;        // Anxiety 자연 감소 속도 배율

    [Header("Distances (meters)")]
    public float seeDistance = 18f;
    public float personalSpace = 2.0f;
    public float safeDistance = 6.5f;

    [Header("Movement Feel (m/s)")]
    public float speedWander = 2.8f;
    public float speedApproach = 3.2f;
    public float speedRetreat = 5.2f;
    public float speedFlee = 7.0f;

    [Header("Food Preferences")]
    public List<FoodPreference> foodPreferences = new List<FoodPreference>()
    {
        new FoodPreference { foodType = AnimalFoodType.StarDust, preference = FoodPreferenceLevel.Love,  trustMultiplier = 1.25f, calmMultiplier = 1.10f },
        new FoodPreference { foodType = AnimalFoodType.Berry,    preference = FoodPreferenceLevel.Like,  trustMultiplier = 1.10f, calmMultiplier = 1.00f },
        new FoodPreference { foodType = AnimalFoodType.Herb,     preference = FoodPreferenceLevel.Neutral,trustMultiplier = 1.00f, calmMultiplier = 1.00f },
    };

    [Serializable]
    public enum FoodPreferenceLevel
    {
        Hate = 0,
        Dislike = 1,
        Neutral = 2,
        Like = 3,
        Love = 4
    }

    [Serializable]
    public struct FoodPreference
    {
        public AnimalFoodType foodType;
        public FoodPreferenceLevel preference;

        [Tooltip("nutrition(Trust) 기본값에 곱해질 배율")]
        public float trustMultiplier;

        [Tooltip("calmDown(Anxiety 감소) 기본값에 곱해질 배율")]
        public float calmMultiplier;

        [Tooltip("handFearHeal 기본값에 곱해질 배율")]
        public float handFearMultiplier;
    }

    /// <summary>
    /// 먹이 타입에 대한 선호 설정을 가져옴. 없으면 Neutral(배율 1) 반환.
    /// </summary>
    public FoodPreference GetPreference(AnimalFoodType type)
    {
        for (int i = 0; i < foodPreferences.Count; i++)
        {
            if (foodPreferences[i].foodType == type)
                return Normalize(foodPreferences[i]);
        }

        // default neutral
        return new FoodPreference
        {
            foodType = type,
            preference = FoodPreferenceLevel.Neutral,
            trustMultiplier = 1f,
            calmMultiplier = 1f,
            handFearMultiplier = 1f
        };
    }

    static FoodPreference Normalize(FoodPreference p)
    {
        if (p.trustMultiplier <= 0f) p.trustMultiplier = 1f;
        if (p.calmMultiplier <= 0f) p.calmMultiplier = 1f;
        if (p.handFearMultiplier <= 0f) p.handFearMultiplier = 1f;
        return p;
    }

    /// <summary>
    /// “이 먹이를 먹었을 때” 적용될 최종 변화량을 계산(프로필 반영).
    /// 실제 적용(Trust/Anxiety/HandFear 값 변경)은 Brain/Emotion에서 수행.
    /// </summary>
    public void EvaluateFood(AnimalFood food, out float trustDelta, out float anxietyDelta, out float handFearDelta)
    {
        var pref = GetPreference(food.foodType);

        trustDelta = food.nutrition * pref.trustMultiplier;
        anxietyDelta = -food.calmDown * pref.calmMultiplier;        // 감소는 음수
        handFearDelta = -food.handFearHeal * pref.handFearMultiplier;

        // Hate/Dislike일 때는 “먹긴 먹어도” 효과를 깎거나 반대로 만들 수도 있음(옵션)
        if (pref.preference == FoodPreferenceLevel.Hate)
        {
            trustDelta *= 0.25f;
            anxietyDelta *= 0.25f;
        }
        else if (pref.preference == FoodPreferenceLevel.Dislike)
        {
            trustDelta *= 0.6f;
            anxietyDelta *= 0.6f;
        }
    }
}
