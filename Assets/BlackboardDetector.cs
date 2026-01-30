using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TensorFlowLite;
using TMPro;
using System.Collections;

public class BlackboardDetector : MonoBehaviour
{
    [Header("Dependencies")]
    public MongoManager mongoManager; 

    [Header("Model Settings")]
    public TextAsset modelFile;

    [Header("AR Settings")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public RectTransform detectionBox;

    [Header("UI Interaction")]
    public GameObject selectionPanel;      
    // Removed old inputs -> MongoManager handles them now!
    
    [Header("Feedback UI")]
    public TextMeshProUGUI debugText;
    public Button scanButton;

    // AI Variables
    private Interpreter interpreter;
    private float[] inputs;
    private float[] outputs;
    private int width = 640;
    private int height = 640;
    private int anchors = 8400;

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
            if(debugText) debugText.text = "AI Init Error: " + e.Message;
        }

        scanButton.onClick.AddListener(OnScanClicked);
        if(debugText) debugText.text = "Select Subject & Scan";
        if(detectionBox) detectionBox.gameObject.SetActive(false);
        if(selectionPanel) selectionPanel.SetActive(true);
    }

    void OnScanClicked()
    {
        // Hide UI and start looking
        if(selectionPanel) selectionPanel.SetActive(false); 
        if(debugText) debugText.text = "Scanning for Blackboard...";
        
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
            
            if(detectionBox) {
                detectionBox.gameObject.SetActive(true);
                detectionBox.anchoredPosition = screenPos; 
            }

            if(debugText) debugText.text = "Board Found! Checking Database...";
            
            // === NEW LOGIC: Just Trigger, no arguments ===
            if (mongoManager != null) {
                mongoManager.TriggerSearchFromAI(); 
                HideVisuals(); 
            }
        } else {
            if(debugText) debugText.text = "No Board Found. Move Closer.";
            if(selectionPanel) selectionPanel.SetActive(true);
        }
    }

    // Helper to hide AR dots/planes once we find the board
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