using UnityEngine;

public enum ItemCategory
{
    Food,
    Quest,
    Material,
    Special // 귀환석 같은 예외용
}

[CreateAssetMenu(menuName = "Stella/Item Definition", fileName = "Item_")]
public class ItemDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string itemId = "stardust";
    public string displayName = "Star Dust";
    public ItemCategory category = ItemCategory.Food;

    [Header("Stack")]
    public bool stackable = true;
    public int maxStack = 20;

    [Header("Rules")]
    [Tooltip("체크하면 인벤토리 슬롯을 차지하지 않음(예: 귀환석).")]
    public bool doesNotConsumeInventorySlot = false;
}