using UnityEngine;

public class PickupItemWorld : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private ItemDefinitionSO item;
    [SerializeField] private int amount = 1;

    [Header("Rules")]
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Debug")]
    [SerializeField] private bool logBlockedWhenFull = false;

    // ✅ 런타임에 드랍 아이템 세팅용
    public void Configure(ItemDefinitionSO newItem, int newAmount)
    {
        item = newItem;
        amount = Mathf.Max(1, newAmount);
    }

    public bool CanInteract(InteractorContext ctx)
    {
        if (item == null) return false;

        float d = Vector3.Distance(ctx.interactor.position, transform.position);
        if (d > interactRange) return false;

        var inv = ctx.interactor.GetComponent<InventoryComponent>();
        if (inv == null) return false;

        bool can = inv.CanAdd(item, amount);

        if (!can && logBlockedWhenFull)
            Debug.Log($"[Pickup BLOCKED] Inventory full: {item.displayName} x{amount}");

        return can;
    }

    public void Interact(InteractorContext ctx)
    {
        var inv = ctx.interactor.GetComponent<InventoryComponent>();
        if (inv == null) return;

        if (!inv.CanAdd(item, amount))
            return;

        bool added = inv.TryAdd(item, amount);
        if (!added) return;

        if (destroyOnPickup)
            Destroy(gameObject);
    }

    public string GetPrompt(InteractorContext ctx)
    {
        return item == null ? "줍기" : $"줍기 ({item.displayName})";
    }

    public Transform GetTransform() => transform;
}