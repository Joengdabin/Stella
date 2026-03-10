using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DroppedPickupAutoDespawn : MonoBehaviour
{
    [Header("Despawn Time")]
    [Tooltip("체크하면 일정 시간이 지나면 자동 소멸")]
    [SerializeField] private bool enableTimedDespawn = true;

    [Tooltip("자동 소멸까지 걸리는 시간(초)")]
    [SerializeField] private float despawnSeconds = 20f;

    [Header("Global Count Limit")]
    [Tooltip("체크하면 씬 전체 드랍 아이템 수 제한 적용")]
    [SerializeField] private bool enableGlobalLimit = true;

    [Tooltip("씬에 존재 가능한 최대 드랍 아이템 수")]
    [SerializeField] private int maxDroppedItemCount = 20;

    [Header("Debug")]
    [SerializeField] private bool logDespawn = false;

    // 씬 전체 드랍 아이템 관리
    private static readonly List<DroppedPickupAutoDespawn> ActiveDrops = new List<DroppedPickupAutoDespawn>();

    private float timer;

    private void OnEnable()
    {
        timer = 0f;

        if (!ActiveDrops.Contains(this))
            ActiveDrops.Add(this);

        EnforceGlobalLimit();
    }

    private void OnDisable()
    {
        ActiveDrops.Remove(this);
    }

    private void Update()
    {
        if (!enableTimedDespawn) return;

        timer += Time.deltaTime;
        if (timer >= despawnSeconds)
        {
            if (logDespawn)
                Debug.Log($"[DroppedPickup] Timed despawn: {name}");

            Destroy(gameObject);
        }
    }

    private void EnforceGlobalLimit()
    {
        if (!enableGlobalLimit) return;
        if (maxDroppedItemCount <= 0) return;

        // 오래된 드랍부터 제거
        while (ActiveDrops.Count > maxDroppedItemCount)
        {
            var oldest = ActiveDrops[0];

            if (oldest == null)
            {
                ActiveDrops.RemoveAt(0);
                continue;
            }

            if (logDespawn)
                Debug.Log($"[DroppedPickup] Global limit despawn: {oldest.name}");

            ActiveDrops.RemoveAt(0);
            Destroy(oldest.gameObject);
        }
    }
}