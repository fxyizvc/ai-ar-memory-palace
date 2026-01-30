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
    public TMP_Dropdown subjectDropdown;   // <--- NEW: Connected to SyllabusManager
    public TMP_Dropdown branchDropdown;    
    public TMP_Dropdown semesterDropdown;  
    public GameObject pdfButton;           

    // State Variables
    public bool isInsideCollege = false;
    private string currentCollege = "";
    private string currentPdfUrl = "";     

    void Start()
    {
        // Auto-find PDF Button if missing
        if (pdfButton == null)
        {
            pdfButton = GameObject.Find("PDF Button"); 
            if(pdfButton == null) pdfButton = GameObject.Find("PDFButton");
        }

        if(pdfButton != null) pdfButton.SetActive(false); 

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
    public void OnScanButtonClicked()
    {
        PerformSearch();
    }

    // --- TRIGGER 2: AI SCAN ---
    public void TriggerSearchFromAI()
    {
        PerformSearch();
    }

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
        if(pdfButton != null) pdfButton.SetActive(false); 

        string url = $"{backendUrl}?subject={subject}&branch={branch}&semester={sem}&t={System.DateTime.Now.Ticks}";
        Debug.Log("SENT URL: " + url);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("RESPONSE: " + json);

                if (json.Contains("\"found\":true"))
                {
                    string filename = ExtractValue(json, "filename");
                    currentPdfUrl = ExtractValue(json, "pdf_url"); 
                    string rawGlbUrl = ExtractValue(json, "glb_url");
                    
                    string debugMsg = $"Found: {filename}";

                    // 1. Download 3D Model (with Link Fix)
                    if (!string.IsNullOrEmpty(rawGlbUrl))
                    {
                        string directGlbLink = FixGoogleDriveLink(rawGlbUrl);
                        if (ModelDownloader.Instance != null)
                            ModelDownloader.Instance.Download3DModel(directGlbLink);
                    }

                    // 2. Show PDF Button
                    if (!string.IsNullOrEmpty(currentPdfUrl))
                    {
                        debugMsg += "\n(PDF Available)";
                        if (pdfButton != null) pdfButton.SetActive(true);
                    }
                    
                    if(statusText) statusText.text = debugMsg;
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

    public void OnPdfButtonClicked()
    {
        if (!string.IsNullOrEmpty(currentPdfUrl))
        {
            if(statusText) statusText.text = "Opening Browser...";
            OpenInChrome(FixGoogleDriveLink(currentPdfUrl));
        }
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