using UnityEngine;

[CreateAssetMenu(menuName = "Stella/AI/Animal Personality", fileName = "AnimalPersonalitySO")]
public class AnimalPersonalitySO : ScriptableObject
{
    [Header("Temperament (0..1)")]
    [Range(0f, 1f)] public float baseCuriosity = 0.5f;
    [Range(0f, 1f)] public float baseAnxiety = 0.35f;
    [Range(0f, 1f)] public float baseTrust = 0.25f;

    [Header("Distances")]
    public float personalSpace = 2.0f;
    public float safeDistance = 6.5f;
    public float seeDistance = 18f;
}
