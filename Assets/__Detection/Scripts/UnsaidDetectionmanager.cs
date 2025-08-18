using UnityEngine;
using PassthroughCameraSamples;
using System.Collections;

public class UnsaidDetectionmanager : MonoBehaviour
{

    [SerializeField]
    FaceDetection FaceDetection;

    [SerializeField]
    WebCamTextureManager webCamTextureManager;


    [SerializeField] private OVRPassthroughLayer oVRPassthroughLayer;



    bool passthroughRunning = false;

    private void Awake()
    {
        oVRPassthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);
        // 1) We enable the passthrough layer to kick off its initialization process
        oVRPassthroughLayer.enabled = true;
        passthroughRunning = true;
    }

    void Start()
    {
        StartCoroutine(WaitForInitialization());
    }

    IEnumerator WaitForInitialization()
    {
        while (webCamTextureManager.WebCamTexture == null)
            yield return null;


      //  Debug.Log("WebcamTexture populated, starting Face Detection");
        //if its initialized start FaceDetection
        FaceDetection.gameObject.SetActive(true);
       // FaceDetection.OnFaceDetected.AddListener(HandleFaceDetected);
    }

    public void HandleFaceDetected(FaceDetectionResult facePosition)
    {
        Debug.Log("[DetectionManager] FaceDetected, trying to capture");
        WebRequester.Instance.OnCaptureImage(FaceDetection.CopiedTexture);
    }


    private void OnDestroy()
    {
        oVRPassthroughLayer.passthroughLayerResumed.RemoveListener(OnPassthroughLayerResumed);
        passthroughRunning = false;
        FaceDetection.OnFaceDetected.RemoveListener(HandleFaceDetected);
    }

    // 2) OnPassthroughLayerResumed is called once the layer is fully initialized and passthrough is visible
    private void OnPassthroughLayerResumed(OVRPassthroughLayer passthroughLayer)
    {

    }





    void TryGetEmotion()
    {

        //early out when no passthrough available
        if (!passthroughRunning)
            return;


    }
}
