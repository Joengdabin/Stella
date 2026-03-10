using UnityEngine;

public class AnimalOfflineBehavior : MonoBehaviour
{
    public GameObject basicFoodPrefab;

    void Start()
    {
        var offlineTime = OfflineTimeTracker.GetOfflineDuration();

        if (offlineTime.TotalHours > 5)
        {
            SpawnFood();
        }
    }

    void SpawnFood()
    {
        Instantiate(basicFoodPrefab, transform.position + Vector3.forward * 2f, Quaternion.identity);
    }
}