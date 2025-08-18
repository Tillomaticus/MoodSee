using UnityEngine;

public enum Emotion { Sad, Happy, Angry, Neutral }

[System.Serializable]
public class EmotionMapping
{
    public Emotion emotionType;
    public GameObject emotionPrefab;
}