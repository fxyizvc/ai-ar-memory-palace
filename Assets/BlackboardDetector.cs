using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TensorFlowLite;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using GLTFast;
using UnityEngine.Networking;

public class BlackboardDetector : MonoBehaviour
{
    [Header("Model Settings")]
    public TextAsset modelFile;

    [Header("AR Settings")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public RectTransform detectionBox;

    [Header("UI Interaction")]
    public GameObject selectionPanel;      
    public TMP_Dropdown semDropdown;       
    public TMP_Dropdown branchDropdown;    
    public TMP_InputField subjectInput;    
    
    [Header("Feedback UI")]
    public TextMeshProUGUI debugText;
    public Button scanButton;
    public Slider downloadBar;

    // REPLACE THIS WITH YOUR NEW SCRIPT URL FROM STEP 2 👇
    private string googleScriptUrl = "https://script.google.com/macros/s/AKfycbwZ6d_8n2GUqLILrWKdaxvAoKZwp6xqjJxochxf4BZ94t99E4MCpWbDItv40yo3gjXh/exec";

    // AI Variables
    private Interpreter interpreter;
    private float[] inputs;
    private float[] outputs;
    private int width = 640;
    private int height = 640;
    private int anchors = 8400;
    private string targetSubject = "";

    void Start()
    {
        try {
            var options = new InterpreterOptions();
            options.threads = 2;
            interpreter = new Interpreter(modelFile.bytes, options);
            interpreter.AllocateTensors();
            inputs = new float[width * height * 3];
            outputs = new float[1 * 6 * anchors];
        } catch (System.Exception e) {
            debugText.text = "AI Init Error: " + e.Message;
        }

        scanButton.onClick.AddListener(OnScanClicked);
        debugText.text = "Enter Subject Code and Scan.";
        detectionBox.gameObject.SetActive(false);
        if(downloadBar) downloadBar.gameObject.SetActive(false);
        if(selectionPanel) selectionPanel.SetActive(true);
    }

    void OnScanClicked()
    {
        string subject = subjectInput.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(subject)) {
            debugText.text = "Error: Please enter a Subject Code.";
            return;
        }

        targetSubject = subject;
        selectionPanel.SetActive(false); 
        debugText.text = "Searching for Blackboard...";
        StartCoroutine(CaptureAndRun());
    }

    // --- 1. THE AI LOOP ---
    IEnumerator CaptureAndRun()
    {
        yield return new WaitForEndOfFrame();

        Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTex.Apply();

        // Image Processing...
        Color[] pixels = screenTex.GetPixels();
        float xRatio = (float)screenTex.width / width;
        float yRatio = (float)screenTex.height / height;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
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
        for (int i = 0; i < anchors; i++) {
            float score = outputs[4 * anchors + i];
            if (score > maxScore) { maxScore = score; bestIndex = i; }
        }

        if (maxScore > 0.6f) {
            float cx = outputs[0 * anchors + bestIndex];
            float cy = outputs[1 * anchors + bestIndex];
            Vector2 screenPos = new Vector2(cx * Screen.width, cy * Screen.height);
            
            detectionBox.gameObject.SetActive(true);
            detectionBox.anchoredPosition = screenPos; 

            debugText.text = "Board Found. Fetching Cloud Data...";
            
            // START THE NEW FETCH ROUTINE
            StartCoroutine(FetchAndDownloadRoutine(screenPos));
        } else {
            debugText.text = "No Blackboard found. Try moving closer.";
        }
    }

    // --- 2. THE NEW DYNAMIC FETCHER ---
    IEnumerator FetchAndDownloadRoutine(Vector2 screenPos)
    {
        // A. Ask Google Script for the link
        string fetchUrl = googleScriptUrl + "?subject=" + targetSubject;
        string downloadLink = "";
        
        using (UnityWebRequest www = UnityWebRequest.Get(fetchUrl))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success) {
                debugText.text = "API Error: " + www.error;
                yield break;
            }
            
            downloadLink = www.downloadHandler.text;
            
            // Check if script returned an error message
            if (downloadLink.StartsWith("Error")) {
                debugText.text = downloadLink; // e.g., "Error: Folder not found"
                yield break;
            }
        }

        // B. Download the actual GLB file
        Debug.Log("Downloading from: " + downloadLink);
        debugText.text = "Downloading Model...";
        
        Vector3 spawnPos; Quaternion spawnRot;
        CalculateSpawnPose(screenPos, out spawnPos, out spawnRot);

        if(downloadBar) { downloadBar.gameObject.SetActive(true); downloadBar.value = 0; }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(downloadLink))
        {
            var operation = webRequest.SendWebRequest();
            while (!operation.isDone) {
                if(downloadBar) downloadBar.value = operation.progress;
                yield return null;
            }

            if (webRequest.result != UnityWebRequest.Result.Success) {
                debugText.text = "Download Failed (404/500)";
            } else {
                debugText.text = "Processing Model...";
                if(downloadBar) downloadBar.value = 1.0f;
                LoadBytesIntoScene(webRequest.downloadHandler.data, spawnPos, spawnRot);
            }
        }
    }

    async void LoadBytesIntoScene(byte[] data, Vector3 pos, Quaternion rot)
    {
        GameObject cloudObject = new GameObject("CloudModel");
        cloudObject.transform.position = pos;
        cloudObject.transform.rotation = rot;
        cloudObject.transform.Rotate(0, 180, 0); 
        cloudObject.transform.localScale = Vector3.one * 0.05f; 
        cloudObject.AddComponent<ARAnchor>();

        var gltf = new GltfImport();
        bool success = await gltf.LoadGltfBinary(data); 

        if (success) {
            await gltf.InstantiateMainSceneAsync(cloudObject.transform);
            HideVisuals();
            debugText.text = "Success! " + targetSubject + " Loaded.";
            if(downloadBar) downloadBar.gameObject.SetActive(false);
        } else {
            debugText.text = "Model Parsing Failed.";
        }
    }

    // (Helper functions CalculateSpawnPose, HideVisuals, OnDestroy remain the same)
    void CalculateSpawnPose(Vector2 screenPos, out Vector3 pos, out Quaternion rot)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon)) {
            Pose wallPose = hits[0].pose;
            Vector3 dir = (arCamera.transform.position - wallPose.position).normalized;
            pos = wallPose.position + (dir * 0.3f);
            rot = Quaternion.LookRotation(dir);
        } else {
            pos = arCamera.transform.position + (arCamera.transform.forward * 0.5f);
            rot = Quaternion.LookRotation(arCamera.transform.forward);
        }
    }

    void HideVisuals()
    {
        var planeMan = FindObjectOfType<ARPlaneManager>();
        if (planeMan) { planeMan.enabled = false; foreach (var p in planeMan.trackables) p.gameObject.SetActive(false); }
        var pointMan = FindObjectOfType<ARPointCloudManager>();
        if (pointMan) { pointMan.enabled = false; foreach (var p in pointMan.trackables) p.gameObject.SetActive(false); }
    }

    void OnDestroy() {
        interpreter?.Dispose();
    }
}