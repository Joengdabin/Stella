using UnityEngine;

[DisallowMultipleComponent]
public class PlayerItemDropper : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InventoryComponent inventory;

    [Header("Drop Prefab")]
    [Tooltip("DroppedPickup.prefab 넣기 (PickupItemWorld 포함)")]
    [SerializeField] private GameObject dropPickupPrefab;

    [Header("Drop Tuning (Inspector only)")]
    [SerializeField] private int dropItemCountMin = 1;
    [SerializeField] private int dropItemCountMax = 3;

    [Tooltip("각 아이템에서 몇 개를 떨굴지(기본 먹이도 포함). 보통 1 추천")]
    [SerializeField] private int dropAmountPerItem = 1;

    [Tooltip("플레이어 주변 드랍 흩뿌리기 반경")]
    [SerializeField] private float scatterRadius = 1.5f;

    [Tooltip("드랍될 때 위로 살짝 튀는 힘")]
    [SerializeField] private float tossUpForce = 2.5f;

    [Tooltip("드랍될 때 옆으로 튀는 힘")]
    [SerializeField] private float tossSideForce = 2.0f;

    [Header("Rules")]
    [Tooltip("doesNotConsumeInventorySlot 아이템(예: 귀환석)은 드랍하지 않기")]
    [SerializeField] private bool neverDropNoSlotItems = true;

    [Header("Debug")]
    [SerializeField] private bool logDrop = true;

    void Awake()
    {
        if (!inventory) inventory = GetComponent<InventoryComponent>();
    }

    public void DropOnHit(int seed = -1)
    {
        if (!inventory || !dropPickupPrefab) return;

        int slotCount = inventory.SlotCount();
        if (slotCount <= 0) return;

        int dropCount = Random.Range(dropItemCountMin, dropItemCountMax + 1);
        dropCount = Mathf.Clamp(dropCount, 1, 999);

        // 슬롯이 줄어드므로, 반복마다 slotCount 갱신
        for (int i = 0; i < dropCount; i++)
        {
            int currentSlots = inventory.SlotCount();
            if (currentSlots <= 0) break;

            int idx = Random.Range(0, currentSlots);
            var (it, amt) = inventory.GetSlot(idx);
            if (it == null || amt <= 0) continue;

            if (neverDropNoSlotItems && it.doesNotConsumeInventorySlot)
                continue;

            int removeAmt = Mathf.Min(dropAmountPerItem, amt);

            if (!inventory.TryRemoveFromSlot(idx, removeAmt, out var removedItem, out var removedAmount))
                continue;

            SpawnDroppedPickup(removedItem, removedAmount);
        }
    }

    void SpawnDroppedPickup(ItemDefinitionSO item, int amount)
    {
        Vector3 basePos = transform.position + Vector3.up * 0.2f;
        Vector2 rnd = Random.insideUnitCircle * scatterRadius;
        Vector3 pos = basePos + new Vector3(rnd.x, 0f, rnd.y);

        GameObject go = Instantiate(dropPickupPrefab, pos, Quaternion.identity);

        // 런타임 세팅
        var pickup = go.GetComponent<PickupItemWorld>();
        if (pickup != null)
            pickup.Configure(item, amount);

        // 튀는 힘
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 side = new Vector3(rnd.x, 0f, rnd.y).normalized;
            rb.AddForce(Vector3.up * tossUpForce + side * tossSideForce, ForceMode.Impulse);
        }

        if (logDrop)
            Debug.Log($"[Drop] {item.displayName} x{amount}");
    }
}