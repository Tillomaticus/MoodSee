using UnityEngine;
using Unity.InferenceEngine;
using System.Linq;

public class EmotionDetector : MonoBehaviour
{
    public ModelAsset modelAsset;  // drag FERPlus.onnx here in Inspector
    private Worker worker;
    private Tensor<float> inputTensor;
    private int modelLayerCount;

    [Header("Testing")]
    public bool Activate = false;
    public Texture2D testImage;



    public static EmotionDetector Instance;



    private string[] expressions = new string[]
    {
        "Angry", "Disgust", "Fear", "Happy", "Sad", "Surprise", "Neutral"
    };


    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        var model = ModelLoader.Load(modelAsset);

        // Setup a simple pass-through graph
        var graph = new FunctionalGraph();
        var inputs = graph.AddInputs(model);
        var outputs = Functional.Forward(model, inputs);
        model = graph.Compile(outputs[0]);
        modelLayerCount = model.layers.Count;

        // Create the worker
        worker = new Worker(model, BackendType.GPUCompute);

        // Prepare input tensor 
        inputTensor = new Tensor<float>(new TensorShape(1, 1, 48, 48));
    }

    void Update()
    {
        if (Activate)
        {
            Activate = false;
            PredictEmotion(testImage);
        }
    }


    public string PredictEmotion(Texture2D faceTexture)
    {
        // Convert Texture2D to tensor
        TextureConverter.ToTensor(faceTexture, inputTensor, new TextureTransform());

        // Schedule inference
        var execution = worker.ScheduleIterable(inputTensor);
        while (execution.MoveNext()) { }

        // Get output tensor
        var outputTensor = (worker.PeekOutput() as Tensor<float>);

        // Convert output to probabilities and find max
        float[] logProbs = outputTensor.DownloadToArray();

        // Exponentiate to get probabilities
        float[] probs = new float[logProbs.Length];
        float sum = 0f;
        for (int i = 0; i < logProbs.Length; i++)
        {
            probs[i] = Mathf.Exp(logProbs[i]);
            sum += probs[i];
        }

        // Normalize so they sum to 1
        for (int i = 0; i < probs.Length; i++)
        {
            probs[i] /= sum;
        }


        // Debug.Log("Result :");
        // for (int i = 0; i < expressions.Length; i++)
        // {
        //     Debug.Log(expressions[i] + " : " + probs[i].ToString("F3"));
        // }


        // Find top prediction
        int topIndex = 0;
        float maxVal = probs[0];
        for (int i = 1; i < probs.Length; i++)
        {
            if (probs[i] > maxVal)
            {
                maxVal = probs[i];
                topIndex = i;
            }
        }

        Debug.Log("Top Expression: " + expressions[topIndex]);

        return expressions[topIndex];
    }

    void OnDestroy()
    {
        worker.Dispose();
        inputTensor.Dispose();
    }
}
