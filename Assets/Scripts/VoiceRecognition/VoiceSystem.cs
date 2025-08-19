using UnityEngine;
using UnityEngine.Events;
using Oculus.Voice;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class VoiceSystem : MonoBehaviour
{
    // Current audio request for specific deactivation
    [SerializeField] private AppVoiceExperience _appVoiceExperience;

    [SerializeField] private UnityEvent _wakeWordDetected;

    [SerializeField] private UnityEvent<string> _completeTranscription;



    private string _dateId = "[DATE]";



    private bool _voiceCommandReady;

    private string _transcriptionText;

    static public VoiceSystem Instance;



    void Awake()
    {
        if (Instance == null)
            Instance = this;

        //_appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(ReactivateVoice);
        _appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        _appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);

        StartCoroutine(ActivateDelayed());

        //we are continously forcing the reactivate, this is not recommended and very hacky. 
        // This should be replaced by wake word or some proper activation
    }


    IEnumerator ContinouslyReactivateListener()
    {
        while (_appVoiceExperience.isActiveAndEnabled)
        {
            yield return null;
            if (_appVoiceExperience.Active)
                yield return null;
            else
                _appVoiceExperience.Activate();
        }
    }





    //do this to prevent an error 
    IEnumerator ActivateDelayed()
    {
        yield return null;
        _appVoiceExperience.Activate();
        yield return null;
        _appVoiceExperience.Deactivate();
        yield return null;

        _appVoiceExperience.Activate();

        _voiceCommandReady = true;

        StartCoroutine(ContinouslyReactivateListener());

    }

    private void ReactivateVoice() => _appVoiceExperience.Activate();

    public void WakeWordDetected(string[] arg0)
    {
        _voiceCommandReady = true;
        _wakeWordDetected.Invoke();
    }


    void OnPartialTranscription(string transcription)
    {
        if (!_voiceCommandReady) return;
        //  _transcriptionText = transcription;
        //also compare transcription to sentence?
    }

    void OnFullTranscription(string transcription)
    {
        if (!_voiceCommandReady) return;

        _voiceCommandReady = false;
        Debug.Log("Full transcription " + transcription);

        //compare transcription to sentence

        _appVoiceExperience.Deactivate();

        if (CompareToKeywords(transcription))
            StartNextScene();
        else
        {
            Debug.Log("Voiceline not valid, restarting listener");
            StartCoroutine(ActivateDelayed());
        }
        _completeTranscription?.Invoke(transcription);
    }

    private void OnDestroy()
    {
        //   _appVoiceExperience.VoiceEvents.OnRequestCompleted.RemoveListener(ReactivateVoice);
        _appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        _appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
    }



    // Format text with current datetime
    private string FormatText(string text)
    {
        string result = text;
        if (result.Contains(_dateId))
        {
            DateTime now = DateTime.UtcNow;
            string dateString = $"{now.ToLongDateString()} at {now.ToLongTimeString()}";
            result = text.Replace(_dateId, dateString);
        }
        return result;
    }



    bool CompareToKeywords(string transcriptionText)
    {
        //"I will now record your face and use ChatGPT to identify your expression"
        if ((transcriptionText.Contains("record") && transcriptionText.Contains("face")) ||
        (transcriptionText.Contains("identify") && transcriptionText.Contains("expression")))
            return true;

        return false;
    }

    void StartNextScene()
    {
        SceneManager.LoadScene("MoodSee");
    }


}
