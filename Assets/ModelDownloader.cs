using UnityEngine;
using System.Collections;
using GLTFast; 

public class ModelDownloader : MonoBehaviour
{
    public static ModelDownloader Instance;

    [Header("Settings")]
    public Transform spawnPoint;  
    
    // CHANGED: Default scale to 0.05 (much smaller)
    public float modelScale = 0.05f; 

    private GameObject currentModel;

    void Awake()
    {
        Instance = this;
    }

    public void Download3DModel(string url)
    {
        if (currentModel != null) Destroy(currentModel);

        Debug.Log("ModelDownloader: Starting download from " + url);
        StartCoroutine(LoadModelRoutine(url));
    }

    IEnumerator LoadModelRoutine(string url)
    {
        currentModel = new GameObject("AR_Model");
        
        // 1. POSITION
        if (spawnPoint != null)
        {
            currentModel.transform.position = spawnPoint.position;
            currentModel.transform.rotation = spawnPoint.rotation;
        }
        else
        {
            currentModel.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.0f;
            currentModel.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }

        // 2. FIX ROTATION (Flip 180 degrees to show front)
        currentModel.transform.Rotate(0, 180, 0);

        // 3. FIX SCALE (Apply the smaller size)
        currentModel.transform.localScale = Vector3.one * modelScale;

        // 4. LOAD CONTENT
        var gltf = currentModel.AddComponent<GltfAsset>();
        gltf.Url = url;

        yield return null; 
    }

    public void HideModel()
    {
        if (currentModel != null) Destroy(currentModel);
    }
}