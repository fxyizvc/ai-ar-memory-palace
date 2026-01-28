using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Android; 

public class GPSManager : MonoBehaviour
{
    [Header("Dependencies")]
    public MongoManager mongoManager;

    [Header("UI Feedback")]
    public TextMeshProUGUI statusText;

    public float currentLatitude = 0f;
    public float currentLongitude = 0f;

    void Start()
    {
        StartCoroutine(StartLocationService());
    }

    IEnumerator StartLocationService()
    {
        // 1. Check if User has GPS enabled on phone
        if (!Input.location.isEnabledByUser)
        {
            if(statusText) statusText.text = "Error: GPS is off. Turn it on.";
            yield break;
        }

        // 2. Request Permission (Android)
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1.0f); 
        }

        // 3. Start Service
        Input.location.Start(5f, 5f); 

        // 4. Wait for Initialization
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            if(statusText) statusText.text = "Error: GPS timed out.";
            yield break;
        }

        if(statusText) statusText.text = "GPS Active. Verifying location...";
        
        // 5. Start the Continuous Loop
        StartCoroutine(UpdateCoordinates());
    }

    IEnumerator UpdateCoordinates()
    {
        while (true)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                currentLatitude = Input.location.lastData.latitude;
                currentLongitude = Input.location.lastData.longitude;
                float accuracy = Input.location.lastData.horizontalAccuracy;

                // Send coordinates to MongoManager to verify College access
                // We do this repeatedly to allow users to walk into range
                if (mongoManager != null)
                {
                    mongoManager.FindNearestCollege(currentLatitude, currentLongitude);
                }
            }
            
            // Wait 10 seconds before next check to save battery/data
            yield return new WaitForSeconds(10.0f); 
        }
    }
}