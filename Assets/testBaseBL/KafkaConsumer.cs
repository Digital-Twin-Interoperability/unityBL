using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;

[System.Serializable]
public class KafkaMessage
{
    public string topic;
    public int partition;
    public string offset;
    public string key;
    public string value;
    public string timestamp;
}

[System.Serializable]
public class HttpKafkaResponse
{
    public string topic;
    public HttpKafkaMessage[] messages;
    public int count;
    public string timestamp;
}

[System.Serializable]
public class HttpKafkaMessage
{
    public string topic;
    public int partition;
    public string offset;
    public string key;
    public string value;
    public string timestamp;
}

public class KafkaConsumer : MonoBehaviour
{
    public static KafkaConsumer Instance;

    [Header("HTTP Consumer Settings")]
    public string httpServerUrl = "http://localhost:8082";
    public float pollInterval = 0.5f; // Faster polling for real-time feel
    public bool useHttpPolling = true;

    [Header("Topics to Monitor")]
    public string[] topicsToConsume = { "movement-commands", "omniverse-position-data" };

    private Dictionary<string, System.Action<KafkaMessage>> messageHandlers = new Dictionary<string, System.Action<KafkaMessage>>();
    private bool isPolling = false;

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
        // Register default handlers
        RegisterMessageHandler("movement-commands", HandleMovementCommand);

        if (useHttpPolling)
        {
            Debug.Log("Starting HTTP polling for Kafka messages");
            StartHttpPolling();
        }
    }

    public void RegisterMessageHandler(string topic, System.Action<KafkaMessage> handler)
    {
        messageHandlers[topic] = handler;
        Debug.Log($"Registered message handler for topic: {topic}");
    }

    public void StartHttpPolling()
    {
        if (!isPolling && useHttpPolling)
        {
            isPolling = true;
            StartCoroutine(HttpPollingLoop());
            Debug.Log("Started HTTP polling for messages");
        }
    }

    public void StopHttpPolling()
    {
        isPolling = false;
        Debug.Log("Stopped HTTP polling for messages");
    }

    private IEnumerator HttpPollingLoop()
    {
        while (isPolling && useHttpPolling)
        {
            // Poll each topic
            foreach (string topic in topicsToConsume)
            {
                if (messageHandlers.ContainsKey(topic))
                {
                    yield return StartCoroutine(PollTopicHttp(topic));
                }
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private IEnumerator PollTopicHttp(string topic)
    {
        string url = $"{httpServerUrl}/consume/{topic}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string jsonResponse = request.downloadHandler.text;

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    HttpKafkaResponse response = JsonUtility.FromJson<HttpKafkaResponse>(jsonResponse);

                    if (response != null && response.messages != null && response.messages.Length > 0)
                    {
                        Debug.Log($"Received {response.messages.Length} messages from {response.topic}");

                        foreach (var httpMessage in response.messages)
                        {
                            // Convert to KafkaMessage format
                            KafkaMessage message = new KafkaMessage
                            {
                                topic = httpMessage.topic,
                                partition = httpMessage.partition,
                                offset = httpMessage.offset,
                                key = httpMessage.key,
                                value = httpMessage.value,
                                timestamp = httpMessage.timestamp
                            };

                            ProcessMessage(message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing HTTP response for topic {topic}: {e.Message}");
                Debug.LogError($"Raw response: {request.downloadHandler.text}");
            }
        }
        else if (request.responseCode != 404) // 404 is normal when no messages
        {
            Debug.LogWarning($"HTTP request failed for topic {topic}: {request.error} (Code: {request.responseCode})");
        }
    }

    private void ProcessMessage(KafkaMessage message)
    {
        Debug.Log($"Processing message from topic '{message.topic}': {message.value}");

        if (messageHandlers.ContainsKey(message.topic))
        {
            try
            {
                messageHandlers[message.topic](message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing message from topic '{message.topic}': {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"No handler registered for topic: {message.topic}");
        }
    }

    private void HandleMovementCommand(KafkaMessage message)
    {
        Debug.Log($"Movement command received: {message.value}");
    }

    // Legacy methods for compatibility
    public void AddTopic(string topic)
    {
        var list = new List<string>(topicsToConsume ?? new string[0]);
        if (!list.Contains(topic))
        {
            list.Add(topic);
            topicsToConsume = list.ToArray();
            Debug.Log($"Added topic: {topic}");
        }
    }

    public void PollNow()
    {
        if (useHttpPolling)
        {
            StartCoroutine(PollAllTopicsOnce());
        }
        else
        {
            Debug.LogWarning("PollNow called but HTTP polling is disabled.");
        }
    }

    private IEnumerator PollAllTopicsOnce()
    {
        foreach (string topic in topicsToConsume)
        {
            if (messageHandlers.ContainsKey(topic))
            {
                yield return StartCoroutine(PollTopicHttp(topic));
            }
        }
    }

    public void ConsumeFromTopicManually(string topic, System.Action<KafkaMessage> handler)
    {
        RegisterMessageHandler(topic, handler);
        AddTopic(topic);
        PollNow();
    }

    public void ConsumeFromTopicManually(string topic)
    {
        if (messageHandlers.ContainsKey(topic))
        {
            StartCoroutine(PollTopicHttp(topic));
        }
        else
        {
            Debug.LogWarning($"No handler registered for topic '{topic}'. Register a handler first using RegisterMessageHandler().");
        }
    }

    // Test methods
    [ContextMenu("Test HTTP Connection")]
    public void TestConnection()
    {
        StartCoroutine(TestHttpConnection());
    }

    private IEnumerator TestHttpConnection()
    {
        string healthUrl = $"{httpServerUrl}/health";
        UnityWebRequest request = UnityWebRequest.Get(healthUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("HTTP connection successful!");
            Debug.Log($"Health response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"HTTP connection failed: {request.error}");
        }
    }

    [ContextMenu("Manual Poll All Topics")]
    public void ManualPollAll()
    {
        StartCoroutine(PollAllTopicsOnce());
    }

    void OnDestroy()
    {
        StopHttpPolling();
    }

    // Public setters
    public void SetPollInterval(float interval)
    {
        pollInterval = interval;
        Debug.Log($"Poll interval set to: {interval} seconds");
    }

    public void EnableHttpPolling(bool enabled)
    {
        useHttpPolling = enabled;
        if (enabled && !isPolling)
        {
            StartHttpPolling();
        }
        else if (!enabled && isPolling)
        {
            StopHttpPolling();
        }
    }
}