using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class HsmlPositionSender : MonoBehaviour
{
    public string apiUrl = "http://localhost:5000/save_score";

    void Start()
    {
        StartCoroutine(SendPosition());
    }

    IEnumerator SendPosition()
    {
        Vector3 position = transform.position;
        PositionPayload payload = new PositionPayload(position);

        string json = JsonUtility.ToJson(payload);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Position sent successfully!");
        }
        else
        {
            Debug.LogError("Error sending position: " + request.error);
        }
    }
}
