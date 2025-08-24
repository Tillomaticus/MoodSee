using System;
using Unity.Mathematics;
using Unity.InferenceEngine;
using UnityEngine;
using PassthroughCameraSamples;
using UnityEngine.Events;
using Meta.XR;
using Unity.VisualScripting;


[System.Serializable]
public struct FaceDetectionResult
{
    public bool isEmpty;
    public Vector3 worldPosition;  // center of the face in world space
    public Vector2 imageSize;      // width/height of the face in image space

    public FaceDetectionResult(Vector3 pos, Vector2 size, bool isEmptyState)
    {
        worldPosition = pos;
        imageSize = size;
        isEmpty = isEmptyState;
    }

    public FaceDetectionResult(bool isEmptyState) : this(Vector3.zero, Vector2.zero, isEmptyState)
    {
    }

}

[System.Serializable]
public class FaceDetectedEvent : UnityEvent<FaceDetectionResult> { }

public class FaceDetection : MonoBehaviour
{


    [SerializeField]
    DebugCamera debugCamera;

    [SerializeField]
    Camera vrCamera;


    public Texture2D CopiedTexture { get; private set; }
    public Texture2D CroppedFace { get; private set; }

    [SerializeField] int croppedTargetWidth = 48;
    [SerializeField] int croppedTargetHeight = 48;
    [SerializeField] bool greyScale = false;



    public FaceDetectedEvent OnFaceDetected;

    EnvironmentRaycastManager environmentRaycastManager;


    public ModelAsset faceDetector;
    public TextAsset anchorsCSV;

    public float iouThreshold = 0.3f;
    public float scoreThreshold = 0.5f;

    const int k_NumAnchors = 896;
    float[,] m_Anchors;

    const int k_NumKeypoints = 6;
    const int detectorInputSize = 128;

    Worker m_FaceDetectorWorker;
    Tensor<float> m_DetectorInput;
    Awaitable m_DetectAwaitable;

    [SerializeField]
    WebCamTextureManager webCamTextureManager;

    float m_TextureWidth;
    float m_TextureHeight;


    void Awake()
    {
        environmentRaycastManager = FindFirstObjectByType<EnvironmentRaycastManager>();
        if (environmentRaycastManager == null)
            Debug.LogWarning("No EnvironmentRaycastManager found in the scene!");

    }


    public async void Start()
    {
        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);

        var faceDetectorModel = ModelLoader.Load(faceDetector);

        // post process the model to filter scores + nms select the best faces
        var graph = new FunctionalGraph();
        var input = graph.AddInput(faceDetectorModel, 0);
        var outputs = Functional.Forward(faceDetectorModel, 2 * input - 1);
        var boxes = outputs[0]; // (1, 896, 16)
        var scores = outputs[1]; // (1, 896, 1)
        var anchorsData = new float[k_NumAnchors * 4];
        Buffer.BlockCopy(m_Anchors, 0, anchorsData, 0, anchorsData.Length * sizeof(float));
        var anchors = Functional.Constant(new TensorShape(k_NumAnchors, 4), anchorsData);
        var idx_scores_boxes = BlazeUtils.NMSFiltering(boxes, scores, anchors, detectorInputSize, iouThreshold, scoreThreshold);
        faceDetectorModel = graph.Compile(idx_scores_boxes.Item1, idx_scores_boxes.Item2, idx_scores_boxes.Item3);

        m_FaceDetectorWorker = new Worker(faceDetectorModel, BackendType.GPUCompute);

