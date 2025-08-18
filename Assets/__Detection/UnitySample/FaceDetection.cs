using System;
using Unity.Mathematics;
using Unity.InferenceEngine;
using UnityEngine;
using PassthroughCameraSamples;
using UnityEngine.Events;


[System.Serializable]
public class FaceDetectedEvent : UnityEvent<Vector3> { }


public class FaceDetection : MonoBehaviour
{

    public FaceDetectedEvent OnFaceDetected;


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


        Texture texture = webCamTextureManager.WebCamTexture;

        m_TextureWidth = texture.width;
        m_TextureHeight = texture.height;

        var size = Mathf.Max(texture.width, texture.height);

        // The affine transformation matrix to go from tensor coordinates to image coordinates
        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(BlazeUtils.TranslationMatrix(0.5f * (new Vector2(texture.width, texture.height) + new Vector2(-size, size))), BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
        BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);

        m_FaceDetectorWorker.Schedule(m_DetectorInput);

        var outputIndicesAwaitable = (m_FaceDetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var outputScoresAwaitable = (m_FaceDetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var outputBoxesAwaitable = (m_FaceDetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();

        using var outputIndices = await outputIndicesAwaitable;
        using var outputScores = await outputScoresAwaitable;
        using var outputBoxes = await outputBoxesAwaitable;

        var numFaces = outputIndices.shape.length;


    // TODO this is finding multiple faces, need to only take one.
        for (var i = 0; i < outputIndices.count; i++)
        {
            if (i >= numFaces)
                break;

            var idx = outputIndices[i];

            // Anchor position in input image space
            var anchorPosition = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);

            // Face center in image space
            var box_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(
                outputBoxes[0, i, 0],
                outputBoxes[0, i, 1]
            ));

            // Convert to world space
            var faceWorldPos = ImageToWorld(box_ImageSpace);

            // Log face position
            Debug.Log($"Face {i} World Position: {faceWorldPos}");
            OnFaceDetected.Invoke(faceWorldPos);

        }

        // if no faces are recognized then the awaitable outputs return synchronously so we need to add an extra frame await here to allow the main thread to run
        if (numFaces == 0)
            await Awaitable.NextFrameAsync();
    }

    void OnDestroy()
    {
        m_DetectAwaitable.Cancel();
    }
}
