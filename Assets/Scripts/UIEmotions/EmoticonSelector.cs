using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EmoticonSelector : MonoBehaviour
{


    [SerializeField] private Emotion _currentEmotion;

    [SerializeField] private SpriteRenderer _coronaSpriteRenderer;
    public List<EmotionMapping> _emotions = new List<EmotionMapping>();

    public static EmoticonSelector Instance;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }


    void Start()
    {
        hideObjects();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void changeEmotion(Emotion emotion)
    {
        Debug.Log("changing to " + emotion);
        _currentEmotion = emotion;
        hideObjects();
        foreach (var mapping in _emotions)
        {
            if (mapping.emotionType == emotion)
            {
                mapping.emotionPrefab.SetActive(true);
                ShowCorona(true, mapping.coronaColor);
            }
        }
    }
    private void hideObjects()
    {
        ShowCorona(false, Color.black);
        foreach (var m in _emotions)
        {
            m.emotionPrefab.SetActive(false);
        }
    }


    void ShowCorona(bool active, Color color)
    {
        _coronaSpriteRenderer.gameObject.SetActive(active);
        _coronaSpriteRenderer.color = color;
    }

}
