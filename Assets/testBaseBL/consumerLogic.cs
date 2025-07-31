using UnityEngine;
using System;
using System.Collections.Generic;

public class consumerLogic : MonoBehaviour
{
    [SerializeField] private GameObject targetObject; // Direct GameObject reference
    private bool connected = false;

    void Start()
    {
        if (ConnectToUnityObject())
        {
            Debug.Log("Connected to Unity object: " + targetObject.name);

            // Example HSML data - this would normally come from Kafka
            Dictionary<string, object> hsmlData = new Dictionary<string, object>
            {
                { "entity_id", "test_unity_agent" },
                { "position", new Dictionary<string, object> { { "x", 10.0f }, { "y", 5.0f }, { "z", 2.0f } } },
                { "rotation", new Dictionary<string, object> { { "x", 0f }, { "y", 0f }, { "z", 0f }, { "w", 1f } } },
                { "timestamp", "2024-06-01T12:00:00Z" }
            };

            ProcessHsmlMovement(hsmlData);
        }
        else
        {
            Debug.LogError("Target object is not assigned in the inspector!");
        }
    }

    public bool ConnectToUnityObject()
    {
        connected = targetObject != null;
        return connected;
    }

    // Optional: method to set target object at runtime
    public void SetTargetObject(GameObject newTarget)
    {
        targetObject = newTarget;
        connected = targetObject != null;

        if (connected)
        {
            Debug.Log("Target object updated to: " + targetObject.name);
        }
    }

    public void ProcessHsmlMovement(Dictionary<string, object> hsmlData)
    {
        if (!connected || targetObject == null)
        {
            Debug.LogError("Not connected to Unity object or target object is null.");
            return;
        }

        // Send movement command to cross-platform topic
        var movementCommand = new
        {
            command_type = "move_to_position",
            platform = "unity",
            target_entity = hsmlData["entity_id"].ToString(),
            hsml_data = hsmlData,
            timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        KafkaProducer.Instance.SendToTopic("movement-commands",
            hsmlData["entity_id"].ToString(),
            movementCommand);

        // Send HSML data to standardized topic
        KafkaProducer.Instance.SendToTopic("hsml-data",
            hsmlData["entity_id"].ToString(),
            hsmlData);

        // Convert to Omniverse format and send to sync topic
        var omniverseData = PluginLogic.HsmlToOmniverse(hsmlData);
        KafkaProducer.Instance.SendToTopic("scene-sync",
            omniverseData["id"].ToString(),
            omniverseData);

        var transformData = ConvertHsmlToUnityTransform(hsmlData);
        MoveObject(transformData);
    }

    private (Vector3 position, Quaternion rotation) ConvertHsmlToUnityTransform(Dictionary<string, object> hsml)
    {
        Dictionary<string, object> positionDict = hsml["position"] as Dictionary<string, object>;
        Dictionary<string, object> rotationDict = hsml["rotation"] as Dictionary<string, object>;

        Vector3 position = new Vector3(
            Convert.ToSingle(positionDict["x"]),
            Convert.ToSingle(positionDict["y"]),
            Convert.ToSingle(positionDict["z"])
        );

        Quaternion rotation = new Quaternion(
            Convert.ToSingle(rotationDict["x"]),
            Convert.ToSingle(rotationDict["y"]),
            Convert.ToSingle(rotationDict["z"]),
            Convert.ToSingle(rotationDict["w"])
        );

        return (position, rotation);
    }

    private void MoveObject((Vector3 position, Quaternion rotation) transformData)
    {
        if (targetObject != null)
        {
            targetObject.transform.SetPositionAndRotation(transformData.position, transformData.rotation);
            Debug.Log($"Moved {targetObject.name} to Position: {transformData.position}, Rotation: {transformData.rotation}");
        }
        else
        {
            Debug.LogError("Cannot move object: targetObject is null");
        }
    }

    // Optional: Validation method you can call from inspector or other scripts
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
        }
    }
}