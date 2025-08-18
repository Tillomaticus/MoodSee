using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EmoticonSelector : MonoBehaviour
{
    

    [SerializeField] private Emotion _emotion;

    public List<EmotionMapping> _emotions = new List<EmotionMapping>();



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
        hideObjects();
        foreach (var mapping in _emotions)
        {
            if (mapping.emotionType == emotion)
            { mapping.emotionPrefab.SetActive(true); }
        }
    }
    private void hideObjects()
    {
        foreach (var m in _emotions)
        {
            m.emotionPrefab.SetActive(false);
        }
    }

}
