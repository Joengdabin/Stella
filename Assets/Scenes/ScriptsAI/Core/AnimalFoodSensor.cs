using UnityEngine;

public class AnimalFoodSensor : MonoBehaviour
{
    public float detectRadius = 10f;
    public LayerMask foodMask = ~0;

    public bool TryFindNearestFood(out AnimalFood food)
    {
        food = null;

        Collider[] cols = Physics.OverlapSphere(
            transform.position,
            detectRadius,
            foodMask,
            QueryTriggerInteraction.Collide
        );

        float best = float.MaxValue;

        foreach (var c in cols)
        {
            if (!c) continue;

            // Tag 기반 필터
            if (!c.CompareTag("Food")) continue;

            var af = c.GetComponent<AnimalFood>();
            if (!af || af.consumed) continue;

            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                food = af;
            }
        }

        return food != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
#endif
}