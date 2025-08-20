using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class mover : MonoBehaviour
{
    private NavMeshAgent agent;
    private Rigidbody rb;
    public LayerMask layerMask;

    [Header("Physics Settings")]
    [Tooltip("Enable this if you want NavMeshAgent to work with physics collisions")]
    public bool enablePhysicsIntegration = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // Validate components
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on " + gameObject.name + ". Physics collision detection will not work properly.");
        }
        else
        {
            // Configure Rigidbody for proper physics interaction
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (enablePhysicsIntegration && agent != null)
            {

                rb.isKinematic = true; 

                Debug.Log("NavMeshAgent configured to work with Rigidbody physics");
            }
        }

        // Validate collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("Collider component not found on " + gameObject.name + ". Add a Box/Capsule/Sphere Collider for collision detection.");
        }
        else if (col.isTrigger)
        {
            Debug.LogWarning("Collider is set as Trigger on " + gameObject.name + ". Physics collisions will not work. Uncheck 'Is Trigger' if you want solid collisions.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                Debug.Log("Click registered at: " + hit.point);

                // Get current transform data for Kafka message
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

                // Move using NavMeshAgent (this already respects physics when properly configured)
                if (agent != null)
                {
                    agent.SetDestination(hit.point);
                    Debug.Log($"NavMeshAgent moving {gameObject.name} to {hit.point}");
                }
                else
                {
                    Debug.LogError("Cannot move: NavMeshAgent is null");
                }
            }
        }
    }

    // Manual physics integration for advanced users
    void FixedUpdate()
    {
        if (!enablePhysicsIntegration || agent == null || rb == null || rb.isKinematic)
            return;

        // Manually apply NavMeshAgent's desired velocity to Rigidbod

        if (agent.hasPath)
        {
            Vector3 desiredVelocity = agent.desiredVelocity;

            // Apply the desired velocity as force or directly set velocity
            rb.velocity = new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z);

            // Keep NavMeshAgent in sync (important!)
            agent.nextPosition = transform.position;
        }
    }

    [ContextMenu("Validate Physics Setup")]
    public void ValidatePhysicsSetup()
    {
        Debug.Log("=== Physics Setup Validation ===");

        // Check NavMeshAgent
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent missing");
        }
        else
        {
            Debug.Log($"NavMeshAgent found - Speed: {agent.speed}, Angular Speed: {agent.angularSpeed}");
        }

        // Check Rigidbody
        if (rb == null)
        {
            Debug.LogError("Rigidbody missing - Physics collisions won't work!");
        }
        else
        {
            Debug.Log($"Rigidbody found - IsKinematic: {rb.isKinematic}, CollisionDetection: {rb.collisionDetectionMode}");

            if (rb.isKinematic && enablePhysicsIntegration)
            {
                Debug.Log("Rigidbody is kinematic - NavMeshAgent controls movement, physics handles collisions");
            }
            else if (!rb.isKinematic && enablePhysicsIntegration)
            {
                Debug.Log(" Rigidbody is non-kinematic - Using manual physics integration in FixedUpdate");
            }
        }

        // Check Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("Collider missing - No collision detection possible!");
        }
        else
        {
            Debug.Log($"Collider found - Type: {col.GetType().Name}, IsTrigger: {col.isTrigger}");

            if (col.isTrigger)
            {
                Debug.LogWarning("Collider is set as Trigger - Won't block other objects!");
            }
        }

        Debug.Log("=== End Validation ===");
    }
}