using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TensorFlowLite;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using GLTFast;
using UnityEngine.Networking; // Required for Web Download

public class BlackboardDetector : MonoBehaviour
{
    [Header("Model Settings")]
    public TextAsset modelFile;

    [Header("AR Settings")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public RectTransform detectionBox;

    [Header("Cloud Settings")]
    // This MUST be the "Direct Download" link format
    public string glbUrl = "https://drive.google.com/uc?export=download&id=1Mh8xJc6Mj94ZJ3wzE5WqfXgXjE9sZz_J"; 

    [Header("UI Settings")]
    public TextMeshProUGUI debugText;
    public Button scanButton;
    public Slider downloadBar; // DRAG YOUR SLIDER HERE

    private Interpreter interpreter;
    private float[] inputs;
    private float[] outputs;
    private int width = 640;
    private int height = 640;
    private int anchors = 8400;

    void Start()
    {
        var options = new InterpreterOptions();
        options.threads = 2;
        interpreter = new Interpreter(modelFile.bytes, options);
        interpreter.AllocateTensors();

        inputs = new float[width * height * 3];
        outputs = new float[1 * 6 * anchors];

        scanButton.onClick.AddListener(OnScanClicked);
        debugText.text = "System Ready. Scan Now.";
        detectionBox.gameObject.SetActive(false);
        if(downloadBar) downloadBar.gameObject.SetActive(false);
    }

    void OnScanClicked()
    {
        debugText.text = "Scanning...";
        StartCoroutine(CaptureAndRun());
    }

    IEnumerator CaptureAndRun()
    {
        yield return new WaitForEndOfFrame();

        Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTex.Apply();

        Color[] pixels = screenTex.GetPixels();
        float xRatio = (float)screenTex.width / width;
        float yRatio = (float)screenTex.height / height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int srcX = Mathf.FloorToInt(x * xRatio);
                int srcY = Mathf.FloorToInt(y * yRatio);
                srcX = Mathf.Clamp(srcX, 0, screenTex.width - 1);
                srcY = Mathf.Clamp(srcY, 0, screenTex.height - 1);
                
                Color c = pixels[srcY * screenTex.width + srcX];
                int index = ((height - 1 - y) * width + x) * 3; 
                inputs[index + 0] = c.r;
                inputs[index + 1] = c.g;
                inputs[index + 2] = c.b;
            }
        }
        Destroy(screenTex);

        interpreter.SetInputTensorData(0, inputs);
        interpreter.Invoke();
        interpreter.GetOutputTensorData(0, outputs);

        float maxScore = 0f;
        int bestIndex = -1;

        for (int i = 0; i < anchors; i++)
        {
            float score = outputs[4 * anchors + i];
            if (score > maxScore) { maxScore = score; bestIndex = i; }
        }

        if (maxScore > 0.6f) // Improved Threshold
        {
            float cx = outputs[0 * anchors + bestIndex];
            float cy = outputs[1 * anchors + bestIndex];
            Vector2 screenPos = new Vector2(cx * Screen.width, cy * Screen.height);
            
            detectionBox.gameObject.SetActive(true);
            detectionBox.anchoredPosition = screenPos; 

            debugText.text = "Found it! Fetching Cloud Data...";
            
            // Start the Web Request Routine
            StartCoroutine(DownloadAndSpawnRoutine(screenPos));
        }
        else
        {
            debugText.text = "No Blackboard found.";
        }
    }

    // --- NEW: Download Routine with Progress Bar ---
    IEnumerator DownloadAndSpawnRoutine(Vector2 screenPos)
    {
        // 1. Determine Position first
        Vector3 spawnPos;
        Quaternion spawnRot;
        CalculateSpawnPose(screenPos, out spawnPos, out spawnRot);

        // 2. Start Download
        if(downloadBar) {
            downloadBar.gameObject.SetActive(true);
            downloadBar.value = 0;
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(glbUrl))
        {
            // Send request and wait
            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                if(downloadBar) downloadBar.value = operation.progress;
                debugText.text = $"Downloading... {Mathf.RoundToInt(operation.progress * 100)}%";
                yield return null;
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                debugText.text = "Download Failed: " + webRequest.error;
            }
            else
            {
                debugText.text = "Processing 3D Model...";
                if(downloadBar) downloadBar.value = 1.0f;
                
                // 3. Load the downloaded data using GLTFast
                LoadBytesIntoScene(webRequest.downloadHandler.data, spawnPos, spawnRot);
            }
        }
    }

    async void LoadBytesIntoScene(byte[] data, Vector3 pos, Quaternion rot)
    {
        // Create Container
        GameObject cloudObject = new GameObject("CloudModel");
        cloudObject.transform.position = pos;
        cloudObject.transform.rotation = rot;
        cloudObject.transform.Rotate(0, 180, 0); 
        cloudObject.transform.localScale = Vector3.one * 0.05f; 
        cloudObject.AddComponent<ARAnchor>();

        // Import
        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(data); // Load from memory

        if (success)
        {
            await gltf.InstantiateMainSceneAsync(cloudObject.transform);
            HideVisuals();
            debugText.text = "Success! Anchored.";
            if(downloadBar) downloadBar.gameObject.SetActive(false);
        }
        else
        {
            debugText.text = "Error parsing 3D Model.";
        }
    }

    void CalculateSpawnPose(Vector2 screenPos, out Vector3 pos, out Quaternion rot)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose wallPose = hits[0].pose;
            Vector3 dir = (arCamera.transform.position - wallPose.position).normalized;
            pos = wallPose.position + (dir * 0.3f);
            rot = Quaternion.LookRotation(dir);
        }
        else
        {
            pos = arCamera.transform.position + (arCamera.transform.forward * 0.5f);
            rot = Quaternion.LookRotation(arCamera.transform.forward);
        }
    }

    void HideVisuals()
    {
        var planeMan = FindObjectOfType<ARPlaneManager>();
        if (planeMan) {
            foreach (var p in planeMan.trackables) p.gameObject.SetActive(false);
            planeMan.enabled = false; 
        }
        var pointMan = FindObjectOfType<ARPointCloudManager>();
        if (pointMan) {
            foreach (var p in pointMan.trackables) p.gameObject.SetActive(false);
            pointMan.enabled = false;
        }
    }

    void OnDestroy()
    {
        interpreter?.Dispose();
    }
}