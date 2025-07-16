using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class CoordinateData
{
    public float x;
    public float y;
    public float z;

    public CoordinateData(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

public class logs : MonoBehaviour
{
    public void SaveCoords(float x, float y, float z)  // Changed to float
    {
        StartCoroutine(SendCoordsToServer(x, y, z));
    }

    IEnumerator SendCoordsToServer(float x, float y, float z)
    {
        string url = "http://localhost:5000/save_score";

        // Use the serializable class
        CoordinateData coordData = new CoordinateData(x, y, z);
        string jsonData = JsonUtility.ToJson(coordData);

        Debug.Log("Sending JSON: " + jsonData);  // Debug the JSON being sent

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Coords saved successfully!");
        }
        else
        {
            Debug.LogError("Error saving coords: " + request.error);
        }
    }
}