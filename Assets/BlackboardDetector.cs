using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TensorFlowLite;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using GLTFast;

public class BlackboardDetector : MonoBehaviour
{
    [Header("Model Settings")]
    public TextAsset modelFile;

    [Header("AR Settings")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public RectTransform detectionBox;

    [Header("Cloud Settings")]
    public string glbUrl = "https://drive.google.com/uc?export=download&id=1Mh8xJc6Mj94ZJ3wzE5WqfXgXjE9sZz_J"; 

    [Header("UI Settings")]
    public TextMeshProUGUI debugText;
    public Button scanButton;

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

        if (maxScore > 0.4f)
        {
            float cx = outputs[0 * anchors + bestIndex];
            float cy = outputs[1 * anchors + bestIndex];
            Vector2 screenPos = new Vector2(cx * Screen.width, cy * Screen.height);
            
            detectionBox.gameObject.SetActive(true);
            detectionBox.anchoredPosition = screenPos; 

            debugText.text = "Found it! Downloading...";
            DownloadAndSpawn(screenPos);
        }
        else
        {
            debugText.text = "No Blackboard found.";
        }
    }

    async void DownloadAndSpawn(Vector2 screenPos)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose wallPose = hits[0].pose;
            Vector3 directionToCamera = (arCamera.transform.position - wallPose.position).normalized;
            
            // 30cm pop-out effect
            spawnPos = wallPose.position + (directionToCamera * 0.3f);
            spawnRot = Quaternion.LookRotation(directionToCamera);
        }
        else
        {
            spawnPos = arCamera.transform.position + (arCamera.transform.forward * 0.5f);
            spawnRot = Quaternion.LookRotation(arCamera.transform.forward);
        }

        GameObject cloudObject = new GameObject("CloudModel");
        cloudObject.transform.position = spawnPos;
        cloudObject.transform.rotation = spawnRot;
        cloudObject.transform.Rotate(0, 180, 0); 
        cloudObject.transform.localScale = Vector3.one * 0.05f; 

        cloudObject.AddComponent<ARAnchor>();
        var gltf = cloudObject.AddComponent<GltfAsset>();
        gltf.Url = glbUrl;

        // --- NEW: Cleanup the View ---
        HideVisuals();

        debugText.text = "Success! Anchored.";
    }

    void HideVisuals()
    {
        // 1. Find the Plane Manager and turn off all planes
        var planeMan = FindObjectOfType<ARPlaneManager>();
        if (planeMan != null)
        {
            foreach (var plane in planeMan.trackables)
                plane.gameObject.SetActive(false);
            
            // Stop finding new planes (saves battery and keeps view clean)
            planeMan.enabled = false; 
        }

        // 2. Find the Point Cloud Manager (The White Dots) and turn them off
        var pointMan = FindObjectOfType<ARPointCloudManager>();
        if (pointMan != null)
        {
            foreach (var point in pointMan.trackables)
                point.gameObject.SetActive(false);
            
            pointMan.enabled = false;
        }
    }

    void OnDestroy()
    {
        interpreter?.Dispose();
    }
}