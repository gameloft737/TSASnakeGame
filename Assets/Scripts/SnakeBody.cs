using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using System;

public class SnakeBody : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private GameObject bodyPartPrefab;
    [SerializeField] private int bodyLength = 5;
    [SerializeField] private float segmentSpacing = 0.5f;
    [SerializeField] private float minRecordDistance = 0.05f;

    [SerializeField] private float impulseCarryover = 0.9f;
    [SerializeField] private float rotationBlendAmount = 0.3f;
    
    // Material system
    [SerializeField] private Renderer headRenderer;
    private GameObject currentAttachment;
    
    public List<BodyPart> bodyParts = new List<BodyPart>();
    private List<PositionData> positionHistory = new List<PositionData>();
    private Vector3 lastRecordedPosition;
    private bool isHeadMoving = false;
    
    private struct PositionData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float distanceFromStart;
    }
    
    public static event Action OnBodyPartsInitialized;
    
    void Start()
    {
        lastRecordedPosition = head.position;
        
        // Get head renderer if not assigned
        if (headRenderer == null)
        {
            headRenderer = head.GetComponent<Renderer>();
            if (headRenderer == null)
            {
                headRenderer = head.GetComponentInChildren<Renderer>();
            }
        }
        
        positionHistory.Add(new PositionData 
        { 
            position = head.position, 
            rotation = head.rotation,
            distanceFromStart = 0f
        });

        for (int i = 0; i < bodyLength; i++)
        {
            Vector3 startPos = head.position - head.forward * segmentSpacing * (i + 1);
            GameObject part = Instantiate(bodyPartPrefab, startPos, head.rotation);
            BodyPart bodyPartComponent = part.GetComponent<BodyPart>();
            bodyParts.Add(bodyPartComponent);
            
            if (i < 3)
            {
                float[] tailScales = { 0.5f, 0.7f, 0.8f};
                Vector3 scale = part.transform.localScale;
                scale.x = tailScales[i];
                part.transform.localScale = scale;
            }
            
            // Capture the final scale after all modifications
            bodyPartComponent.CaptureBaseScale();
        }
        
        OnBodyPartsInitialized?.Invoke();
    }

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

        for (int i = 0; i < bodyParts.Count; i++)
        {
            int reverseIndex = bodyParts.Count - 1 - i;
            float targetDistance = (reverseIndex + 1) * segmentSpacing;
            var targetData = GetPositionAtDistance(targetDistance);
            
            Quaternion historicalRotation = targetData.rotation;
            Quaternion lookAtRotation;
            Vector3 lookTarget;
            
            if (i == bodyParts.Count - 1)
            {
                lookTarget = head.position;
            }
            else
            {
                lookTarget = bodyParts[i + 1].transform.position;
            }
            
            Vector3 directionToNext = (lookTarget - bodyParts[i].transform.position).normalized;
            
            if (directionToNext != Vector3.zero)
            {
                lookAtRotation = Quaternion.LookRotation(directionToNext);
            }
            else
            {
                lookAtRotation = historicalRotation;
            }
            
            Quaternion finalRotation = Quaternion.Slerp(historicalRotation, lookAtRotation, rotationBlendAmount);
            
            bodyParts[i].FollowTarget(targetData.position, finalRotation, isHeadMoving);
        }

        float maxDistance = bodyLength * segmentSpacing + segmentSpacing * 2;
        CleanupHistory(maxDistance);
    }
    
    public void ApplyForceToBody(Vector3 direction, float force)
    {
        Vector3 lungeDirection = direction;
        
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null && pm.IsGrounded())
        {
            lungeDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        }
        
        foreach (BodyPart part in bodyParts)
        {
            Rigidbody partRb = part.GetComponent<Rigidbody>();
            if (partRb != null)
            {
                partRb.AddForce(lungeDirection * force * impulseCarryover, ForceMode.Impulse);
            }
        }
    }
    
    public void IncreaseSize(int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = head.position - head.forward * segmentSpacing;
            Quaternion spawnRot = head.rotation;
            
            GameObject newPart = Instantiate(bodyPartPrefab, spawnPos, spawnRot);
            BodyPart bodyPartComponent = newPart.GetComponent<BodyPart>();
            
            // Capture scale before adding to list
            bodyPartComponent.CaptureBaseScale();
            
            bodyParts.Add(bodyPartComponent);
            bodyLength++;
        }
    }
    
    public int GetBodyLength()
    {
        return bodyLength;
    }

    public void ApplyAttackVariation(Material headMaterial, Material bodyMaterial, GameObject attachmentObject)
    {
        if (headRenderer != null && headMaterial != null)
        {
            headRenderer.material = headMaterial;
        }
        
        if (bodyMaterial != null)
        {
            foreach (BodyPart part in bodyParts)
            {
                part.SetMaterial(bodyMaterial);
            }
        }
        
        if (currentAttachment != null)
        {
            currentAttachment.SetActive(false);
            currentAttachment = null;
        }
        
        if (attachmentObject != null)
        {
            currentAttachment = attachmentObject;
            currentAttachment.SetActive(true);
        }
    }
    
    public void ClearAttachment()
    {
        if (currentAttachment != null)
        {
            currentAttachment.SetActive(false);
            currentAttachment = null;
        }
    }
    
    public void TriggerSwallowAnimation(float bulgeScale = 1.3f, float bulgeSpeed = 0.08f)
    {
        StartCoroutine(SwallowAnimationCoroutine(bulgeScale, bulgeSpeed));
    }

    private IEnumerator SwallowAnimationCoroutine(float bulgeScale, float bulgeSpeed)
    {
        for (int i = bodyParts.Count - 1; i >= 0; i--)
        {
            bodyParts[i].AnimateBulge(bulgeScale, 0.2f);
            yield return new WaitForSeconds(bulgeSpeed);
        }
    }

    private PositionData GetPositionAtDistance(float targetDistance)
    {
        if (positionHistory.Count == 0)
            return new PositionData { position = head.position, rotation = head.rotation, distanceFromStart = 0 };

        float currentTotalDistance = positionHistory[0].distanceFromStart;
        float distanceFromHead = currentTotalDistance - targetDistance;

        if (distanceFromHead < 0)
            return positionHistory[positionHistory.Count - 1];

        for (int i = 0; i < positionHistory.Count - 1; i++)
        {
            float dist1 = positionHistory[i].distanceFromStart;
            float dist2 = positionHistory[i + 1].distanceFromStart;

            if (distanceFromHead >= dist2 && distanceFromHead <= dist1)
            {
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

        return positionHistory[0];
    }

    private void CleanupHistory(float maxDistance)
    {
        if (positionHistory.Count < 2) return;

        float currentDistance = positionHistory[0].distanceFromStart;
        
        for (int i = positionHistory.Count - 1; i >= 0; i--)
        {
            if (currentDistance - positionHistory[i].distanceFromStart > maxDistance)
            {
                positionHistory.RemoveAt(i);
            }
        }
    }

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