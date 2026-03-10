using UnityEngine;

[System.Serializable]
public struct AnimalMemorySnapshot
{
    public float trust;
    public float anxiety;
    public float curiosity;
    public float handFear;
    public float noiseMemory01;

    public static AnimalMemorySnapshot From(AnimalEmotionModel emo)
    {
        return new AnimalMemorySnapshot
        {
            trust = emo.Trust,
            anxiety = emo.Anxiety,
            curiosity = emo.Curiosity,
            handFear = emo.HandFear,
            noiseMemory01 = emo.NoiseMemory01
        };
    }
}
