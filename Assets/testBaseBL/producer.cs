// Producer.cs
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class producer : MonoBehaviour
{
    [Header("Legacy HTTP Settings")]
    public string apiUrl = "http://localhost:5000/save_score"; // Keep for backwards compatibility

    [Header("Kafka Settings")]
    public float sendInterval = 1.0f; // Send position updates every second
    public bool useKafka = true;
    public bool useHTTP = false; // Disable HTTP by default

    void Start()
    {
        StartCoroutine(SendPositionPeriodically());
    }

    IEnumerator SendPositionPeriodically()
    {
        while (true)
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            if (useKafka)
            {
                // Build a shared-dictionary message for agent-position-updates
                var agentData = new Dictionary<string, object>
                {
                    { "entity_id", gameObject.name },
                    { "position", new Dictionary<string, object>
                        {
                            { "x", pos.x },
                            { "y", pos.y },
                            { "z", pos.z }
                        }
                    },
                    { "rotation", new Dictionary<string, object>
                        {
                            { "x", rot.x },
                            { "y", rot.y },
                            { "z", rot.z },
                            { "w", rot.w }
                        }
                    },
                    { "timestamp", System.DateTime.UtcNow.ToString("o") }
                };
                KafkaProducer.Instance.SendToTopic("agent-position-updates", gameObject.name, agentData);

                // HSML-standardized format
                var hsmlData = OmniverseProducerUtil.ConvertUnityToHsml(gameObject);
                KafkaProducer.Instance.SendToTopic("hsml-data", gameObject.name, hsmlData);
            }

            if (useHTTP)
            {
                var payload = new CoordinateData { x = pos.x, y = pos.y, z = pos.z };
                yield return StartCoroutine(SendPositionHTTP(payload));
            }

            yield return new WaitForSeconds(sendInterval);
        }
    }

    IEnumerator SendPositionHTTP(CoordinateData payload)
    {
        string json = JsonUtility.ToJson(payload);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Position sent via HTTP successfully!");
        }
        else
        {
            Debug.LogError("Error sending position via HTTP: " + request.error);
        }
    }
}
