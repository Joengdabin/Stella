using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InventoryComponent : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public ItemDefinitionSO item;
        public int amount;
    }

    [Header("Capacity")]
    [SerializeField] private int maxSlots = 20;

    [Header("Debug")]
    [SerializeField] private bool logOnChange = true;

    [SerializeField] private List<Slot> slots = new List<Slot>();

    public int MaxSlots => maxSlots;
    public IReadOnlyList<Slot> Slots => slots;

    public int UsedSlotsCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item == null) continue;
                if (slots[i].item.doesNotConsumeInventorySlot) continue;
                count++;
            }
            return count;
        }
    }

    public bool CanAdd(ItemDefinitionSO item, int amount)
    {
        if (item == null || amount <= 0) return false;

        // 귀환석 같은 특수 아이템(슬롯 미소모)은 항상 OK(여기선 수량 제한만)
        if (item.doesNotConsumeInventorySlot) return true;

        // 스택 가능한 아이템이면 기존 스택에 일부라도 들어갈 수 있는지 확인
        if (item.stackable)
        {
            int remaining = amount;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.item == item && s.amount < item.maxStack)
                {
                    int space = item.maxStack - s.amount;
                    remaining -= space;
                    if (remaining <= 0) return true;
                }
            }

            // 새 슬롯이 필요한 경우
            int used = UsedSlotsCount;
            return used < maxSlots;
        }
        else
        {
            // 비스택은 수량만큼 슬롯 필요(간단 처리: 1개씩 슬롯)
            int needed = amount;
            int used = UsedSlotsCount;
            return (used + needed) <= maxSlots;
        }
    }

    public bool TryAdd(ItemDefinitionSO item, int amount)
    {
        if (!CanAdd(item, amount)) return false;

        if (item.doesNotConsumeInventorySlot)
        {
            // 슬롯을 쓰지 않는 아이템은 "전용 저장"으로 간단 처리:
            // slots 리스트에 넣되 UsedSlotsCount 계산에서 제외되도록 함.
            AddToSlots(item, amount);
            Changed($"Added (no-slot): {item.displayName} x{amount}");
            return true;
        }

        if (item.stackable)
        {
            int remaining = amount;

            // 1) 기존 스택 채우기
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.item == item && s.amount < item.maxStack)
                {
                    int space = item.maxStack - s.amount;
                    int add = Mathf.Min(space, remaining);
                    s.amount += add;
                    remaining -= add;
                    if (remaining <= 0)
                    {
                        Changed($"Added: {item.displayName} x{amount}");
                        return true;
                    }
                }
            }

            // 2) 새 슬롯 생성
            while (remaining > 0)
            {
                int add = Mathf.Min(item.maxStack, remaining);
                AddToSlots(item, add);
                remaining -= add;
            }

            Changed($"Added: {item.displayName} x{amount}");
            return true;
        }
        else
        {
            // 비스택: 1개씩 슬롯 추가
            for (int i = 0; i < amount; i++)
                AddToSlots(item, 1);

            Changed($"Added: {item.displayName} x{amount}");
            return true;
        }
    }

    private void AddToSlots(ItemDefinitionSO item, int amount)
    {
        var s = new Slot { item = item, amount = amount };
        slots.Add(s);
    }

    private void Changed(string msg)
    {
        if (!logOnChange) return;

        Debug.Log($"[Inventory] {msg} | UsedSlots={UsedSlotsCount}/{maxSlots}");
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.item == null) continue;
            Debug.Log($" - Slot {i}: {s.item.displayName} x{s.amount} (noSlot={s.item.doesNotConsumeInventorySlot})");
        }
    }
    
    public bool TryTakeFromSlot(int slotIndex, int takeAmount, out ItemDefinitionSO itemTaken, out int amountTaken)
    {
        itemTaken = null;
        amountTaken = 0;

        if (slotIndex < 0 || slotIndex >= slots.Count) return false;
        var s = slots[slotIndex];
        if (s.item == null || s.amount <= 0) return false;
        if (takeAmount <= 0) return false;

        int actual = Mathf.Min(takeAmount, s.amount);
        itemTaken = s.item;
        amountTaken = actual;

        s.amount -= actual;
        if (s.amount <= 0)
        {
            // 슬롯 비우기: 리스트에서 제거(동숲처럼 칸 하나가 사라지는 느낌)
            slots.RemoveAt(slotIndex);
        }

        if (logOnChange) Changed($"Took from slot: {itemTaken.displayName} x{amountTaken}");
        return true;
    }

    public int SlotCount() => slots.Count;

    public (ItemDefinitionSO item, int amount) GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return (null, 0);
        return (slots[index].item, slots[index].amount);
    }
    public int CountItem(ItemDefinitionSO item)
    {
        if (item == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.item == item) total += s.amount;
        }
        return total;
    }

    public bool TryConsumeItem(ItemDefinitionSO item, int amount)
    {
        if (item == null || amount <= 0) return false;

        int remaining = amount;

        // 앞 슬롯부터 차감(일관된 규칙)
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            var s = slots[i];
            if (s.item != item) continue;

            int take = Mathf.Min(s.amount, remaining);
            s.amount -= take;
            remaining -= take;

            if (s.amount <= 0)
            {
                slots.RemoveAt(i);
                i--; // Remove로 인덱스 당겨졌으니 보정
            }
        }

        bool ok = (remaining == 0);
        if (logOnChange)
            Changed(ok ? $"Consumed: {item.displayName} x{amount}" : $"Consume FAILED: {item.displayName} x{amount}");

        return ok;
    }
   
    public bool TryRemoveFromSlot(int slotIndex, int removeAmount, out ItemDefinitionSO removedItem, out int removedAmount)
    {
        removedItem = null;
        removedAmount = 0;

        if (slotIndex < 0 || slotIndex >= slots.Count) return false;
        var s = slots[slotIndex];
        if (s.item == null || s.amount <= 0) return false;
        if (removeAmount <= 0) return false;

        int actual = Mathf.Min(removeAmount, s.amount);
        removedItem = s.item;
        removedAmount = actual;

        s.amount -= actual;
        if (s.amount <= 0)
            slots.RemoveAt(slotIndex);

        if (logOnChange) Changed($"Removed(drop): {removedItem.displayName} x{removedAmount}");
        return true;
    }

}