using UnityEngine;

[DisallowMultipleComponent]
public class AnimalBondSystem : MonoBehaviour
{
    [Header("Bond (0~100)")]
    public float bondMax = 100f;
    [Range(0f, 100f)] public float bond = 0f;

    [Header("Alpha (100+ bonus)")]
    public float alphaBonusMax = 50f;                 // 100 넘는 추가 구간
    [Range(0f, 50f)] public float alphaBonus = 0f;    // 현재 알파 보너스
    public bool InAlphaStage => bond >= bondMax;      // 100 도달하면 알파 가능

    [Header("Alpha Event 조건(공용)")]
    public int requiredPetCount = 3;
    public int requiredFeedCount = 3;
    public int petCount;
    public int feedCount;

    [Header("Hold (귀환 거부)")]
    public float holdSeconds = 8f;         // "아직 떠나기 싫어" 머무는 시간
    public float holdCooldownSeconds = 60f; // 너무 자주 터지지 않게
    public bool isHolding;
    public float holdTimeLeft;

    float _holdCooldownLeft;

    [Header("Bond Gain (기본값)")]
    public float bondPerPet = 3f;
    public float bondPerFeed_StarDust = 5f;
    public float bondPerFeed_Berry = 10f;
    public float bondPerFeed_Herb = 7f;

    void OnEnable() => AnimalBondEventManager.Register(this);
    void OnDisable() => AnimalBondEventManager.Unregister(this);

    void Update()
    {
        if (_holdCooldownLeft > 0f) _holdCooldownLeft -= Time.deltaTime;

        if (isHolding)
        {
            holdTimeLeft -= Time.deltaTime;
            if (holdTimeLeft <= 0f)
            {
                isHolding = false;
                holdTimeLeft = 0f;
            }
        }
    }

    public float TotalBond => bond + alphaBonus;

    public void AddBond(float amount)
    {
        bond = Mathf.Clamp(bond + amount, 0f, bondMax);
    }

    public void AddAlphaBonus(float amount)
    {
        alphaBonus = Mathf.Clamp(alphaBonus + amount, 0f, alphaBonusMax);
    }

    public void RegisterPet()
    {
        petCount++;
        AddBond(bondPerPet);
        TryRequestAlphaEvent();
    }

    public void RegisterFeed(AnimalFoodType foodType)
    {
        feedCount++;

        float gain = bondPerFeed_StarDust;
        if (foodType == AnimalFoodType.Berry) gain = bondPerFeed_Berry;
        else if (foodType == AnimalFoodType.Herb) gain = bondPerFeed_Herb;

        AddBond(gain);
        TryRequestAlphaEvent();
    }

    // ✅ 기존 FoodEater가 SendMessage("OnAteFood", food)을 쓰고 있으니
    // 이 이름/시그니처 그대로 받아서 "자동 연결"되게 해둠.
    public void OnAteFood(AnimalFood food)
    {
        if (!food) return;
        RegisterFeed(food.foodType);
    }

    public bool AlphaConditionsMet =>
        InAlphaStage && petCount >= requiredPetCount && feedCount >= requiredFeedCount;

    void TryRequestAlphaEvent()
    {
        if (!AlphaConditionsMet) return;
        AnimalBondEventManager.TryTriggerRandomAlphaEvent();
    }

    // ====== 귀환석 사용 시 호출할 공용 API ======
    public bool TryRefuseReturnStoneAndHold()
    {
        if (!AlphaConditionsMet) return false;
        if (_holdCooldownLeft > 0f) return false;
        if (isHolding) return false;

        // 여기서 "귀환 거부" 연출은 다른 창에서.
        StartHold();
        return true;
    }

    public void StartHold()
    {
        isHolding = true;
        holdTimeLeft = holdSeconds;
        _holdCooldownLeft = holdCooldownSeconds;

        // 알파 보너스 조금 지급 (원하면 수치 조절)
        AddAlphaBonus(10f);

        Debug.Log($"[Bond] HOLD started: {name} (Bond={bond:F1}, Alpha+={alphaBonus:F1})");
    }
}