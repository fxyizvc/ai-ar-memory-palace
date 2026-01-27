using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Android; 

public class GPSManager : MonoBehaviour
{
    [Header("Dependencies")]
    public MongoManager mongoManager; // <--- 1. ADD THIS VARIABLE

    [Header("UI Feedback")]
    public TextMeshProUGUI statusText;

    public float currentLatitude = 0f;
    public float currentLongitude = 0f;
    public bool isLocationReady = false;

    // We add a flag so we don't spam the database every 2 seconds
    private bool hasFetchedData = false; 

    void Start()
    {
        StartCoroutine(StartLocationService());
    }

    IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "Error: GPS is off. Turn it on in Settings.";
            yield break;
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1.0f); 
        }

        Input.location.Start(5f, 5f); 

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            statusText.text = "Error: GPS timed out.";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            statusText.text = "Error: GPS failed to start.";
            yield break;
        }

        isLocationReady = true;
        statusText.text = "GPS Active. Waiting for satellites...";
        
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

                // Only update text if we haven't found a college yet
                if (!hasFetchedData) 
                {
                    statusText.text = $"Lat: {currentLatitude}\nLon: {currentLongitude}\nAcc: {accuracy}m";
                    
                    // <--- 2. CALL MONGO DB HERE
                    if (mongoManager != null && accuracy < 50f && !hasFetchedData) // Wait for good accuracy (<50m)
                    {
                        mongoManager.FindNearestCollege(currentLatitude, currentLongitude);
                        hasFetchedData = true; // Stop calling it repeatedly
                    }
                }
            }
            yield return new WaitForSeconds(3.0f); 
        }
    }
}