using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class mover : MonoBehaviour
{
    private NavMeshAgent agent;
    public LayerMask layerMask;
    public logs coordsLogger;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                Debug.Log("Click registered at: " + hit.point);
                Vector3 objectPosition = transform.position;
                Quaternion objectRotation = transform.rotation;

                var hsmlPayload = new Dictionary<string, object>
                {
                    { "entity_id", gameObject.name },
                    { "position", new Dictionary<string, object>
                        {
                            { "x", objectPosition.x },
                            { "y", objectPosition.y },
                            { "z", objectPosition.z }
                        }
                    },
                    { "rotation", new Dictionary<string, object>
                        {
                            { "x", objectRotation.x },
                            { "y", objectRotation.y },
                            { "z", objectRotation.z },
                            { "w", objectRotation.w }
                        }
                    },
                    { "timestamp", System.DateTime.UtcNow.ToString("o") }
                };

                // Send to expected topics
                KafkaProducer.Instance.SendToTopic("coordinate-logs", gameObject.name, hsmlPayload);
                KafkaProducer.Instance.SendToTopic("hsml-data", gameObject.name, hsmlPayload);

                // Optional: local file logging
                if (coordsLogger != null)
                    coordsLogger.SaveCoords(objectPosition.x, objectPosition.y, objectPosition.z);

                agent.SetDestination(hit.point);
            }
        }
    }
}
