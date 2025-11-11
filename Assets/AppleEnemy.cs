using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AppleEnemy : MonoBehaviour
{
    private NavMeshAgent agent;

    [SerializeField] private SnakeBody snakeBody; // Reference to the snake
    [SerializeField] private float contactDuration = 5f; // Duration to stay in contact before destroying
    [SerializeField] private float contactDistance = 1.5f; // Distance to count as "touching" a body part
    [SerializeField] private Transform agentObj; // Visual object to flip
    [SerializeField] private AppleChecker appleChecker; // Reference to checker component

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
                // Check if the apple checker is touching snake
                bool touchingSnake = appleChecker != null && appleChecker.isTouching;

                // Check if ANY body part is nearby (not just the target we're moving toward)
                Transform contactedPart;
                isInContact = IsAnyBodyPartNearby(out contactedPart) || touchingSnake;

                if (isInContact)
                {
                    // Stop moving when in contact
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(transform.position);
                        agent.velocity = Vector3.zero;
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

                // Face the direction of movement (only if moving)
                if (agentObj != null && agent.velocity.sqrMagnitude > 0.01f)
                {
                    // Calculate the angle based on the direction of movement
                    float angle = Mathf.Atan2(agent.velocity.z, agent.velocity.x) * Mathf.Rad2Deg;
                    agentObj.transform.rotation = Quaternion.Euler(0, -angle + 90, 0);
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