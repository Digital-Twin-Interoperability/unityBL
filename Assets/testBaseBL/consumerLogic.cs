using UnityEngine;
using System;
using System.Collections.Generic;

public class consumerLogic : MonoBehaviour
{
    private GameObject targetObject;
    private bool connected = false;

    [SerializeField] private string objectPath = "Rover"; // Unity GameObject name

    void Start()
    {
        if (ConnectToUnityObject(objectPath))
        {
            Debug.Log("Connected to Unity object.");
            // Example HSML data
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
            Debug.LogError("Could not find target object.");
        }
    }

    public bool ConnectToUnityObject(string path)
    {
        targetObject = GameObject.Find(path);
        connected = targetObject != null;
        return connected;
    }

    public void ProcessHsmlMovement(Dictionary<string, object> hsmlData)
    {
        if (!connected)
        {
            Debug.LogError("Not connected to Unity object.");
            return;
        }

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
        targetObject.transform.SetPositionAndRotation(transformData.position, transformData.rotation);
        Debug.Log($"Moved object to Position: {transformData.position}, Rotation: {transformData.rotation}");
    }
}
