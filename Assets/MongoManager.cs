using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class MongoManager : MonoBehaviour
{
    [Header("Cloud Settings")]
    public string backendUrl = "https://YOUR-PROJECT-NAME.vercel.app/api/find"; 

    [Header("UI References")]
    public TextMeshProUGUI statusText;     
    public TMP_InputField subjectInput;    
    public GameObject pdfButton;           

    // CHANGE 1: No more 'static'. These reset every time the app opens.
    private bool isInsideCollege = false;
    private string currentCollege = "";
    private string currentPdfUrl = "";     

    void Start()
    {
        // CHANGE 2: Bulletproof Button Finder
        // If the button link is broken, we find it manually by name
        if (pdfButton == null)
        {
            pdfButton = GameObject.Find("PDF Button");
        }

        // Force hide at start
        if(pdfButton != null) 
        {
            pdfButton.SetActive(false); 
        }
        else 
        {
            if(statusText) statusText.text = "Warning: PDF Button not found!";
        }

        if(statusText) statusText.text = "Initializing GPS...";
    }

    public void FindNearestCollege(float userLat, float userLon)
    {
        StartCoroutine(CheckLocationRoutine(userLat, userLon));
    }

    IEnumerator CheckLocationRoutine(float lat, float lon)
    {
        // Cache Buster is still good to keep
        string url = $"{backendUrl}?lat={lat}&lon={lon}&t={System.DateTime.Now.Ticks}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                
                if (json.Contains("\"found\":true"))
                {
                    isInsideCollege = true;
                    currentCollege = ExtractValue(json, "college_name");
                    // Only update text if it's a new connection or different
                    if(statusText) statusText.text = $"Connected: {currentCollege}\nReady to Scan.";
                }
                else
                {
                    isInsideCollege = false;
                    // Optional: You can comment this out if it spams too much
                    // string dist = ExtractValue(json, "distance").Replace("}", "").Trim();
                    // if(statusText) statusText.text = $"Not in college.\nDist: {dist}m";
                }
            }
        }
    }

    public void OnScanButtonClicked()
    {
        // Remove the blocking check for debugging if you are testing at home
        // if (!isInsideCollege) { ... }

        string subjectCode = subjectInput.text;
        if (string.IsNullOrEmpty(subjectCode))
        {
            if(statusText) statusText.text = "Please enter a subject code.";
            return;
        }

        StartCoroutine(FetchAssetRoutine(subjectCode));
    }

    IEnumerator FetchAssetRoutine(string subject)
    {
        if(statusText) statusText.text = "Searching Database...";
        if(pdfButton) pdfButton.SetActive(false); 

        string url = $"{backendUrl}?subject={subject}&t={System.DateTime.Now.Ticks}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("JSON: " + json); // Debugging

                if (json.Contains("\"found\":true"))
                {
                    string filename = ExtractValue(json, "filename");
                    currentPdfUrl = ExtractValue(json, "pdf_url"); 
                    string glbUrl = ExtractValue(json, "glb_url");

                    if(statusText) statusText.text = $"Found: {filename}";

                    // CHANGE 3: Explicit Debugging on Screen
                    if (!string.IsNullOrEmpty(currentPdfUrl))
                    {
                        if (pdfButton != null)
                        {
                            pdfButton.SetActive(true);
                            // Verify it actually turned on
                            if(statusText) statusText.text += "\n(Notes Available)";
                        }
                        else
                        {
                            if(statusText) statusText.text += "\nError: Button Missing!";
                        }
                    }
                }
                else
                {
                    if(statusText) statusText.text = "Subject not found.";
                }
            }
            else
            {
                if(statusText) statusText.text = "Network Error: " + request.error;
            }
        }
    }

    public void OnPdfButtonClicked()
    {
        if (!string.IsNullOrEmpty(currentPdfUrl))
        {
            if(statusText) statusText.text = "Opening Browser...";
            OpenInChrome(currentPdfUrl);
        }
    }

    public void OpenInChrome(string url)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW", 
                       new AndroidJavaObject("android.net.Uri").CallStatic<AndroidJavaObject>("parse", url)))
                {
                    intent.Call<AndroidJavaObject>("setPackage", "com.android.chrome");
                    intent.Call<AndroidJavaObject>("addFlags", 0x10000000); 
                    currentActivity.Call("startActivity", intent);
                }
            }
            catch (System.Exception e)
            {
                Application.OpenURL(url);
            }
        #else
            Application.OpenURL(url);
        #endif
    }

    string ExtractValue(string json, string key)
    {
        string search = "\"" + key + "\":\"";
        int start = json.IndexOf(search);
        if (start == -1) return "";
        start += search.Length;
        int end = json.IndexOf("\"", start);
        return json.Substring(start, end - start);
    }
}