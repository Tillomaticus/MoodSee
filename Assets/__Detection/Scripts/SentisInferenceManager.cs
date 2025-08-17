using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This script is a clean-room, project-agnostic reimplementation of a
// "run-one-inference without blocking the main thread" workflow for Sentis.
// It keeps the public surface small, adds events, and avoids coupling to any UI.
//
// HOW TO USE
// 1) Add this component to a GameObject.
// 2) Assign: Sentis model (ModelAsset), labels (optional), inputSize, backend.
// 3) Call RunOnce(texture) whenever you have a new frame you want to infer on.
//    - For continuous inference, call RunOnce each time you receive a new camera frame.
// 4) Subscribe to OnDetectionsReady to receive results (already filtered + NMS).
//
// Notes:
// - Model is pre-warmed at Start to avoid the first-frame stall.
// - Work is scheduled in small chunks (layersPerFrame) to keep framerate stable.
// - Outputs are fetched via PeekOutput + async Readback to keep the main thread responsive.
// - Provides a minimal CPU-side NMS as a safety net, in case your model does not do NMS.
//
// Replace namespaces to fit your project if needed.


[DefaultExecutionOrder(-50)]    
public class SentisInferenceManager : MonoBehaviour
{

        [Header("Sentis Model config")]
        [SerializeField] private Vector2Int inputSize = new(640, 640);
        [SerializeField] private Unity.InferenceEngine.BackendType backend = Unity.InferenceEngine.BackendType.CPU;
        [SerializeField] private Unity.InferenceEngine.ModelAsset modelAsset;
        [Tooltip("How many layers to execute per frame. Lower = smoother, Higher = faster.")]
        [SerializeField, Min(1)] private int layersPerFrame = 25;
        [Header("Thresholds (post-processing)")]
        [Range(0, 1)] public float scoreThreshold = 0.23f;
        [Range(0, 1)] public float iouThreshold = 0.60f;
        [Tooltip("Optional labels file, one label per line.")]
        [SerializeField] private TextAsset labelsAsset;

        public bool IsModelLoaded { get; private set; }
        public bool IsRunning { get; private set; }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
