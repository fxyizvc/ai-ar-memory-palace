using UnityEngine;
using Unity.Sentis; // The AI Engine
using UnityEngine.UI;
using UnityEngine.Android; // For Permissions
using System.Collections; // For GPS Coroutine

public class MobileDetector : MonoBehaviour
{
    [Header("UI & Model Setup")]
    public ModelAsset modelAsset;      // Drag 'best.onnx' here
    public Text statusText;            // Drag your Text (Legacy) here
    public RawImage cameraView;        // Drag your RawImage here

    // --- INTERNAL VARIABLES ---
    private Model runtimeModel;
    private Worker worker;
    private WebCamTexture webcam;
    private Tensor<float> inputTensor;
    
    // GPS Variables
    private string gpsLocation = "GPS Initializing...";
    private bool isGpsReady = false;

    // Constants
    const int ModelSize = 640; 
    const int NumAnchors = 8400; // Standard for YOLOv8

    void Start()
    {
        // 1. Start Camera
        webcam = new WebCamTexture(640, 480);
        cameraView.texture = webcam;
        webcam.Play();

        // 2. Load AI Model
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
        inputTensor = new Tensor<float>(new TensorShape(1, 3, ModelSize, ModelSize));

        // 3. Start GPS Service (Runs in background)
        StartCoroutine(StartLocationService());
    }

    // --- GPS SETUP ROUTINE ---
    IEnumerator StartLocationService()
    {
        // A. Ask for Permission
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1.5f); // Give user time to click 'Allow'
        }

        // B. Check if User Enabled Location
        if (!Input.location.isEnabledByUser)
        {
            gpsLocation = "GPS Disabled in Settings";
            isGpsReady = false;
            yield break;
        }

        // C. Start Service
        Input.location.Start(10f, 10f); // Accuracy: 10 meters

        // D. Wait for Initialization (Max 20 seconds)
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // E. Check Result
        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            gpsLocation = "GPS Connection Failed";
            isGpsReady = false;
        }
        else
        {
            isGpsReady = true;
        }
    }

    void Update()
    {
        if (!webcam.didUpdateThisFrame) return;

        // --- 1. HANDLE ROTATION ---
        cameraView.rectTransform.localEulerAngles = new Vector3(0, 0, -webcam.videoRotationAngle);

        // --- 2. UPDATE GPS STATUS ---
        // We update this every frame so you can see coordinates change as you walk
        if (isGpsReady && Input.location.status == LocationServiceStatus.Running)
        {
            float lat = Input.location.lastData.latitude;
            float lon = Input.location.lastData.longitude;
            float alt = Input.location.lastData.altitude;
            gpsLocation = $"Lat: {lat:F5}\nLon: {lon:F5}\nAlt: {alt:F1}m";
        }
        else if (!isGpsReady)
        {
            // Keep showing the error/status if not ready
            gpsLocation = $"Status: {Input.location.status}";
        }

        // --- 3. RUN AI ---
        TextureConverter.ToTensor(webcam, inputTensor, new TextureTransform());
        worker.Schedule(inputTensor);

        // --- 4. READ RESULTS ---
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        using var readable = output.ReadbackAndClone();
        float[] data = readable.DownloadToArray();

        // Check 5th block (Confidences)
        int totalValues = data.Length;
        int offset = 4 * NumAnchors; 
        
        bool detected = false;
        float maxScore = 0f;

        if (offset < totalValues)
        {
            for (int i = offset; i < totalValues; i++) 
            {
                if (data[i] > 0.70f) // 70% Confidence
                {
                    detected = true;
                    maxScore = data[i];
                    break; 
                }
            }
        }

        // --- 5. UPDATE UI (Now shows GPS in BOTH states) ---
        if (detected)
        {
            statusText.text = $"✅ FOUND ({maxScore:P0})\n{gpsLocation}";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = $"❌ SEARCHING...\n{gpsLocation}";
            statusText.color = Color.red;
        }
    }

    void OnDisable()
    {
        worker?.Dispose();
        inputTensor?.Dispose();
        webcam?.Stop();
        Input.location.Stop(); // Stop GPS to save battery
    }
}
