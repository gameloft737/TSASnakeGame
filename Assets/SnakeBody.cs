using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SnakeBody : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private GameObject bodyPartPrefab;
    [SerializeField] private int bodyLength = 5;
    [SerializeField] private float segmentSpacing = 0.5f;
    [SerializeField] private float minRecordDistance = 0.05f; // Only record when head moves this far

    private List<BodyPart> bodyParts = new List<BodyPart>();
    private List<PositionData> positionHistory = new List<PositionData>();
    private Vector3 lastRecordedPosition;
    private bool isHeadMoving = false;
    
    private struct PositionData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float distanceFromStart; // Cumulative distance traveled
    }

    void Start()
    {
        lastRecordedPosition = head.position;
        
        // Initialize history with starting position
        positionHistory.Add(new PositionData 
        { 
            position = head.position, 
            rotation = head.rotation,
            distanceFromStart = 0f
        });

        // Create body segments at correct starting positions
        for (int i = 0; i < bodyLength; i++)
        {
            Vector3 startPos = head.position - head.forward * segmentSpacing * (i + 1);
            GameObject part = Instantiate(bodyPartPrefab, startPos, head.rotation);
            bodyParts.Add(part.GetComponent<BodyPart>());
        }
    }

    // Input System callback method
    public void OnAdd(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IncreaseSize(1);
            Debug.Log($"Added segment! Total body length: {bodyLength}");
        }
    }

    void FixedUpdate()
    {
        // Only record new position if head has moved significantly
        float distanceMoved = Vector3.Distance(head.position, lastRecordedPosition);
        isHeadMoving = distanceMoved >= minRecordDistance;
        
        if (isHeadMoving)
        {
            float totalDistance = positionHistory.Count > 0 
                ? positionHistory[0].distanceFromStart + distanceMoved 
                : distanceMoved;
            
            positionHistory.Insert(0, new PositionData 
            { 
                position = head.position, 
                rotation = head.rotation,
                distanceFromStart = totalDistance
            });
            
            lastRecordedPosition = head.position;
        }

        // Update each body part based on DISTANCE, not time
        // REVERSED: Count from the end so newest parts (end of list) follow closest to head
        for (int i = 0; i < bodyParts.Count; i++)
        {
            int reverseIndex = bodyParts.Count - 1 - i;
            float targetDistance = (reverseIndex + 1) * segmentSpacing;
            var targetData = GetPositionAtDistance(targetDistance);
            bodyParts[i].FollowTarget(targetData.position, targetData.rotation, isHeadMoving);
        }

        // Clean up old history (keep extra buffer for safety)
        float maxDistance = bodyLength * segmentSpacing + segmentSpacing * 2;
        CleanupHistory(maxDistance);
    }
    
    public void IncreaseSize(int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            // Spawn new segment right behind the head
            Vector3 spawnPos = head.position - head.forward * segmentSpacing;
            Quaternion spawnRot = head.rotation;
            
            GameObject newPart = Instantiate(bodyPartPrefab, spawnPos, spawnRot);
            BodyPart bodyPartComponent = newPart.GetComponent<BodyPart>();
            
            // Add to the END of the list (newest parts at end)
            bodyParts.Add(bodyPartComponent);
            bodyLength++;
        }
    }
    
    public int GetBodyLength()
    {
        return bodyLength;
    }

    private PositionData GetPositionAtDistance(float targetDistance)
    {
        if (positionHistory.Count == 0)
            return new PositionData { position = head.position, rotation = head.rotation, distanceFromStart = 0 };

        float currentTotalDistance = positionHistory[0].distanceFromStart;
        float distanceFromHead = currentTotalDistance - targetDistance;

        // If target is ahead of all history, return the oldest position
        if (distanceFromHead < 0)
            return positionHistory[positionHistory.Count - 1];

        // Find the two history points that surround our target distance
        for (int i = 0; i < positionHistory.Count - 1; i++)
        {
            float dist1 = positionHistory[i].distanceFromStart;
            float dist2 = positionHistory[i + 1].distanceFromStart;

            if (distanceFromHead >= dist2 && distanceFromHead <= dist1)
            {
                // Interpolate between these two points
                float segmentLength = dist1 - dist2;
                float t = segmentLength > 0 ? (distanceFromHead - dist2) / segmentLength : 0;

                return new PositionData
                {
                    position = Vector3.Lerp(positionHistory[i + 1].position, positionHistory[i].position, t),
                    rotation = Quaternion.Slerp(positionHistory[i + 1].rotation, positionHistory[i].rotation, t),
                    distanceFromStart = distanceFromHead
                };
            }
        }

        // Fallback to most recent position
        return positionHistory[0];
    }

    private void CleanupHistory(float maxDistance)
    {
        if (positionHistory.Count < 2) return;

        float currentDistance = positionHistory[0].distanceFromStart;
        
        // Remove positions that are too far back
        for (int i = positionHistory.Count - 1; i >= 0; i--)
        {
            if (currentDistance - positionHistory[i].distanceFromStart > maxDistance)
            {
                positionHistory.RemoveAt(i);
            }
        }
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (positionHistory == null || positionHistory.Count < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < positionHistory.Count - 1; i++)
        {
            Gizmos.DrawLine(positionHistory[i].position, positionHistory[i + 1].position);
        }
    }
}