using UnityEngine;
using PassthroughCameraSamples;
using System.Collections;

public class UnsaidDetectionmanager : MonoBehaviour
{

    [SerializeField]
    BlazeFaceTensor blazeFaceTensor;


    [SerializeField] private OVRPassthroughLayer oVRPassthroughLayer;



    bool passthroughRunning = false;

    private void Awake()
    {
        oVRPassthroughLayer.passthroughLayerResumed.AddListener(OnPassthroughLayerResumed);
        // 1) We enable the passthrough layer to kick off its initialization process
        oVRPassthroughLayer.enabled = true;
        passthroughRunning = true;
    }

    private void OnDestroy()
    {
        oVRPassthroughLayer.passthroughLayerResumed.RemoveListener(OnPassthroughLayerResumed);
        passthroughRunning = false;
    }

    // 2) OnPassthroughLayerResumed is called once the layer is fully initialized and passthrough is visible
    private void OnPassthroughLayerResumed(OVRPassthroughLayer passthroughLayer)
    {

    }


    void Start()
    {
        StartCoroutine("CaptureEvery5Seconds");
    }

    IEnumerator CaptureEvery5Seconds()
    {
        while (true)
        {
            yield return new WaitForSeconds(5.0f);
            TryGetFace();
        }
    }


    void TryGetFace()
    {

        //early out when no passthrough available
        if (!passthroughRunning)
            return;

        Texture2D faceImage = blazeFaceTensor.LatestCroppedFaceImage;

        Debug.Log("running");


        if (faceImage == null)
        {
            Debug.Log("No face found");
        }
        else
        {
             Debug.Log("Face found and cropped");
        }
    }
}
