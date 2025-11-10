using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AppleEnemy : MonoBehaviour
{
    private NavMeshAgent agent;

    [SerializeField] private SnakeBody snakeBody; // Reference to the snake
    [SerializeField] private float searchRadius = 50f; // Radius to search for snake initially
    [SerializeField] private float contactDuration = 5f; // Duration to stay in contact before destroying
    [SerializeField] private float contactDistance = 1.5f; // Distance to count as "touching" a body part
    [SerializeField] private Transform agentObj; // Visual object to flip

    private Transform nearestBodyPart;
    private float contactTimer = 0f;
    private bool isInContact = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        
        StartCoroutine(TrackAndMonitorContact());
    }

    private Transform FindNearestBodyPart()
    {
        if (snakeBody == null || snakeBody.bodyParts == null || snakeBody.bodyParts.Count == 0)
            return null;

        float nearestDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (var bodyPart in snakeBody.bodyParts)
        {
            if (bodyPart != null)
            {
                float distance = Vector3.Distance(transform.position, bodyPart.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = bodyPart.transform;
                }
            }
        }

        return nearest;
    }

    private bool IsAnyBodyPartNearby(out Transform closestPart)
    {
        closestPart = null;
        
        if (snakeBody == null || snakeBody.bodyParts == null || snakeBody.bodyParts.Count == 0)
            return false;

        float closestDistance = Mathf.Infinity;

        foreach (var bodyPart in snakeBody.bodyParts)
        {
            if (bodyPart != null)
            {
                float distance = Vector3.Distance(transform.position, bodyPart.transform.position);
                if (distance <= contactDistance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPart = bodyPart.transform;
                }
            }
        }

        return closestPart != null;
    }

    private IEnumerator TrackAndMonitorContact()
    {
        // Find initial target
        nearestBodyPart = FindNearestBodyPart();

        while (true)
        {
            if (nearestBodyPart != null)
            {
                // Check if ANY body part is nearby (not just the target we're moving toward)
                Transform contactedPart;
                isInContact = IsAnyBodyPartNearby(out contactedPart);

                if (isInContact)
                {
                    // Stop moving when in contact
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(transform.position);
                    }

                    // Increment contact timer
                    contactTimer += Time.deltaTime;

                    // Destroy after sustained contact
                    if (contactTimer >= contactDuration)
                    {
                        Destroy(gameObject);
                        yield break;
                    }
                }
                else
                {
                    // Reset timer if not in contact
                    contactTimer = 0f;

                    // Re-find nearest body part occasionally for better tracking
                    nearestBodyPart = FindNearestBodyPart();

                    // Move toward the nearest body part
                    if (nearestBodyPart != null && agent.isOnNavMesh)
                    {
                        agent.SetDestination(nearestBodyPart.position);
                    }
                }

                // Flip the GameObject based on movement direction
                if (agentObj != null && agent.velocity.x != 0)
                {
                    Vector3 rotation = agentObj.transform.eulerAngles;
                    rotation.y = agent.velocity.x > 0 ? 0 : 180;
                    agentObj.transform.eulerAngles = rotation;
                }
            }
            else
            {
                // No body parts found, try searching again
                nearestBodyPart = FindNearestBodyPart();
            }

            yield return null; // Wait for the next frame
        }
    }

    // Optional: Visualize the contact distance in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isInContact ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, contactDistance);
        
        if (nearestBodyPart != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, nearestBodyPart.position);
        }
    }
}