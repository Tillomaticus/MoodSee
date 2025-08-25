using System;
using Meta.WitAi.CallbackHandlers;
using Meta.WitAi.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class EmotionBar : MonoBehaviour
{
    [SerializeField]
    float minScale;
    [SerializeField]
    float maxScale;

    [SerializeField]
    TextMeshPro actualValueField;

    [SerializeField] GameObject fillerAnchor;


    /// <summary>
    /// Update Emotion UI
    /// </summary>
    /// <param name="value">expects value to be between 0 and 1</param>
    public void UpdateEmotionValue(float value)
    {
        actualValueField.text = value.ToString("0.00");

        float newXScale = Mathf.Lerp(minScale, maxScale, value);
        newXScale = Mathf.Clamp(newXScale, minScale, maxScale);

        fillerAnchor.transform.localScale = new Vector3(newXScale, fillerAnchor.transform.localScale.y, fillerAnchor.transform.localScale.z);

    }


}
