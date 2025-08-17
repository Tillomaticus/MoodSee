using UnityEngine;
using PassthroughCameraSamples;
using PassthroughCameraSamples.MultiObjectDetection;

public class EmotionDetectionManager : MonoBehaviour
{

    [SerializeField] private WebCamTextureManager m_webCamTextureManager;

    [Header("Sentis inference ref")]
    [SerializeField] private SentisInferenceRunManager m_runInference;

    private bool isPaused = true;
    private float delayPauseBackTime = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Get the WebCamTexture CPU image
        var hasWebCamTextureData = m_webCamTextureManager.WebCamTexture != null;

        // Dont start a sentis inference if the app is paused or we don't have a valid WebCamTexture
        if (isPaused || !hasWebCamTextureData)
        {
            if (isPaused)
            {
                // Set the delay time for the A button to return from the pause menu
                delayPauseBackTime = 0.1f;
            }
            return;
        }

        // Run a new inference when the current inference finishes
        if (!m_runInference.IsRunning())
        {
            m_runInference.RunInference(m_webCamTextureManager.WebCamTexture);
        }
    }
}
