using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;
using Meta.XR;
using Newtonsoft.Json;
using System.Text;
using System.IO;

/// <summary>
/// Handles webcam streaming, sending frames to Roboflow,
/// receiving detections, and rendering tracked objects in 3D space.
/// </summary>
public class RoboflowCaller : MonoBehaviour
{
    [Header("Camera & Streaming")]
    [SerializeField] private RawImage imageDisplay; // UI display for webcam feed
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    private Texture2D texture2D = null; // Used for sending frames to Roboflow
    private bool isStreaming = false; // Streaming toggle

    [Header("3D Scene References")]
    [SerializeField] private EnvironmentRaycastManager envRaycastManager;
    [SerializeField] private GameObject GUI;
    [SerializeField] private GameObject leftHandController;
    [SerializeField] private GameObject CenterEyeAnchor;

    [Header("Tracked Marker Objects")]
    [SerializeField] private GameObject _markerPrefab; // Prefabs to instantiate
    [SerializeField] private List<string> rfClassNames; // Names of classes for UI
    [SerializeField] private List<int> rfClassIds; // IDs of classes for UI
    private Dictionary<int, RoboflowObject> _activeMarkerMap = new(); // runtime pool
    [SerializeField] private float minConfidence = 0.8f; // Detection confidence threshold

    [Header("Roboflow API Configuration")]
    [SerializeField] private string RF_MODEL = "emotion-57ymy/2"; // Model name for Roboflow
    [SerializeField] private string LOCAL_SERVER_IP_ADDRESS = "http://192.168.0.190:9001"; // Local server URL for Roboflow
    private RoboflowInferenceClient client; // API client

    private Texture2D result; // Texture for resized images
    private const int targetWidth = 512; // Target width for resized images
    private const int targetHeight = 512; // Target height for resized images


    // 1. A list to store the responses between calls
    private List<string> collectedResponses = new List<string>();
    // 2. The maximum number of responses you want to save
    private const int MaxResponsesToCollect = 10;

    private void Start()
    {
        // Initialize Roboflow client with local server URL
        client = new RoboflowInferenceClient(APIKeys.RF_API_KEY, LOCAL_SERVER_IP_ADDRESS);
        BuildObjectPool();

        result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        StartCoroutine(initCoroutine());
    }

    /// <summary>
    /// Sets up the object pool for Roboflow objects.
    /// </summary>
    private void BuildObjectPool()
    {
        // Build marker pool dynamically
        for (var i = 0; i < rfClassNames.Count; i++)
        {
            var instance = Instantiate(_markerPrefab, Vector3.zero, Quaternion.identity);
            var rfObject = instance.GetComponent<RoboflowObject>();
            rfObject.Init(rfClassNames[i], rfClassIds[i]); // Initialize with class name and ID
            _activeMarkerMap[rfObject.ClassID] = rfObject;
        }
    }

    /// <summary>
    /// Starts/stops the streaming coroutine.
    /// </summary>
    public void onStreamingButtonCLicked()
    {
        if (isStreaming)
        {
            isStreaming = false;
            clearPreviousMarkers();
            Debug.Log("Streaming stopped.");
        }
        else
        {
            GUI.SetActive(false);
            isStreaming = true;
            StartCoroutine(callRoboflow());
            Debug.Log("Streaming started.");
        }
    }

    private void updateTexture2D() {
        texture2D.SetPixels(webCamTextureManager.WebCamTexture.GetPixels());
        texture2D.Apply();
    }

    /// <summary>
    /// Initializes the webcam texture and sets it to the image display.
    /// </summary>
    private IEnumerator initCoroutine()
    {
        Debug.Log("initCoroutine started");

        // Wait for webcam to be ready
        while (webCamTextureManager.WebCamTexture == null)
            yield return null;

        if (imageDisplay != null && webCamTextureManager.WebCamTexture != null)
        {
            imageDisplay.texture = webCamTextureManager.WebCamTexture;
        }

        texture2D = new Texture2D(webCamTextureManager.WebCamTexture.width, webCamTextureManager.WebCamTexture.height, TextureFormat.RGB24, false);
        updateTexture2D();
    }

    /// <summary>
    /// Scales down a texture to the given dimensions.
    /// </summary>
    private Texture2D resizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Bilinear;
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private IEnumerator callRoboflow()
    {
        while (isStreaming)
        {
            if (texture2D == null)
            {
                yield return null;
                continue;
            }
            updateTexture2D();

            byte[] jpg = resizeTexture(texture2D, 512, 512).EncodeToJPG(80);
            string base64Image = Convert.ToBase64String(jpg);
            var image = new InferenceRequestImage("base64", base64Image);

            bool isDone = false;
            // Call Roboflow and wait for completion
            yield return StartCoroutine(client.InferObjectDetection(
                new ObjectDetectionInferenceRequest(RF_MODEL, image),
                response => { OnResponse(response); isDone = true; },
                error => { Debug.Log(error); isDone = true; }
            ));

            yield return new WaitUntil(() => isDone);
        }
    }

