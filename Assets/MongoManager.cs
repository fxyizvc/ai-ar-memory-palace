using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

// --- Helper classes to parse the JSON Array from Vercel ---
[System.Serializable]
public class AssetData {
    public string filename;
    public string glb_url;
    public string pdf_url;
    public string branch;
    public string semester;
}

[System.Serializable]
public class AssetResponse {
    public bool found;
    public string mode;
    public string error;
    public AssetData[] assets;
}
// -----------------------------------------------------------

public class MongoManager : MonoBehaviour
{
    [Header("Cloud Settings")]
    public string backendUrl = "https://YOUR-PROJECT-NAME.vercel.app/api/find"; // KEEP YOUR VERCEL URL HERE

    [Header("UI References")]
    public TextMeshProUGUI statusText;     
    public TMP_Dropdown subjectDropdown;   
    public TMP_Dropdown branchDropdown;    
    public TMP_Dropdown semesterDropdown;  

    [Header("Carousel UI")]
    public GameObject pdfButton;           
    public GameObject nextButton;          // <-- NEW: Right Arrow Button
    public GameObject prevButton;          // <-- NEW: Left Arrow Button
    public TextMeshProUGUI counterText;    // <-- NEW: Text to show "1 / 3"

    // State Variables
    public bool isInsideCollege = false;
    private string currentCollege = "";
    
    // Carousel State
    private AssetData[] currentAssets;     
    private int currentIndex = 0;          

    void Start()
    {
        // Auto-find PDF Button if missing
        if (pdfButton == null)
        {
            pdfButton = GameObject.Find("PDF Button"); 
            if(pdfButton == null) pdfButton = GameObject.Find("PDFButton");
        }

        HideCarouselUI();

        if(statusText) statusText.text = "Initializing GPS...";
    }

    public void FindNearestCollege(float userLat, float userLon)
    {
        StartCoroutine(CheckLocationRoutine(userLat, userLon));
    }

