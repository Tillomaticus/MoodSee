using System.Collections;
using UnityEngine;
using Unity.InferenceEngine;

public class BlazeFaceCropper : MonoBehaviour
{
    [Header("BlazeFace Model")]
    public ModelAsset blazeFaceModel;
    public Vector2Int inputSize = new(128, 128); // BlazeFace input
    public BackendType backend = BackendType.CPU;

    private Worker engine;
    private bool modelLoaded = false;

    void Start()
    {
        LoadModel();
    }

    private void LoadModel()
    {
        var model = ModelLoader.Load(blazeFaceModel);
        engine = new Worker(model, backend);

        // Warm-up
        var dummy = new Texture2D(inputSize.x, inputSize.y);
        var t = TextureConverter.ToTensor(dummy, inputSize.x, inputSize.y, 3);
        engine.Schedule(t);

        modelLoaded = true;
        Debug.Log("BlazeFace model loaded.");
    }

    /// <summary>
    /// Run BlazeFace inference and return face bounding boxes in normalized coordinates (0..1).
    /// </summary>
    public void RunInference(Texture source, System.Action<Rect[]> onFacesDetected)
    {
        if (!modelLoaded) return;

        var input = TextureConverter.ToTensor(source, inputSize.x, inputSize.y, 3);
        engine.Schedule(input);

        StartCoroutine(PollForResult(input, onFacesDetected));
    }

    private IEnumerator PollForResult(Tensor<float> input, System.Action<Rect[]> callback)
    {
        yield return null; // wait one frame for schedule

        // Pull outputs (BlazeFace usually has [N,4] coords + [N] confidence)
        Tensor<float> out0 = engine.PeekOutput(0) as Tensor<float>; // bbox coords
        Tensor<float> out1 = engine.PeekOutput(1) as Tensor<float>; // confidence

        // Simple blocking readback
        out0.ReadbackRequest();
        out1.ReadbackRequest();

        while (!out0.IsReadbackRequestDone() || !out1.IsReadbackRequestDone())
            yield return null;

        out0 = out0.ReadbackAndClone();
        out1 = out1.ReadbackAndClone();

        int n = Mathf.Min(out0.shape[0], out1.shape[0]);
        var faces = new Rect[n];

        for (int i = 0; i < n; i++)
        {
            float conf = out1[i];
            if (conf < 0.5f) continue;

            // x, y, w, h normalized (BlazeFace output is already 0..1)
            faces[i] = new Rect(out0[i, 0], out0[i, 1], out0[i, 2], out0[i, 3]);
        }

        callback?.Invoke(faces);
    }
}
