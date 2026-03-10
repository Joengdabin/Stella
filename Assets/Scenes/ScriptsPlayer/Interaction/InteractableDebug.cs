using UnityEngine;

public class InteractableDebug : MonoBehaviour, IInteractable
{
    [Header("Tuning")]
    [SerializeField] private string prompt = "Interact";
    [SerializeField] private float interactRange = 3.0f;

    [Header("Debug Effect")]
    [SerializeField] private bool toggleActiveOnInteract = false;
    [SerializeField] private GameObject toggleTarget;

    public bool CanInteract(InteractorContext ctx)
    {
        float d = Vector3.Distance(ctx.interactor.position, transform.position);
        return d <= interactRange;
    }

    public void Interact(InteractorContext ctx)
    {
        Debug.Log($"[InteractableDebug] Interacted with: {name}");

        if (toggleActiveOnInteract && toggleTarget != null)
        {
            toggleTarget.SetActive(!toggleTarget.activeSelf);
        }
    }

    public string GetPrompt(InteractorContext ctx) => prompt;
    public Transform GetTransform() => transform;
}