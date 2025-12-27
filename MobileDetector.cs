using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using GLTFast; // Make sure this package is installed!

public class MobileDetector : MonoBehaviour
{
    [Header("UI & Scene Setup")]
    public Unity.InferenceEngine.ModelAsset modelAsset;      // Drag your .onnx file here
    public RawImage cameraView;        // Drag your CameraView (RawImage) here
    public Text statusText;            // Drag your Status Text here
    public Transform modelHolder;      // Drag your 'ModelHolder' object here

    [Header("Settings")]
    public bool flipX = false; // Try checking this if Left/Right is wrong
    public bool flipY = false; // Try checking this if Up/Down is wrong
    public bool swapXY = false; // Try this if moving Up makes the card move Right
    [Range(0.0f, 1.0f)] public float confidenceThreshold = 0.90f; // 90% Confidence
    public int skipFrames = 15;        // Run AI every 15 frames to stop lag
    public float modelScale = 300f;    // SCALE MULTIPLIER (Increase if model is invisible)

    // --- INTERNAL VARIABLES ---
    private Unity.InferenceEngine.Model runtimeModel;
    private Unity.InferenceEngine.Worker worker;
    private WebCamTexture webcam;
    private Unity.InferenceEngine.Tensor<float> inputTensor;
    private GameObject loadedGlbObject;
    private int frameCount = 0;
    
    // YOLO Constants
    const int ModelSize = 640; 
    const int NumAnchors = 8400;

    async void Start()
    {
        // 1. Start Camera
        webcam = new WebCamTexture(640, 480);
        cameraView.texture = webcam;
        webcam.Play();

        // 2. Load AI Brain
        runtimeModel = Unity.InferenceEngine.ModelLoader.Load(modelAsset);
        worker = new Unity.InferenceEngine.Worker(runtimeModel, Unity.InferenceEngine.BackendType.GPUCompute);
        inputTensor = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, 3, ModelSize, ModelSize));

        // 3. Download 3D Model from Drive
        // PASTE YOUR DIRECT GOOGLE DRIVE LINK HERE
        string glbUrl = "https://drive.google.com/uc?export=download&id=1P8UmIOCM5u-0D_j4MCqli3W9BmNHYiX0";
        
        statusText.text = "Downloading 3D Model...";
        statusText.color = Color.cyan;

        var gltf = new GltfImport();
        // Download and parse the GLB
        bool success = await gltf.Load(glbUrl);

        if (success)
        {
            // Create a wrapper object
            loadedGlbObject = new GameObject("FlashcardGLB");
            loadedGlbObject.transform.SetParent(modelHolder, false); 
            
            // Spawn the actual model inside the wrapper
            await gltf.InstantiateMainSceneAsync(loadedGlbObject.transform);
            
            // --- CRITICAL FIXES ---
            // 1. Scale it up massively (UI Pixels vs Meters)
            loadedGlbObject.transform.localScale = Vector3.one * modelScale;
            
            // 2. Fix Rotation (Spin it around if needed)
            loadedGlbObject.transform.localEulerAngles = new Vector3(0, 180, 0);

            // 3. Hide it until detected
            loadedGlbObject.SetActive(false);
            
            statusText.text = "Model Loaded! Searching...";
            statusText.color = Color.white;
        }
        else
        {
            statusText.text = "Download Failed! Check Link.";
            statusText.color = Color.magenta;
        }
    }

    void Update()
    {
        if (!webcam.didUpdateThisFrame) return;

        // Visuals: Rotate camera view
        cameraView.rectTransform.localEulerAngles = new Vector3(0, 0, -webcam.videoRotationAngle);

        

        // --- LAG FIX ---
        // Only run AI logic once every 'skipFrames' (15)
        frameCount++;
        if (frameCount % skipFrames != 0) return;

        DetectBlackboard();
    }

    void DetectBlackboard()
    {
        // 1. Prepare Image
        Unity.InferenceEngine.TextureConverter.ToTensor(webcam, inputTensor, new Unity.InferenceEngine.TextureTransform());

        // 2. Run AI
        worker.Schedule(inputTensor);

        // 3. Read Output
        Unity.InferenceEngine.Tensor<float> output = worker.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
        using var readable = output.ReadbackAndClone();
        float[] data = readable.DownloadToArray();

        // 4. Find Best Detection
        // YOLO Data: [Row 0=X, Row 1=Y, ... Row 4=Confidence]
        // Skip first 4 rows to find confidence scores
        int offsetConf = 4 * NumAnchors; 
        
        float maxScore = 0f;
        int bestIndex = -1;

        if (offsetConf < data.Length)
        {
            for (int i = 0; i < NumAnchors; i++) 
            {
                float conf = data[offsetConf + i];
                if (conf > confidenceThreshold && conf > maxScore) 
                {
                    maxScore = conf;
                    bestIndex = i;
                }
            }
        }

        // 5. Show/Hide Model
        if (maxScore > confidenceThreshold && bestIndex != -1 && loadedGlbObject != null)
        {
            statusText.text = $"✅ ACTIVE ({maxScore:P0})";
            statusText.color = Color.green;
            
            loadedGlbObject.SetActive(true);

            // Get Coordinates (Row 0 and Row 1)
            float rawX = data[bestIndex]; 
            float rawY = data[NumAnchors + bestIndex];
            
            // Normalize to 0.0 - 1.0 range
            float normX = rawX / ModelSize;
            float normY = rawY / ModelSize;

            PositionOverlay(normX, normY);
        }
        else
        {
            statusText.text = "Searching...";
            statusText.color = Color.yellow;
            if(loadedGlbObject != null) loadedGlbObject.SetActive(false);
        }
    }

    void PositionOverlay(float x, float y)
{
    // 1. Handle Rotation (Swap X and Y)
    if (swapXY) 
    {
        float temp = x;
        x = y;
        y = temp;
    }

    // 2. Handle Mirroring (Flip)
    if (flipX) x = 1.0f - x;
    if (flipY) y = 1.0f - y;

    // 3. Map to Screen
    RectTransform camRect = cameraView.rectTransform;
    float width = camRect.rect.width;
    float height = camRect.rect.height;

    float screenX = (x - 0.5f) * width;
    float screenY = -(y - 0.5f) * height; // Unity UI Y is usually inverted relative to YOLO

    modelHolder.localPosition = new Vector3(0, 0, -40f);
    // 4. Apply
    // Keep Z at 50 to stay visible
    // modelHolder.localPosition = new Vector3(screenX, screenY, 50f);
}

    void OnDisable()
    {
        worker?.Dispose();
        inputTensor?.Dispose();
        webcam?.Stop();
    }
}