    /// <summary>
    /// Callback for successful inference response.
    /// </summary>
    private void OnResponse(ObjectDetectionInferenceResponse response)
    {
        if (response.Predictions != null && response.Predictions.Count > 0)
        {
            foreach (var pred in response.Predictions)
            { 
                Debug.Log($"Detected {pred.Class} at ({pred.X},{pred.Y}) confidence: {pred.Confidence}"); 
                collectedResponses.Add(pred.Class);

            }
            renderDetections(response.Predictions);
        }
        else
        {
            Debug.Log("No predictions found.");
        }
        // Check if we have collected enough responses
        if (collectedResponses.Count >= MaxResponsesToCollect)
        {
            //SaveResponsesToFile();

            // Clear the list to start collecting the next batch of 20
            collectedResponses.Clear();
        }
    }

    /*private void SaveResponsesToFile()
    {
        // Use a StringBuilder for efficient string construction
        var csvBuilder = new StringBuilder();
        // 1. Add the header row
        csvBuilder.AppendLine("ResponseIndex,PredictionIndex,Class,X,Y,Confidence");
        // 2. Loop through each response and its predictions
        for (int i = 0; i < collectedResponses.Count; i++)
        {
            var response = collectedResponses[i];
            if (response.Predictions != null)
            {
                for (int j = 0; j < response.Predictions.Count; j++)
                {
                    var pred = response.Predictions[j];
                    // Create a new line with the data
                    string line = $"{i},{j},{pred.Class},{pred.X},{pred.Y},{pred.Confidence}";
                    csvBuilder.AppendLine(line);
                }
            }
        }

        // 3. Define a path and save the file
        try
        {
            string filePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "detection_responses.csv");
            File.WriteAllText(filePath, csvBuilder.ToString());
            Debug.Log($"Successfully saved {MaxResponsesToCollect} responses to: {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save responses as CSV: {ex.Message}");
        }
    }*/


    /// <summary>
    /// Clears all previously tracked markers.
    /// </summary>
    private void clearPreviousMarkers()
    {
        foreach (var marker in _activeMarkerMap.Values)
        {
            if (marker == null) continue;
            marker.Disable();
        }
    }

    /// <summary>
    /// Returns an existing marker for a given class ID.
    /// </summary>
    private RoboflowObject checkForExistingMarker(int classID)
    {
        _activeMarkerMap.TryGetValue(classID, out var marker);
        return marker;
    }


    /// <summary>
    /// Projects 2D detections into 3D space using raycasting and renders marker objects. Copy from Rob's PCA samples.
    /// </summary>
    public void renderDetections(List<ObjectDetectionPrediction> predictions)
    {
        Vector2Int camRes = PassthroughCameraUtils.GetCameraIntrinsics(webCamTextureManager.Eye).Resolution;
        float halfWidth = targetWidth * 0.5f;
        float halfHeight = targetHeight * 0.5f;

        for (int i = 0; i < predictions.Count; i++)
        {
            ObjectDetectionPrediction prediction = predictions[i];

            if (prediction.Confidence < minConfidence)
            {
                Debug.Log($"Detection {i} below threshold.");
                continue;
            }

            RoboflowObject marker = checkForExistingMarker(prediction.Class_Id);
            if (marker == null)
            {
                Debug.Log($"No marker assigned for class {prediction.Class_Id}");
                continue;
            }

            // Convert center to pixel space
            float adjustedCenterX = prediction.X - halfWidth;
            float adjustedCenterY = prediction.Y - halfHeight;
            float perX = (adjustedCenterX + halfWidth) / targetWidth;
            float perY = (adjustedCenterY + halfHeight) / targetHeight;
            Vector2 centerPixel = new Vector2(perX * camRes.x, (1.0f - perY) * camRes.y);
            Ray centerRay = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.Eye, new Vector2Int(Mathf.RoundToInt(centerPixel.x), Mathf.RoundToInt(centerPixel.y)));

            if (!envRaycastManager.Raycast(centerRay, out var centerHit))
            {
                Debug.LogWarning("Raycast failed.");
                continue;
            }

            Vector3 markerWorldPos = centerHit.point;

            marker.SuccesfullyTracked(markerWorldPos, CenterEyeAnchor.transform.position);
            marker.SetDebugText(prediction.Class + " " + prediction.Confidence.ToString("F2"));
            Debug.Log($"Placed marker {i} at {markerWorldPos}");
        }
    }
}