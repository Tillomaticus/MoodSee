using System;
using Unity.Mathematics;
using Unity.InferenceEngine;
using UnityEngine;
using PassthroughCameraSamples;

public class BlazeFaceTensor : MonoBehaviour
{

    [SerializeField] private WebCamTextureManager webCamTextureManager;


    [Header("BlazeFace Model")]
    public ModelAsset blazeFaceModel;
    public TextAsset anchorsCSV;

    private WebCamTexture savedTexture;
    public float iouThreshold = 0.3f;
    public float scoreThreshold = 0.5f;

    const int k_NumAnchors = 896;
    const int k_NumKeypoints = 6;
    const int detectorInputSize = 128;

    private float[,] m_Anchors;
    private Worker m_FaceDetectorWorker;
    private Tensor<float> m_DetectorInput;
    private Awaitable m_DetectAwaitable;

    public Rect? detectedFaceBox { get; private set; }
    public float detectedFaceScore { get; private set; }
    public Tensor<float> croppedFaceTensor { get; private set; }
    public Texture2D LatestCroppedFaceImage { get; private set; }


    public async void Start()
    {
        // Load anchors
        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);

        // Load model
        var faceDetectorModel = ModelLoader.Load(blazeFaceModel);

        // Build functional graph: input -> normalization -> outputs
        var graph = new FunctionalGraph();
        var input = graph.AddInput(faceDetectorModel, 0);
        var outputs = Functional.Forward(faceDetectorModel, 2 * input - 1);
        var boxes = outputs[0];  // (1, 896, 16)
        var scores = outputs[1]; // (1, 896, 1)
        var anchorsData = new float[k_NumAnchors * 4];
        Buffer.BlockCopy(m_Anchors, 0, anchorsData, 0, anchorsData.Length * sizeof(float));
        var anchorsTensor = Functional.Constant(new TensorShape(k_NumAnchors, 4), anchorsData);

        var idx_scores_boxes = BlazeUtils.NMSFiltering(
            boxes, scores, anchorsTensor, detectorInputSize, iouThreshold, scoreThreshold
        );
        faceDetectorModel = graph.Compile(idx_scores_boxes.Item1, idx_scores_boxes.Item2, idx_scores_boxes.Item3);

        // Create worker
        m_FaceDetectorWorker = new Worker(faceDetectorModel, BackendType.GPUCompute);

        // Allocate input tensor
        m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));

        // Start detection loop
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

    private async Awaitable Detect()
    {
        Texture texture = webCamTextureManager.WebCamTexture;
        if (texture == null)
            return;

        float texWidth = texture.width;
        float texHeight = texture.height;

        // Preprocess image into NHWC input tensor
        float size = Mathf.Max(texWidth, texHeight);
        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(
            BlazeUtils.TranslationMatrix(0.5f * (new Vector2(texWidth, texHeight) + new Vector2(-size, size))),
            BlazeUtils.ScaleMatrix(new Vector2(scale, -scale))
        );
        BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);

        m_FaceDetectorWorker.Schedule(m_DetectorInput);

        // Async readback
        var outputIndices = await (m_FaceDetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var outputScores = await (m_FaceDetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var outputBoxes = await (m_FaceDetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();

        int numFaces = outputIndices.shape.length;
        if (numFaces == 0)
        {
            detectedFaceBox = null;
            detectedFaceScore = 0f;
            croppedFaceTensor?.Dispose();
            croppedFaceTensor = null;
            await Awaitable.NextFrameAsync();
            return;
        }

        // Pick the highest-confidence face
        int bestIndex = 0;
        float bestScore = outputScores[0];
        for (int i = 1; i < numFaces; i++)
        {
            if (outputScores[i] > bestScore)
            {
                bestScore = outputScores[i];
                bestIndex = i;
            }
        }

        detectedFaceScore = bestScore;

        // Compute face bounding box in image space
        var idx = outputIndices[bestIndex];
        var anchorPos = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);
        var boxTopLeft = BlazeUtils.mul(M, anchorPos + new float2(outputBoxes[0, bestIndex, 0], outputBoxes[0, bestIndex, 1]));
        var boxBottomRight = BlazeUtils.mul(M, anchorPos + new float2(
            outputBoxes[0, bestIndex, 0] + 0.5f * outputBoxes[0, bestIndex, 2],
            outputBoxes[0, bestIndex, 1] + 0.5f * outputBoxes[0, bestIndex, 3])
        );

        detectedFaceBox = new Rect(
            boxTopLeft.x, boxTopLeft.y,
            boxBottomRight.x - boxTopLeft.x,
            boxBottomRight.y - boxTopLeft.y
        );


        croppedFaceTensor?.Dispose();
        LatestCroppedFaceImage = GetCroppedFaceTexture();
    }

    private Vector3 ImageToWorld(Vector2 position, float texWidth, float texHeight)
    {
        return (position - 0.5f * new Vector2(texWidth, texHeight)) / texHeight;
    }

    public Texture2D GetCroppedFaceTexture()
    {
        if (!detectedFaceBox.HasValue) return null;
        return BlazeUtils.CropFaceToTexture(savedTexture, detectedFaceBox.Value, 224, 224);
    }

    private void OnDestroy()
    {
        m_DetectAwaitable?.Cancel();
        m_FaceDetectorWorker?.Dispose();
        m_DetectorInput?.Dispose();
        croppedFaceTensor?.Dispose();
    }
}
