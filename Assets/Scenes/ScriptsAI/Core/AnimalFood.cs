using UnityEngine;

[DisallowMultipleComponent]
public class AnimalFood : MonoBehaviour
{
    [Header("Identity")]
    public AnimalFoodType foodType = AnimalFoodType.StarDust;

    [Header("Base Values (applied before preference modifiers)")]
    [Range(0f, 50f)] public float nutrition = 10f;          // Trust 증가 기본치
    [Range(0f, 50f)] public float calmDown = 12f;           // Anxiety 감소 기본치
    [Range(0f, 50f)] public float handFearHeal = 6f;        // HandFear 감소 기본치 (선택)

    [Header("Consume")]
    public bool consumed;
    public bool destroyOnConsume = true;

    /// <summary>먹이 “섭취” 처리. 실제 수치 반영은 Brain/Actions에서.</summary>
    public void Consume()
    {
        if (consumed) return;
        consumed = true;

        // 지금은 간단히 제거. (나중에 파티클/사운드 붙이기 쉬움)
        if (destroyOnConsume) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