        m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));

        while (true)
        {
            try
            {
                m_DetectAwaitable = Detect();
                await m_DetectAwaitable;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        m_FaceDetectorWorker.Dispose();
        m_DetectorInput.Dispose();
    }

    Vector3 ImageToWorld(Vector2 position)
    {
        return (position - 0.5f * new Vector2(m_TextureWidth, m_TextureHeight)) / m_TextureHeight;
    }

    async Awaitable Detect()
    {
        if (webCamTextureManager == null)
        {
            Debug.Log("WebCamTextureManager is null");
            return;
        }

        if (webCamTextureManager.WebCamTexture == null)
        {
            Debug.Log("WebCamTexture is null");
            return;
        }


        WebCamTexture texture = webCamTextureManager.WebCamTexture;

        m_TextureWidth = texture.width;
        m_TextureHeight = texture.height;

        var size = Mathf.Max(texture.width, texture.height);

        // The affine transformation matrix to go from tensor coordinates to image coordinates
        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(BlazeUtils.TranslationMatrix(0.5f * (new Vector2(texture.width, texture.height) + new Vector2(-size, size))), BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
        BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);

        m_FaceDetectorWorker.Schedule(m_DetectorInput);

        var indicesTensor = m_FaceDetectorWorker.PeekOutput(0) as Tensor<int>;
        var scoresTensor = m_FaceDetectorWorker.PeekOutput(1) as Tensor<float>;
        var boxesTensor = m_FaceDetectorWorker.PeekOutput(2) as Tensor<float>;

        if (indicesTensor == null || scoresTensor == null || boxesTensor == null)
        {
            Debug.LogWarning("[Detect] One or more output tensors are null. Skipping this frame.");
            await Awaitable.NextFrameAsync();
            return;
        }

        using var outputIndices = await indicesTensor.ReadbackAndCloneAsync();
        using var outputScores = await scoresTensor.ReadbackAndCloneAsync();
        using var outputBoxes = await boxesTensor.ReadbackAndCloneAsync();

        var numFaces = outputIndices.shape.length;
        //    Debug.Log($"[Detect] Number of detected faces: {numFaces}");



        if (numFaces == 0)
        {
            //      Debug.Log("No faces detected");,
            //create empty detection result
            var detectionResult = new FaceDetectionResult(true);
            OnFaceDetected?.Invoke(detectionResult);
            await Awaitable.NextFrameAsync();
            return;
        }


        // Find the face with the highest score
        int dominantFaceIndex = 0;
        float maxScore = float.MinValue;

        for (int i = 0; i < outputIndices.count && i < numFaces; i++)
        {
            float score = outputScores[0, i, 0]; // adjust if your tensor shape is different
            if (score > maxScore)
            {
                maxScore = score;
                dominantFaceIndex = i;
            }
        }

        //   Debug.Log($"[Detect] Dominant face index: {dominantFaceIndex}, score: {maxScore}");


        // Use only the dominant face
        var idx = outputIndices[dominantFaceIndex];


        // Anchor in input image space
        var anchorPosition = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);

        // Box parameters
        var xCenter = outputBoxes[0, dominantFaceIndex, 0];
        var yCenter = outputBoxes[0, dominantFaceIndex, 1];
        var boxW = outputBoxes[0, dominantFaceIndex, 2];
        var boxH = outputBoxes[0, dominantFaceIndex, 3];

        var boxCenter = BlazeUtils.mul(M, anchorPosition + new float2(xCenter, yCenter));

        //    Debug.Log($"[Detect] Debug after boxCenter");

        // Convert to world space using Meta Passthrough + environment raycast
        Vector3 faceWorldPos;
        Quaternion faceRotation = Quaternion.identity;

        // Convert boxCenter (image space) to integer screen point
        var cameraScreenPoint = new Vector2Int((int)boxCenter.x, (int)boxCenter.y);

        // Create a ray from the passthrough camera
        var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(PassthroughCameraEye.Left, cameraScreenPoint);

        //    Debug.Log($"[Detect] Debug after ray");



        // Raycast against environment mesh
        if (environmentRaycastManager.Raycast(ray, out EnvironmentRaycastHit hitInfo))
        {
            faceWorldPos = hitInfo.point;
            faceRotation = Quaternion.LookRotation(hitInfo.normal, Vector3.up);
        }
        else
        {
            // Fallback: 2 units in front of the camera
            faceWorldPos = vrCamera.transform.position + vrCamera.transform.forward * 2f;
        }



        //    Debug.Log($"[Detect] Debug before Copy");
        CopyWebCamTexture(texture);


        //    Debug.Log($"[Detect] Debug before Update");
        if (CopiedTexture == null || debugCamera == null)
        {
            Debug.Log("[Detect] Copied Texture or Debug Camera is null");
        }
        else
        {
            //         Debug.Log($"[Detect] Debug before Rect");
            // Detector box in detector input space
            float boxX = boxCenter.x - boxW * 0.5f; // top-left x
            float boxY = boxCenter.y - boxH * 0.5f; // top-left y
            float boxWidth = boxW;
            float boxHeight = boxH;

            // Scale to the full webcam texture
            float scaleX = (float)CopiedTexture.width / detectorInputSize;
            float scaleY = (float)CopiedTexture.height / detectorInputSize;


            // expand the box for padding, so we get more of a portrait-style framing
            float expandFactor = 2f; // 2x bigger

            float scaledW = boxW * scaleX * expandFactor;
            float scaledH = boxH * scaleY * expandFactor;

            // boxCenter is in CopiedTexture pixel coordinates already
            //   float texX = boxCenter.x - (boxW * scaleX * 0.5f);
            //   float texY = (CopiedTexture.height - (boxCenter.y + boxH * scaleY * 0.5f)); // flip Y

            // Compute top-left corner in texture space (remember Unity's bottom-left origin)
            float texX = boxCenter.x - scaledW * 0.5f;
            float texY = CopiedTexture.height - (boxCenter.y + scaledH * 0.5f); // flip Y


            // Clamp inside texture
            texX = Mathf.Clamp(texX, 0, CopiedTexture.width - 1);
            texY = Mathf.Clamp(texY, 0, CopiedTexture.height - 1);
            scaledW = Mathf.Clamp(scaledW, 1, CopiedTexture.width - texX);
            scaledH = Mathf.Clamp(scaledH, 1, CopiedTexture.height - texY);


            Rect faceRect = new Rect(texX, texY, scaledW, scaledH);

            CroppedFace = CropFace(CopiedTexture, faceRect);
            Debug.Log($"Cropped rect: x={faceRect.x}, y={faceRect.y}, w={faceRect.width}, h={faceRect.height}, source={CopiedTexture.width}x{CopiedTexture.height}");

            debugCamera.UpdateDebugTexture(CroppedFace);

        }

        //    Debug.Log($"[Detect] Debug after Copy");
        // Invoke event with dominant face
        var detection = new FaceDetectionResult(faceWorldPos, new Vector2(boxW, boxH), false);
        OnFaceDetected?.Invoke(detection);

        Debug.Log($"Dominant face at {faceWorldPos}, box size {boxW}x{boxH},"); //cropped texture {croppedFace.width}x{croppedFace.height}");

    }

    void CopyWebCamTexture(WebCamTexture webCamTex)
    {
        if (webCamTex == null || !webCamTex.isPlaying)
            return;


        if (CopiedTexture != null)
        {
            Destroy(CopiedTexture); // release GPU memory
        }

        Texture2D tex2D = new Texture2D(webCamTex.width, webCamTex.height, TextureFormat.RGB24, false);
        tex2D.SetPixels(webCamTex.GetPixels());
        tex2D.Apply();
        CopiedTexture = tex2D;
    }


    // Utility: crop a face into a new Texture2D
    Texture2D CropFace(Texture2D source, Rect faceRect)
    {
        if (source == null || source.width == 0 || source.height == 0)
            return null;

        // Use Face Rect as it is, as we do the conversion earlier
        int x = Mathf.RoundToInt(faceRect.x);
        int y = Mathf.RoundToInt(faceRect.y);
        int width = Mathf.RoundToInt(faceRect.width);
        int height = Mathf.RoundToInt(faceRect.height);

        // Clamp so it doesn’t go outside the source texture
        x = Mathf.Clamp(x, 0, source.width - 1);
        y = Mathf.Clamp(y, 0, source.height - 1);
        if (x + width > source.width) width = source.width - x;
        if (y + height > source.height) height = source.height - y;

        // Read pixels into new texture
        Texture2D cropped = new Texture2D(width, height, source.format, false);
        Color[] pixels = source.GetPixels(x, y, width, height);
        cropped.SetPixels(pixels);
        cropped.Apply();

        return cropped;

    }

    public Texture2D RescaleAndGreyscale(Texture2D originalTexture)
    {
        // --- Step 1: Scale down using RenderTexture (faster & better quality than GetPixels) ---
        RenderTexture rt = RenderTexture.GetTemporary(croppedTargetWidth, croppedTargetHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(originalTexture, rt);

        Texture2D scaled = new Texture2D(croppedTargetWidth, croppedTargetHeight, TextureFormat.RGBA32, false);
        scaled.ReadPixels(new Rect(0, 0, croppedTargetWidth, croppedTargetHeight), 0, 0);
        scaled.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);


        if (greyScale)
        {
            // --- Step 2: Convert to grayscale ---
            Color[] pixels = scaled.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float gray = c.grayscale; // Unity built-in grayscale (0.299R + 0.587G + 0.114B)
                pixels[i] = new Color(gray, gray, gray, c.a);
            }
            scaled.SetPixels(pixels);
            scaled.Apply();
        }


        return scaled;
    }


    void OnDestroy()
    {
        m_DetectAwaitable.Cancel();
    }
}
