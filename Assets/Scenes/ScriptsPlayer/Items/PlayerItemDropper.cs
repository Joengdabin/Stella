using UnityEngine;

[DisallowMultipleComponent]
public class PlayerItemDropper : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InventoryComponent inventory;
    [SerializeField] private FoodQuickSlot quickSlot;

    [Header("Drop Prefab")]
    [Tooltip("DroppedPickup.prefab 연결 (PickupItemWorld 필요)")]
    [SerializeField] private GameObject dropPickupPrefab;

    [Header("Drop Tuning (Default)")]
    [SerializeField] private int dropItemCountMin = 1;
    [SerializeField] private int dropItemCountMax = 3;

    [Header("Drop Tuning (Low Monster)")]
    [SerializeField] private int lowMonsterDropItemCountMin = 0;
    [SerializeField] private int lowMonsterDropItemCountMax = 1;

    [Tooltip("각 드랍 시도에서 제거할 수량 (기본 1 추천)")]
    [SerializeField] private int dropAmountPerItem = 1;

    [Tooltip("플레이어 주변으로 흩뿌릴 반경")]
    [SerializeField] private float scatterRadius = 1.5f;

    [Tooltip("드랍 아이템 위쪽 임펄스")]
    [SerializeField] private float tossUpForce = 2.5f;

    [Tooltip("드랍 아이템 옆쪽 임펄스")]
    [SerializeField] private float tossSideForce = 2.0f;

    [Header("Rules")]
    [Tooltip("doesNotConsumeInventorySlot 아이템(예: 귀환석)은 드랍 제외")]
    [SerializeField] private bool neverDropNoSlotItems = true;

    [Header("Debug")]
    [SerializeField] private bool logDrop = true;

    void Awake()
    {
        if (!inventory) inventory = GetComponent<InventoryComponent>();
        if (!quickSlot) quickSlot = GetComponent<FoodQuickSlot>();
    }

    public void DropOnHit(int seed = -1)
    {
        DropOnHitRange(dropItemCountMin, dropItemCountMax, seed);
    }

    public void DropOnHitLowTier(int seed = -1)
    {
        DropOnHitRange(lowMonsterDropItemCountMin, lowMonsterDropItemCountMax, seed);
    }

    void DropOnHitRange(int minCount, int maxCount, int seed)
    {
        if (!inventory || !dropPickupPrefab) return;

        int minV = Mathf.Max(0, minCount);
        int maxV = Mathf.Max(minV, maxCount);

        if (seed >= 0)
            Random.InitState(seed);

        int dropCount = Random.Range(minV, maxV + 1);
        if (dropCount <= 0) return;

        for (int i = 0; i < dropCount; i++)
        {
            if (TryDropFromHandFirst())
                continue;

            TryDropFromInventory();
        }
    }

    bool TryDropFromHandFirst()
    {
        if (quickSlot == null) return false;
        if (quickSlot.HandEmpty) return false;

        if (!quickSlot.TryTakeFromHand(dropAmountPerItem, out var item, out var amount))
            return false;

        if (item == null || amount <= 0) return false;

        SpawnDroppedPickup(item, amount);
        if (logDrop) Debug.Log($"[Drop] From hand: {item.displayName} x{amount}");
        return true;
    }

    bool TryDropFromInventory()
    {
        int currentSlots = inventory.SlotCount();
        if (currentSlots <= 0) return false;

        int idx = Random.Range(0, currentSlots);
        var (it, amt) = inventory.GetSlot(idx);
        if (it == null || amt <= 0) return false;

        if (neverDropNoSlotItems && it.doesNotConsumeInventorySlot)
            return false;

        int removeAmt = Mathf.Min(dropAmountPerItem, amt);

        if (!inventory.TryRemoveFromSlot(idx, removeAmt, out var removedItem, out var removedAmount))
            return false;

        SpawnDroppedPickup(removedItem, removedAmount);
        if (logDrop) Debug.Log($"[Drop] From inventory: {removedItem.displayName} x{removedAmount}");
        return true;
    }

    void SpawnDroppedPickup(ItemDefinitionSO item, int amount)
    {
        Vector3 basePos = transform.position + Vector3.up * 0.2f;
        Vector2 rnd = Random.insideUnitCircle * scatterRadius;
        Vector3 pos = basePos + new Vector3(rnd.x, 0f, rnd.y);

        GameObject go = Instantiate(dropPickupPrefab, pos, Quaternion.identity);

        var pickup = go.GetComponent<PickupItemWorld>();
        if (pickup != null)
            pickup.Configure(item, amount);

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 side = new Vector3(rnd.x, 0f, rnd.y).normalized;
            rb.AddForce(Vector3.up * tossUpForce + side * tossSideForce, ForceMode.Impulse);
        }
    }
}
