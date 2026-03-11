using UnityEngine;

[DisallowMultipleComponent]
public class FoodQuickSlot : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InventoryComponent inventory;

    [Header("Input (PC test)")]
    [SerializeField] private KeyCode prevSlotKey = KeyCode.LeftBracket;
    [SerializeField] private KeyCode nextSlotKey = KeyCode.RightBracket;

    [Tooltip("Equip (PC test). Shift+R will force swap even if hand not empty.")]
    [SerializeField] private KeyCode equipKey = KeyCode.R;

    [Tooltip("Use one from hand (later: drop/throw).")]
    [SerializeField] private KeyCode useKey = KeyCode.F;

    [Header("Debug (dev only)")]
    [SerializeField] private bool showDebug = true;
    [SerializeField] private bool logDebug = true;

    [Header("Runtime (Read Only)")]
    [SerializeField] private int selectedSlotIndex = 0;

    [SerializeField] private ItemDefinitionSO heldItem;
    [SerializeField] private int heldAmount;
    [SerializeField] private int heldMax;

    public bool HandEmpty => heldItem == null || heldAmount <= 0;
    public ItemDefinitionSO HeldItem => heldItem;
    public int HeldAmount => heldAmount;
    public int HeldMax => heldMax;

    private void Awake()
    {
        if (!inventory) inventory = GetComponent<InventoryComponent>();
    }

    private void Update()
    {
        if (!inventory) return;

        // 선택 이동
        if (Input.GetKeyDown(prevSlotKey)) selectedSlotIndex--;
        if (Input.GetKeyDown(nextSlotKey)) selectedSlotIndex++;

        int count = inventory.SlotCount();
        if (count <= 0) selectedSlotIndex = 0;
        else selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, count - 1);

        // 장착 / 교체
        if (Input.GetKeyDown(equipKey))
        {
            bool forceSwap = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            EquipOrSwap(forceSwap);
        }

        // 임시: 1개 사용(다음 단계에서 Drop/Throw에 연결)
        if (Input.GetKeyDown(useKey))
            ConsumeOneFromHand();
    }

    private int HandMax(ItemDefinitionSO item)
    {
        if (item == null) return 0;
        if (!item.stackable) return 1;
        return Mathf.Max(1, item.maxStack); // 기본 5 / 나머지 3
    }

    /// <summary>
    /// forceSwap=false면 손이 비었을 때만 장착.
    /// forceSwap=true면 손이 차 있어도 교체 가능(기존 손 먹이는 인벤으로 반환).
    /// </summary>
    public void EquipOrSwap(bool forceSwap)
    {
        int count = inventory.SlotCount();
        if (count <= 0)
        {
            if (logDebug) Debug.Log("[QuickSlot] Inventory empty.");
            return;
        }

        var (item, amountInSlot) = inventory.GetSlot(selectedSlotIndex);
        if (item == null || amountInSlot <= 0) return;

        if (item.category != ItemCategory.Food)
        {
            if (logDebug) Debug.Log($"[QuickSlot] Not food: {item.displayName}");
            return;
        }

        // 손이 차있고 강제교체가 아니면 막기
        if (!HandEmpty && !forceSwap)
        {
            if (logDebug) Debug.Log("[QuickSlot] Hand not empty. (Hold swap to change)");
            return;
        }

        // 강제교체면: 기존 손 먹이를 인벤으로 되돌림
        if (!HandEmpty && forceSwap)
        {
            bool returned = inventory.TryAdd(heldItem, heldAmount);
            if (!returned)
            {
                // 인벤 꽉 차서 되돌릴 수 없으면 교체 불가
                if (logDebug) Debug.Log("[QuickSlot] Cannot swap: inventory full (cannot return held food).");
                return;
            }

            if (logDebug) Debug.Log($"[QuickSlot] Returned to inventory: {heldItem.displayName} x{heldAmount}");
            heldItem = null;
            heldAmount = 0;
            heldMax = 0;
        }

        // 이제 손이 비었으니 인벤에서 손 최대치만큼 꺼내기
        int max = HandMax(item);
        int take = Mathf.Min(max, amountInSlot);

        if (!inventory.TryTakeFromSlot(selectedSlotIndex, take, out var takenItem, out var takenAmount))
        {
            if (logDebug) Debug.Log("[QuickSlot] Take from inventory failed.");
            return;
        }

        heldItem = takenItem;
        heldAmount = takenAmount;
        heldMax = max;

        if (logDebug) Debug.Log($"[QuickSlot] Equipped to hand: {heldItem.displayName} x{heldAmount}/{heldMax}");
    }


    public bool TryTakeFromHand(int amount, out ItemDefinitionSO itemTaken, out int amountTaken)
    {
        itemTaken = null;
        amountTaken = 0;

        if (HandEmpty || amount <= 0) return false;

        int actual = Mathf.Min(amount, heldAmount);
        itemTaken = heldItem;
        amountTaken = actual;

        heldAmount -= actual;
        if (heldAmount <= 0)
        {
            heldItem = null;
            heldAmount = 0;
            heldMax = 0;
        }

        if (logDebug)
            Debug.Log($"[QuickSlot] Took from hand: {itemTaken.displayName} x{amountTaken}");

        return true;
    }
    public bool TryConsumeFromHand(int amount)
    {
        if (HandEmpty || amount <= 0) return false;

        if (amount > heldAmount) return false;

        heldAmount -= amount;
        if (heldAmount <= 0)
        {
            heldItem = null;
            heldAmount = 0;
            heldMax = 0;
        }
        return true;
    }

    // 임시 키 테스트용(다음 단계에서 drop/throw가 이걸 호출할 것)
    private void ConsumeOneFromHand()
    {
        if (HandEmpty)
        {
            if (logDebug) Debug.Log("[QuickSlot] Hand empty.");
            return;
        }

        TryConsumeFromHand(1);
        if (logDebug)
        {
            if (HandEmpty) Debug.Log("[QuickSlot] Hand is now empty.");
            else Debug.Log($"[QuickSlot] Hand left: {heldItem.displayName} x{heldAmount}/{heldMax}");
        }
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        GUI.Box(new Rect(10, 160, 600, 120), "QuickSlot Debug (dev only)");

        var (selItem, selAmount) = inventory ? inventory.GetSlot(selectedSlotIndex) : (null, 0);
        string sel = selItem ? $"{selItem.displayName} x{selAmount}" : "(none)";
        string held = heldItem ? $"{heldItem.displayName} x{heldAmount}/{heldMax}" : "(empty)";

        GUI.Label(new Rect(20, 185, 570, 20), $"Selected Slot [{selectedSlotIndex}] : {sel}   ([ / ])");
        GUI.Label(new Rect(20, 205, 570, 20), $"Hand : {held}");
        GUI.Label(new Rect(20, 225, 570, 20), $"PC test: R=Equip, Shift+R=Swap, F=ConsumeOne");
    }
}