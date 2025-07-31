// logs.cs
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class logs : MonoBehaviour
{
    [Header("HTTP Settings")]
    public string httpServerUrl = "http://localhost:5000/save_score";
    public bool enableHttpLogging = false; // Disabled by default
    public bool logHttpErrors = false;
    public int timeoutSeconds = 5;

    public void SaveCoords(float x, float y, float z)
    {
        if (!enableHttpLogging)
        {
            if (logHttpErrors)
                Debug.Log($"HTTP logging disabled. Would save coords: ({x}, {y}, {z})");
            return;
        }

        StartCoroutine(SendCoordsToServer(x, y, z));
    }

    IEnumerator SendCoordsToServer(float x, float y, float z)
    {
        if (string.IsNullOrEmpty(httpServerUrl))
        {
            if (logHttpErrors)
                Debug.LogWarning("HTTP server URL is not configured");
            yield break;
        }

        // Use the shared CoordinateData (from KafkaProducer.cs)
        CoordinateData coordData = new CoordinateData { x = x, y = y, z = z };
        string jsonData = JsonUtility.ToJson(coordData);

        if (logHttpErrors)
            Debug.Log("Sending JSON: " + jsonData);

        using (UnityWebRequest request = new UnityWebRequest(httpServerUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (logHttpErrors)
                    Debug.Log("Coords saved successfully!");
            }
            else
            {
                if (logHttpErrors)
                    Debug.LogError($"Error saving coords: {request.error}");
            }
        }
    }

    [ContextMenu("Enable HTTP Logging")]
    public void EnableHttpLogging()
    {
        enableHttpLogging = true;
        Debug.Log("HTTP coordinate logging enabled");
    }

    [ContextMenu("Disable HTTP Logging")]
    public void DisableHttpLogging()
    {
        enableHttpLogging = false;
        Debug.Log("HTTP coordinate logging disabled");
    }
}
