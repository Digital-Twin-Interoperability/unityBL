using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class HsmlPositionSender : MonoBehaviour
{
    [System.Serializable]
    public class PositionPayload
    {
        public float x, y, z;

        public PositionPayload(Vector3 position)
        {
            x = position.x;
            y = position.y;
            z = position.z;
        }
    }

    [Header("Settings")]
    public float sendInterval = 2.0f;
    public bool useKafka = true;
    public bool useHTTP = false;

    [Header("Legacy HTTP Settings")]
    public string apiUrl = "http://localhost:5000/save_score";

    void Start()
    {
        StartCoroutine(SendPositionPeriodically());
    }

    IEnumerator SendPositionPeriodically()
    {
        while (true)
        {
            if (useKafka)
            {
                var hsmlData = OmniverseProducerUtil.ConvertUnityToHsml(gameObject);
                KafkaProducer.Instance.SendToTopic("hsml-data", gameObject.name, hsmlData);
            }

            if (useHTTP)
            {
                yield return StartCoroutine(SendPositionHTTP());
            }

            yield return new WaitForSeconds(sendInterval);
        }
    }

    IEnumerator SendPositionHTTP()
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
            Debug.Log("HSML Position sent via HTTP successfully!");
        }
        else
        {
            Debug.LogError("Error sending HSML position via HTTP: " + request.error);
        }
    }
}