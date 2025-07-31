using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class KafkaProducer : MonoBehaviour
{
    public static KafkaProducer Instance;

    private string kafkaProxyUrl = "http://localhost:8082/produce";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(TestConnection());
    }

    public void SendToTopic(string topic, string key, object message)
    {
        // Auto-convert if it's AgentPositionData
        if (message is AgentPositionData apd)
        {
            message = ConvertToDict(apd);
        }

        StartCoroutine(SendKafkaHttp(topic, key, message));
    }

    IEnumerator SendKafkaHttp(string topic, string key, object message)
    {
        Dictionary<string, object> hsmlData = message as Dictionary<string, object>;
        if (hsmlData == null)
        {
            Debug.LogError("KafkaProducer expected a Dictionary<string, object> but got: " + message.GetType());
            yield break;
        }

        string json = WrapKafkaPayload(topic, key, hsmlData);

        Debug.Log("Kafka JSON: " + json);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(kafkaProxyUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Kafka message sent via HTTP proxy.");
        }
        else
        {
            Debug.LogWarning("Kafka HTTP send failed: " + request.error);
        }
    }

    IEnumerator TestConnection()
    {
        Debug.Log("Testing Kafka connection...");

        Dictionary<string, object> dummyMessage = new Dictionary<string, object>()
        {
            { "entity_id", "test-agent" },
            { "position", new Dictionary<string, object> { { "x", 0f }, { "y", 0f }, { "z", 0f } } },
            { "rotation", new Dictionary<string, object> { { "x", 0f }, { "y", 0f }, { "z", 0f }, { "w", 1f } } },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        };

        string json = WrapKafkaPayload("hsml-data", "test-key", dummyMessage);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(kafkaProxyUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Kafka HTTP connection succeeded.");
        }
        else
        {
            Debug.LogWarning("Kafka connection failed: " + request.error);
        }
    }

    string WrapKafkaPayload(string topic, string key, Dictionary<string, object> hsmlData)
    {
        Dictionary<string, object> pos = (Dictionary<string, object>)hsmlData["position"];
        Dictionary<string, object> rot = (Dictionary<string, object>)hsmlData["rotation"];
        string entityId = hsmlData["entity_id"].ToString();
        string timestamp = hsmlData["timestamp"].ToString();

        // Escape quotes
        entityId = entityId.Replace("\"", "\\\"");
        timestamp = timestamp.Replace("\"", "\\\"");

        string messageJson = $@"
        {{
            ""entity_id"": ""{entityId}"",
            ""position"": {{
                ""x"": {pos["x"]},
                ""y"": {pos["y"]},
                ""z"": {pos["z"]}
            }},
            ""rotation"": {{
                ""x"": {rot["x"]},
                ""y"": {rot["y"]},
                ""z"": {rot["z"]},
                ""w"": {rot["w"]}
            }},
            ""timestamp"": ""{timestamp}""
        }}";

        return $@"
        {{
            ""topic"": ""{topic}"",
            ""key"": ""{key}"",
            ""message"": {messageJson}
        }}";
    }

    // Converts AgentPositionData into Kafka-compatible Dictionary
    private Dictionary<string, object> ConvertToDict(AgentPositionData data)
    {
        var position = new Dictionary<string, object>
        {
            { "x", data.position.x },
            { "y", data.position.y },
            { "z", data.position.z }
        };

        var rotation = new Dictionary<string, object>
        {
            { "x", data.rotation.x },
            { "y", data.rotation.y },
            { "z", data.rotation.z },
            { "w", data.rotation.w }
        };

        return new Dictionary<string, object>
        {
            { "entity_id", data.entity_id },
            { "position", position },
            { "rotation", rotation },
            { "timestamp", data.timestamp }
        };
    }
}

// Basic agent + transform class definitions
[System.Serializable]
public class AgentPositionData
{
    public string entity_id;
    public CoordinateData position;
    public CoordinateData rotation;
    public string timestamp;
}

[System.Serializable]
public class CoordinateData
{
    public float x;
    public float y;
    public float z;
    public float w; // Only for rotation
}
