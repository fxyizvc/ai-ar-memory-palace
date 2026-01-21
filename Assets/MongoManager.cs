using UnityEngine;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using TMPro;

public class MongoManager : MonoBehaviour
{
    [Header("Configuration")]
    // Paste your Connection String here (from Atlas > Connect > Drivers)
    private string connectionString = "mongodb+srv://<db_username>:<db_password>@cluster0.kqm7txf.mongodb.net/?appName=Cluster0";
    
    [Header("Database Settings")]
    public string databaseName = "arProjectDB";
    public string collectionName = "assets"; // As seen in your screenshot

    [Header("UI")]
    public TextMeshProUGUI statusText;

    // Static variables for the rest of the app to access
    public static string FoundGLB = "";
    public static string FoundPDF = "";
    public static string FoundSubject = "";

    private MongoClient client;
    private IMongoDatabase db;
    private IMongoCollection<BsonDocument> collection;

    void Start()
    {
        // Initialize connection when app starts
        try {
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            
            client = new MongoClient(settings);
            db = client.GetDatabase(databaseName);
            collection = db.GetCollection<BsonDocument>(collectionName);
            
            Debug.Log("MongoDB Client Initialized");
        }
        catch (System.Exception e) {
            statusText.text = "DB Init Error: " + e.Message;
        }
    }

    // Call this function when you have GPS coordinates
    public async void FindNearestCollege(float userLat, float userLon)
    {
        statusText.text = "Checking Database...";

        await Task.Run(() => 
        {
            try 
            {
                // 1. Get ALL colleges (We filter distance in C# because coordinates are strings)
                // Note: If you have 1000s of colleges, this needs optimization. For now, it's fine.
                var documents = collection.Find(new BsonDocument()).ToList();

                float minDistance = 500000f; // Start huge
                BsonDocument nearestDoc = null;

                foreach (var doc in documents)
                {
                    // Check if document has coordinates
                    if (!doc.Contains("coordinate")) continue;

                    string coordStr = doc["coordinate"].AsString; // "12,13,134"
                    string[] parts = coordStr.Split(',');

                    if (parts.Length >= 2)
                    {
                        if (float.TryParse(parts[0], out float dbLat) && float.TryParse(parts[1], out float dbLon))
                        {
                            float dist = HaversineDistance(userLat, userLon, dbLat, dbLon);
                            
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                nearestDoc = doc;
                            }
                        }
                    }
                }

                // 2. Report Result back to Main Thread
                UnityMainThreadDispatcher.Instance().Enqueue(() => 
                {
                    if (nearestDoc != null && minDistance < 500) // 500 meters range
                    {
                        FoundSubject = nearestDoc.Contains("filename") ? nearestDoc["filename"].AsString : "Unknown";
                        FoundGLB = nearestDoc.Contains("glb_url") ? nearestDoc["glb_url"].AsString : "";
                        FoundPDF = nearestDoc.Contains("pdf_url") ? nearestDoc["pdf_url"].AsString : "";
                        
                        statusText.text = $"Found: {FoundSubject}\n({minDistance:F1}m)";
                        
                        // Optional: Auto-trigger download logic here
                    }
                    else
                    {
                        statusText.text = $"No class nearby.\nClosest: {minDistance:F0}m";
                    }
                });

            }
            catch (System.Exception e)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    statusText.text = "Fetch Error: " + e.Message;
                });
            }
        });
    }

    // Math for GPS distance
    float HaversineDistance(float lat1, float lon1, float lat2, float lon2)
    {
        float R = 6371000f; 
        float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        float dLon = (lon2 - lon1) * Mathf.Deg2Rad;
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return R * c;
    }
}

// Helper to run code on the main Unity thread (Required for UI updates)
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<System.Action> _executionQueue = new Queue<System.Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    public void Enqueue(System.Action action)
    {
        lock (_executionQueue) { _executionQueue.Enqueue(action); }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0) { _executionQueue.Dequeue().Invoke(); }
        }
    }
}