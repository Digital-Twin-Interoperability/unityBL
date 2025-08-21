using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class consumerLogic : MonoBehaviour
{
    [SerializeField] private GameObject targetObject; // Direct GameObject reference
    [SerializeField] private string targetEntityId = "";
    private bool connected = false;

    void Start()
    {
        if (ConnectToUnityObject())
        {
            Debug.Log("Connected to Unity object: " + targetObject.name);
            RegisterWithKafkaConsumer();
        }
        else
        {
            Debug.LogError("Target object is not assigned in the inspector!");
        }
    }

    private void RegisterWithKafkaConsumer()
    {
        StartCoroutine(RegisterAfterFrame());
    }

    private IEnumerator RegisterAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        if (KafkaConsumer.Instance != null)
        {
            KafkaConsumer.Instance.RegisterMessageHandler("omniverse-position-data", OnOmniversePositionReceived);
            Debug.Log("Registered handler for omniverse-position-data topic");

            // Ensure the topic is being monitored
            KafkaConsumer.Instance.AddTopic("omniverse-position-data");

            // If HTTP polling is enabled, make sure it's started
            if (KafkaConsumer.Instance.useHttpPolling)
            {
                KafkaConsumer.Instance.StartHttpPolling();
            }
        }
        else
        {
            Debug.LogError("KafkaConsumer.Instance is null! Make sure KafkaConsumer is in the scene.");
        }
    }

    // This method will be called automatically by KafkaConsumer when messages arrive
    private void OnOmniversePositionReceived(KafkaMessage message)
    {
        if (string.IsNullOrEmpty(message.value))
        {
            return;
        }

        try
        {
            Debug.Log($"Received omniverse position data: {message.value}");

            // Parse the JSON message directly
            var omniverseData = ParseKafkaMessage(message.value);

            Debug.Log($"Raw JSON received: {message.value}");

            if (omniverseData != null)
            {
                // Filter by entity ID if specified
                if (!string.IsNullOrEmpty(targetEntityId) &&
                    omniverseData.entity_id != targetEntityId)
                {
                    Debug.Log($"Ignoring message for entity '{omniverseData.entity_id}' - looking for '{targetEntityId}'");
                    return;
                }

                ProcessOmniverseMovement(omniverseData);
            }
            else
            {
                Debug.LogWarning("Failed to parse omniverse data");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing omniverse position data: {e.Message}");
            Debug.LogError($"Raw message value: {message.value}");
        }
    }

    public bool ConnectToUnityObject()
    {
        connected = targetObject != null;
        return connected;
    }

    public void SetTargetObject(GameObject newTarget)
    {
        targetObject = newTarget;
        connected = targetObject != null;

        if (connected)
        {
            Debug.Log("Target object updated to: " + targetObject.name);
        }
    }

    // Updated serializable classes to match your JSON structure
    [System.Serializable]
    public class KafkaMessageWrapper
    {
        public string topic;
        public string key;
        public OmniversePositionData message;
    }

    [System.Serializable]
    public class OmniversePositionData
    {
        public string entity_id;
        public PositionData position;
        public RotationData rotation;
        public string timestamp;
    }

    [System.Serializable]
    public class PositionData
    {
        public float x, y, z;
    }

    [System.Serializable]
    public class RotationData
    {
        public float x, y, z, w;
    }

    private OmniversePositionData ParseKafkaMessage(string json)
    {
        try
        {
            Debug.Log($"Attempting to parse JSON: {json}");

            // First try direct format (Omniverse)
            var omniverseData = JsonUtility.FromJson<OmniversePositionData>(json);
            if (omniverseData != null && !string.IsNullOrEmpty(omniverseData.entity_id))
            {
                Debug.Log("Parsed as direct Omniverse format");
                return omniverseData;
            }

            // Try wrapped format (Unity producer)
            var wrapper = JsonUtility.FromJson<KafkaMessageWrapper>(json);
            if (wrapper != null && wrapper.message != null)
            {
                Debug.Log("Parsed as wrapped Unity producer format");
                return wrapper.message;
            }

            Debug.LogError("Could not parse as either format");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse Kafka JSON: {e.Message}. JSON: {json}");
            return null;
        }
    }

    public void ProcessOmniverseMovement(OmniversePositionData omniverseData)
    {
        Debug.Log($"Script is attached to: {gameObject.name}, Target Object is: {(targetObject ? targetObject.name : "NULL")}");
        if (!connected || targetObject == null)
        {
            Debug.LogError("Not connected to Unity object or target object is null.");
            return;
        }

        if (omniverseData == null)
        {
            Debug.LogError("OmniversePositionData is null");
            return;
        }

        // Convert Omniverse coordinates to Unity coordinates
        var transformData = ConvertOmniverseToUnityTransform(omniverseData);

        // Move the object
        MoveObject(transformData);

        Debug.Log($"Moved {targetObject.name} to Position: {transformData.position}, Rotation: {transformData.rotation}");
    }

    private (Vector3 position, Quaternion rotation) ConvertOmniverseToUnityTransform(OmniversePositionData omniverseData)
    {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        try
        {
            // Extract position with coordinate transformation
            if (omniverseData.position != null)
            {
                // Apply coordinate transformation:
                // - Switch Y and Z coordinates 
                // - Apply X offset of -90
                // - Apply Y offset of +110
                position = new Vector3(
                    -omniverseData.position.y/100f,    // X with -90 offset
                    0,   // Y becomes Z with +110 offset
                    -omniverseData.position.x/100f           // Z becomes Y (switched)
                );
                
                Debug.Log($"Original position: ({omniverseData.position.x}, {omniverseData.position.y}, {omniverseData.position.z})");
                Debug.Log($"Transformed position: {position}");
            }
            else
            {
                Debug.LogWarning("Position data is null, using default position");
            }

            // Extract rotation
            if (omniverseData.rotation != null)
            {
                rotation = new Quaternion(
                    omniverseData.rotation.x+90f,
                    omniverseData.rotation.y,
                    omniverseData.rotation.z,
                    omniverseData.rotation.w-90f
                );
                Debug.Log($"Extracted rotation: {rotation}");
            }
            else
            {
                Debug.LogWarning("Rotation data is null, using default rotation");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error converting omniverse data to Unity transform: {e.Message}");
        }

        return (position, rotation);
    }

    private void MoveObject((Vector3 position, Quaternion rotation) transformData)
    {
        if (targetObject != null)
        {
            // Apply the position and rotation directly
            targetObject.transform.position = transformData.position;
            targetObject.transform.rotation = transformData.rotation;

            Debug.Log($"Successfully applied transform - Position: {transformData.position}, Rotation: {transformData.rotation}");
        }
        else
        {
            Debug.LogError("Cannot move object: targetObject is null");
        }
    }

    // Testing and validation methods
    [ContextMenu("Validate Target Object")]
    public void ValidateTargetObject()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target object is not assigned!");
        }
        else
        {
            Debug.Log($"Target object is assigned: {targetObject.name}");
            Debug.Log($"Looking for entity_id: {targetEntityId}");
            Debug.Log($"Connected status: {connected}");
        }
    }

    [ContextMenu("Test Manual Consume")]
    public void TestManualConsume()
    {
        if (KafkaConsumer.Instance != null)
        {
            Debug.Log("Triggering manual consume for omniverse-position-data");
            KafkaConsumer.Instance.ConsumeFromTopicManually("omniverse-position-data");
        }
        else
        {
            Debug.LogError("KafkaConsumer.Instance is null!");
        }
    }

    [ContextMenu("Test HTTP Connection")]
    public void TestHttpConnection()
    {
        if (KafkaConsumer.Instance != null)
        {
            KafkaConsumer.Instance.TestConnection();
        }
        else
        {
            Debug.LogError("KafkaConsumer.Instance is null!");
        }
    }

    [ContextMenu("Force Poll Now")]
    public void ForcePollNow()
    {
        if (KafkaConsumer.Instance != null)
        {
            KafkaConsumer.Instance.PollNow();
        }
        else
        {
            Debug.LogError("KafkaConsumer.Instance is null!");
        }
    }

    public void SetTargetEntityId(string entityId)
    {
        targetEntityId = entityId;
        Debug.Log($"Target entity ID set to: {entityId}");
    }

    // Debug method to simulate receiving a message
    [ContextMenu("Simulate Position Message")]
    public void SimulatePositionMessage()
    {
        string testJson = @"{
            ""topic"": ""omniverse-position-data"",
            ""key"": ""test-key"",
            ""message"": {
                ""entity_id"": """ + targetEntityId + @""",
                ""position"": {
                    ""x"": 5.0,
                    ""y"": 1.0,
                    ""z"": 3.0
                },
                ""rotation"": {
                    ""x"": 0.0,
                    ""y"": 0.0,
                    ""z"": 0.0,
                    ""w"": 1.0
                },
                ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
            }
        }";

        var testMessage = new KafkaMessage
        {
            topic = "omniverse-position-data",
            value = testJson
        };

        Debug.Log($"Simulating message: {testJson}");
        OnOmniversePositionReceived(testMessage);
    }
}