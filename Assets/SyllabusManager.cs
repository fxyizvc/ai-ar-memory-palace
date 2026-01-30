using UnityEngine;
using System.Collections.Generic;
using TMPro; 

public class SyllabusManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown branchDropdown;
    public TMP_Dropdown semesterDropdown;
    public TMP_Dropdown subjectDropdown; 

    // Data Store
    private Dictionary<string, Dictionary<string, List<string>>> syllabusData = new Dictionary<string, Dictionary<string, List<string>>>();

    void Start()
    {
        InitializeSyllabus();

        // Detect changes
        branchDropdown.onValueChanged.AddListener(delegate { UpdateSubjects(); });
        semesterDropdown.onValueChanged.AddListener(delegate { UpdateSubjects(); });

        // Run once at start
        UpdateSubjects();
    }

    void UpdateSubjects()
    {
        // 1. Get Text & Normalize
        string branch = branchDropdown.options[branchDropdown.value].text.ToUpper().Trim();
        string sem = semesterDropdown.options[semesterDropdown.value].text.ToUpper().Trim();

        // --- DEBUGGING START ---
        Debug.Log($"[SyllabusManager] Selected: '{branch}' and '{sem}'");
        // --- DEBUGGING END ---

        // 2. Clear old options
        subjectDropdown.ClearOptions();

        // 3. Find Matches
        if (syllabusData.ContainsKey(branch))
        {
            if (syllabusData[branch].ContainsKey(sem))
            {
                List<string> subjects = syllabusData[branch][sem];
                subjectDropdown.AddOptions(subjects);
                // Debug.Log($"[SyllabusManager] Found {subjects.Count} subjects.");
            }
            else
            {
                subjectDropdown.AddOptions(new List<string> { "No Subjects Found" });
                Debug.LogError($"[SyllabusManager] Branch '{branch}' found, but Semester '{sem}' is missing from database!");
            }
        }
        else
        {
            subjectDropdown.AddOptions(new List<string> { "No Subjects Found" });
            Debug.LogError($"[SyllabusManager] Branch '{branch}' NOT found in database! (Check spelling in Dropdown Options)");
        }
    }

    // --- DATA ENTRY ---
    void InitializeSyllabus()
    {
        void Add(string branch, string sem, params string[] subs)
        {
            branch = branch.ToUpper().Trim();
            sem = sem.ToUpper().Trim();

            if (!syllabusData.ContainsKey(branch)) syllabusData[branch] = new Dictionary<string, List<string>>();
            if (!syllabusData[branch].ContainsKey(sem)) syllabusData[branch][sem] = new List<string>();
            syllabusData[branch][sem].AddRange(subs);
        }

        // === S1 & S2 (COMMON) ===
        string[] s1Subs = { "MAT101", "PHT100", "CYT100", "EST100", "EST110", "HUT101" };
        string[] s2Subs = { "MAT102", "PHT110", "CYT110", "EST102", "HUT102" };

        // Ensure these match your Dropdown Options EXACTLY
        foreach(string b in new string[] { "CSE", "CE", "ME", "EEE" }) {
            Add(b, "S1", s1Subs);
            Add(b, "S2", s2Subs);
        }

        // === CSE ===
        Add("CSE", "S3", "MAT203", "CST201", "CST203", "CST205", "EST200", "HUT200", "MCN201");
        Add("CSE", "S4", "MAT206", "CST202", "CST204", "CST206", "MCN202", "MCN204", "HUT200");
        Add("CSE", "S5", "CST301", "CST303", "CST305", "CST307", "CST309", "MCN301");
        Add("CSE", "S6", "CST302", "CST304", "CST306", "CST308", "HUT300");
        Add("CSE", "S7", "CST401", "CST403", "CST405", "CST407", "MCN401");
        Add("CSE", "S8", "CST402", "CST404", "CST406");

        // === EEE (Using ECE Data) ===
        Add("EEE", "S3", "MAT203", "ECT201", "ECT203", "ECT205", "ECT207", "HUT200", "MCN201");
        Add("EEE", "S4", "MAT206", "ECT202", "ECT204", "ECT206", "ECT208", "HUT200", "MCN202");
        Add("EEE", "S5", "ECT301", "ECT303", "ECT305", "ECT307", "MCN301");
        Add("EEE", "S6", "ECT302", "ECT304", "ECT306", "ECT308", "MCN302");
        Add("EEE", "S7", "ECT401", "ECT403", "ECT405", "MCN401");
        Add("EEE", "S8", "ECT402", "ECT404");

        // === MECH (ME) ===
        Add("ME", "S3", "MAT203", "MET201", "MET203", "MET205", "MET207", "HUT200", "MCN201");
        Add("ME", "S4", "MAT206", "MET202", "MET204", "MET206", "MET208", "HUT200", "MCN202");
        Add("ME", "S5", "MET301", "MET303", "MET305", "MET307", "MCN301");
        Add("ME", "S6", "MET302", "MET304", "MET306", "MET308", "MCN302");
        Add("ME", "S7", "MET401", "MET403", "MET405", "MCN401");
        Add("ME", "S8", "MET402");

        // === CIVIL (CE) ===
        Add("CE", "S3", "MAT203", "CET201", "CET203", "CET205", "CET207", "HUT200", "MCN201");
        Add("CE", "S4", "MAT206", "CET202", "CET204", "CET206", "CET208", "HUT200", "MCN202");
        Add("CE", "S5", "CET301", "CET303", "CET305", "CET307", "CET309", "MCN301");
        Add("CE", "S6", "CET302", "CET304", "CET306", "CET308", "MCN302");
        Add("CE", "S7", "CET401", "CET403", "CET405", "MCN401");
        Add("CE", "S8", "CET402");
    }
}