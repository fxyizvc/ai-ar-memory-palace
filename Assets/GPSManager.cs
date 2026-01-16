using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Android; // Required for Permissions

public class GPSManager : MonoBehaviour
{
    [Header("UI Feedback")]
    public TextMeshProUGUI statusText;

    // We store these to share with other scripts later
    public float currentLatitude = 0f;
    public float currentLongitude = 0f;
    public bool isLocationReady = false;

    void Start()
    {
        // Automatically try to start GPS when app launches
        StartCoroutine(StartLocationService());
    }

    IEnumerator StartLocationService()
    {
        // 1. Check if user enabled Location on device
        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "Error: GPS is off. Turn it on in Settings.";
            yield break;
        }

        // 2. Request Permission (Crucial for Android 10+)
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1.0f); // Give UI time to pop up
        }

        // 3. Start Service
        // accuracy: 5 meters, updateDistance: 5 meters
        Input.location.Start(5f, 5f); 

        // 4. Wait for Initialization
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // 5. Check Failures
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

        // 6. Success! Loop to update coordinates
        isLocationReady = true;
        statusText.text = "GPS Active. Waiting for satellites...";
        
        // We keep updating the text so you can walk around and verify it works
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

                // Show on screen for testing
                statusText.text = $"Lat: {currentLatitude}\nLon: {currentLongitude}\nAcc: {accuracy}m";
            }
            yield return new WaitForSeconds(2.0f); // Update every 2 seconds
        }
    }
}