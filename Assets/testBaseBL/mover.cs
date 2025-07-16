using UnityEngine;
using UnityEngine.AI;

public class mover : MonoBehaviour
{
    private NavMeshAgent agent;
    public LayerMask layerMask;  
    public logs coordsLogger;  // Reference to the logs script

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                Debug.Log("Click registered at: " + hit.point); // Check where the click happened
                Vector3 objectPosition = transform.position;

                // Call SaveCoords from the logs script
                coordsLogger.SaveCoords(objectPosition.x, objectPosition.y, objectPosition.z);

                Debug.Log("Object's position: " + objectPosition);
                agent.SetDestination(hit.point);
            }
        }
    }
}
