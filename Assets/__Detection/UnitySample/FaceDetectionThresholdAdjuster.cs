using UnityEngine;

public class FaceDetectionThresholdAdjuster : MonoBehaviour

{
    [SerializeField]
    FaceDetection faceDetection;


    [Range(0f, 1f)]
    public float minScoreThreshold = 0.5f; // starting threshold

    public float step = 0.1f; // amount to increase/decrease per button press

    void Start()
    {
        minScoreThreshold = faceDetection.scoreThreshold;
    }

    void Update()
    {

        // "A" button increases threshold
        if (OVRInput.GetDown(OVRInput.Button.One)) // Button.One = A button
        {
            UpdatePositionScript.Instance.lerpSpeed += 1f;
            Debug.Log("LerpSpeed " + UpdatePositionScript.Instance.lerpSpeed);

        }

        // "B" button decreases threshold
        if (OVRInput.GetDown(OVRInput.Button.Two)) // Button.Two = B button
        {
            UpdatePositionScript.Instance.lerpSpeed -= 1f;
            Debug.Log("LerpSpeed " + UpdatePositionScript.Instance.lerpSpeed);

        }

        // // "A" button increases threshold
        // if (OVRInput.GetDown(OVRInput.Button.One)) // Button.One = A button
        // {
        //     minScoreThreshold = Mathf.Clamp(minScoreThreshold + step, 0f, 1f);
        //     Debug.Log($"Threshold increased: {minScoreThreshold:F2}");
        //     faceDetection.scoreThreshold = minScoreThreshold;
        // }

        // // "B" button decreases threshold
        // if (OVRInput.GetDown(OVRInput.Button.Two)) // Button.Two = B button
        // {
        //     minScoreThreshold = Mathf.Clamp(minScoreThreshold - step, 0f, 1f);
        //     Debug.Log($"Threshold decreased: {minScoreThreshold:F2}");
        //      faceDetection.scoreThreshold = minScoreThreshold;
        // }
    }
}
