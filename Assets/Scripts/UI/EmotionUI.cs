using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EmotionUI : MonoBehaviour
{

    public List<EmotionBar> EmotionBars;

    [SerializeField] TextMeshPro topExpressionField;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var emotionBar in EmotionBars)
        {
            emotionBar.UpdateEmotionValue(0);
        }
    }



    public void RefreshUI(string topExpression, float[] probs)
    {
        topExpressionField.text = topExpression;

        if (probs.Length != EmotionBars.Count)
        {
            Debug.LogError("Amount of UIEmotionBars is not equal amount of returned emotions");
            return;
        }

        for (int i = 0; i < probs.Length; i++)
        {
            EmotionBars[i].UpdateEmotionValue(probs[i]);
        }
    }
}
