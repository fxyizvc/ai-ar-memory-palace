using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class MongoManager : MonoBehaviour
{
    [Header("Cloud Settings")]
    public string backendUrl = "https://YOUR-PROJECT-NAME.vercel.app/api/find"; // PASTE VERCEL URL HERE

    [Header("UI References")]
    public TextMeshProUGUI statusText;     
    public TMP_InputField subjectInput;
    public TMP_Dropdown branchDropdown;    
    public TMP_Dropdown semesterDropdown;  
    public GameObject pdfButton;           

    // State Variables
    public bool isInsideCollege = false; // Made public so we can see it in Inspector
    private string currentCollege = "";
    private string currentPdfUrl = "";     

    void Start()
    {
        // 1. AUTO-FIND BUTTON
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
                
                // === GPS ENFORCEMENT LOGIC ===
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
                    if(statusText) statusText.text = "Location Restriction:\nYou are not inside a registered college.";
                }
            }
        }
    }

    // --- TRIGGER 1: MANUAL SCAN ---
    public void OnScanButtonClicked()
    {
        // 1. STRICT ENFORCEMENT
        if (!isInsideCollege)
        {
            if(statusText) statusText.text = "Restricted: You must be at a college to scan.";
            return; // STOP HERE
        }

        string subjectCode = subjectInput.text;
        if (string.IsNullOrEmpty(subjectCode)) return;

        string selectedBranch = "All";
        string selectedSem = "All";
        if (branchDropdown != null) selectedBranch = branchDropdown.options[branchDropdown.value].text;
        if (semesterDropdown != null) selectedSem = semesterDropdown.options[semesterDropdown.value].text;

        StartCoroutine(FetchAssetRoutine(subjectCode, selectedBranch, selectedSem));
    }

    // --- TRIGGER 2: AI SCAN ---
    public void TriggerSearchFromAI(string subjectCode)
    {
        // 1. STRICT ENFORCEMENT
        if (!isInsideCollege)
        {
            if(statusText) statusText.text = "Board Detected, but you are not at College.\nAccess Denied.";
            return; // STOP HERE
        }

        string selectedBranch = "All";
        string selectedSem = "All";
        if (branchDropdown != null) selectedBranch = branchDropdown.options[branchDropdown.value].text;
        if (semesterDropdown != null) selectedSem = semesterDropdown.options[semesterDropdown.value].text;

        StartCoroutine(FetchAssetRoutine(subjectCode, selectedBranch, selectedSem));
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
                
                if (json.Contains("\"found\":true"))
                {
                    string filename = ExtractValue(json, "filename");
                    currentPdfUrl = ExtractValue(json, "pdf_url"); 
                    string rawGlbUrl = ExtractValue(json, "glb_url");
                    
                    string debugMsg = $"Found: {filename}";

                    // FIX 1: 3D Model
                    if (!string.IsNullOrEmpty(rawGlbUrl))
                    {
                        string directGlbLink = FixGoogleDriveLink(rawGlbUrl);
                        if (ModelDownloader.Instance != null)
                            ModelDownloader.Instance.Download3DModel(directGlbLink);
                    }

                    // FIX 2: PDF Button
                    if (!string.IsNullOrEmpty(currentPdfUrl))
                    {
                        debugMsg += "\n(PDF Available)";
                        if (pdfButton != null) pdfButton.SetActive(true);
                    }
                    
                    if(statusText) statusText.text = debugMsg;
                }
                else
                {
                    if(statusText) statusText.text = $"No match in {branch}-{sem}.";
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

    public void OpenInChrome(string url)
    {
        Application.OpenURL(url);
    }

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