    IEnumerator CheckLocationRoutine(float lat, float lon)
    {
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
                    if(statusText) statusText.text = $"Connected: {currentCollege}\nReady to Scan.";
                }
                else
                {
                    isInsideCollege = false;
                    currentCollege = "";
                    if(statusText) statusText.text = "Restricted: You are not inside a registered college.";
                }
            }
        }
    }

    // --- TRIGGER 1: MANUAL SCAN ---
    public void OnScanButtonClicked() { PerformSearch(); }

    // --- TRIGGER 2: AI SCAN ---
    public void TriggerSearchFromAI() { PerformSearch(); }

    // --- SHARED SEARCH LOGIC ---
    private void PerformSearch()
    {
        // 1. GPS Check
        if (!isInsideCollege)
        {
            if(statusText) statusText.text = "Access Denied: You must be at college to scan.";
            return;
        }

        // 2. Read Values from Dropdowns
        string selectedBranch = "All";
        string selectedSem = "All";
        string selectedSubject = "";

        if (branchDropdown != null) selectedBranch = branchDropdown.options[branchDropdown.value].text;
        if (semesterDropdown != null) selectedSem = semesterDropdown.options[semesterDropdown.value].text;
        
        // 3. Read the Smart Subject Dropdown
        if (subjectDropdown != null && subjectDropdown.options.Count > 0)
        {
            selectedSubject = subjectDropdown.options[subjectDropdown.value].text;
        }

        // 4. Validate
        if (selectedSubject == "No Subjects Found" || string.IsNullOrEmpty(selectedSubject))
        {
            if(statusText) statusText.text = "Error: Please select a valid subject.";
            return;
        }

        StartCoroutine(FetchAssetRoutine(selectedSubject, selectedBranch, selectedSem));
    }

    IEnumerator FetchAssetRoutine(string subject, string branch, string sem)
    {
        if(statusText) statusText.text = $"Searching {subject}...";
        HideCarouselUI(); 

        string url = $"{backendUrl}?subject={subject}&branch={branch}&semester={sem}&t={System.DateTime.Now.Ticks}";
        Debug.Log("SENT URL: " + url);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("RESPONSE: " + json);

                // Parse the JSON Array using Unity's JsonUtility
                AssetResponse response = JsonUtility.FromJson<AssetResponse>(json);

                if (response != null && response.found && response.assets != null && response.assets.Length > 0)
                {
                    currentAssets = response.assets;
                    currentIndex = 0; // Start at the first module
                    LoadCurrentAsset();
                }
                else
                {
                    if(statusText) statusText.text = $"No match found for {subject}.";
                }
            }
            else
            {
                if(statusText) statusText.text = "Network Error.";
            }
        }
    }

    // --- CAROUSEL LOGIC ---

    void LoadCurrentAsset()
    {
        if (currentAssets == null || currentAssets.Length == 0) return;

        AssetData data = currentAssets[currentIndex];
        string debugMsg = $"Found: {data.filename}";
        
        // 1. Update Counter Text (e.g., "1 / 3")
        if (counterText != null) 
            counterText.text = $"{currentIndex + 1} / {currentAssets.Length}";

        // 2. Download 3D Model
        if (!string.IsNullOrEmpty(data.glb_url))
        {
            string directGlbLink = FixGoogleDriveLink(data.glb_url);
            if (ModelDownloader.Instance != null)
                ModelDownloader.Instance.Download3DModel(directGlbLink);
        }

        // 3. Enable/Disable UI Elements
        if (!string.IsNullOrEmpty(data.pdf_url))
        {
            debugMsg += "\n(PDF Available)";
            if (pdfButton != null) pdfButton.SetActive(true);
        }
        else
        {
            if (pdfButton != null) pdfButton.SetActive(false);
        }

        // Show Next/Prev arrows ONLY if there is more than 1 file
        bool showArrows = currentAssets.Length > 1;
        if (nextButton != null) nextButton.SetActive(showArrows);
        if (prevButton != null) prevButton.SetActive(showArrows);
        if (counterText != null) counterText.gameObject.SetActive(showArrows);

        if(statusText) statusText.text = debugMsg;
    }

    public void OnNextClicked()
    {
        if (currentAssets == null || currentAssets.Length == 0) return;
        currentIndex++;
        if (currentIndex >= currentAssets.Length) currentIndex = 0; // Loop back to start
        LoadCurrentAsset();
    }

    public void OnPrevClicked()
    {
        if (currentAssets == null || currentAssets.Length == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = currentAssets.Length - 1; // Loop back to end
        LoadCurrentAsset();
    }

    public void OnPdfButtonClicked()
    {
        if (currentAssets != null && currentAssets.Length > 0)
        {
            string currentPdf = currentAssets[currentIndex].pdf_url;
            if (!string.IsNullOrEmpty(currentPdf))
            {
                if(statusText) statusText.text = "Opening Browser...";
                OpenInChrome(FixGoogleDriveLink(currentPdf));
            }
        }
    }

    void HideCarouselUI()
    {
        if(pdfButton != null) pdfButton.SetActive(false);
        if(nextButton != null) nextButton.SetActive(false);
        if(prevButton != null) prevButton.SetActive(false);
        if(counterText != null) counterText.gameObject.SetActive(false);
    }

    public void OpenInChrome(string url) { Application.OpenURL(url); }

    string FixGoogleDriveLink(string url)
    {
        if (url.Contains("drive.google.com") && url.Contains("/file/d/"))
        {
            try 
            {
                int start = url.IndexOf("/d/") + 3;
                int end = url.IndexOf("/view", start);
                if (end == -1) end = url.IndexOf("/", start);
                string id = url.Substring(start, end - start);
                return "https://drive.google.com/uc?export=download&id=" + id;
            }
            catch { return url; }
        }
        return url;
    }

    // Still used for extracting the college name from the GPS check JSON
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