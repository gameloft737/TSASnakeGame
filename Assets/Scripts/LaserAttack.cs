using UnityEngine;

public class LaserAttack : Attack
{
    [Header("Laser References")]
    [SerializeField] private LineRenderer leftLineRenderer;
    [SerializeField] private LineRenderer rightLineRenderer;
    [SerializeField] private ParticleSystem leftParticles;
    [SerializeField] private ParticleSystem rightParticles;

    [Header("Laser Settings (Base - overridden by upgrade data if assigned)")]
    [SerializeField] private float laserDistance = 8f;
    [SerializeField] private float laserWidth = 0.1f;
    [SerializeField] private LayerMask ignoreMask;
    [SerializeField] private Transform aimReference; // The object that aims forward (e.g., head)
    [SerializeField] private float mouseSensitivity = 0.5f; // How much mouse movement affects rotation
    [SerializeField] private float minXRotation = -5f; // Minimum X rotation
    [SerializeField] private float maxXRotation = 5f; // Maximum X rotation
    
    [Header("Mouse Look Reference")]
    [SerializeField] private MouseLookAt mouseLookAt;
    [SerializeField] private CameraManager cameraManager;
    
    // Custom stat names for upgrade data
    private const string STAT_LASER_DISTANCE = "laserDistance";
    private const string STAT_LASER_WIDTH = "laserWidth";

    private AppleEnemy leftTarget;
    private AppleEnemy rightTarget;
    private bool isDamagingLeft = false;
    private bool isDamagingRight = false;
    private Vector3 convergencePoint;
    private float targetXRotation; // Current X rotation from mouse input

    private void Awake()
    {
        attackType = AttackType.Continuous;
        
        SetupLineRenderer(leftLineRenderer);
        SetupLineRenderer(rightLineRenderer);

        if (leftParticles != null) leftParticles.Stop();
        if (rightParticles != null) rightParticles.Stop();
    }
    
    /// <summary>
    /// Apply custom stats from upgrade data
    /// </summary>
    protected override void ApplyCustomStats(AttackLevelStats stats)
    {
        // Apply laser-specific stats from upgrade data
        laserDistance = GetCustomStat(STAT_LASER_DISTANCE, laserDistance);
        laserWidth = GetCustomStat(STAT_LASER_WIDTH, laserWidth);
        
        // Update line renderer widths
        UpdateLineRendererWidth();
    }
    
    /// <summary>
    /// Called when the attack is upgraded
    /// </summary>
    protected override void OnUpgrade()
    {
        // Visual feedback for upgrade could go here
        Debug.Log($"Laser upgraded! Distance: {laserDistance}, Width: {laserWidth}");
    }
    
    private void UpdateLineRendererWidth()
    {
        if (leftLineRenderer != null)
        {
            leftLineRenderer.startWidth = laserWidth;
            leftLineRenderer.endWidth = laserWidth;
        }
        if (rightLineRenderer != null)
        {
            rightLineRenderer.startWidth = laserWidth;
            rightLineRenderer.endWidth = laserWidth;
        }
    }

    private void SetupLineRenderer(LineRenderer lr)
    {
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.enabled = false;
            lr.startWidth = laserWidth;
            lr.endWidth = laserWidth;
            lr.useWorldSpace = true;
        }
    }

    protected override void OnActivate()
    {
        if (leftLineRenderer != null) leftLineRenderer.enabled = true;
        if (rightLineRenderer != null) rightLineRenderer.enabled = true;
        if (leftParticles != null) leftParticles.Play();
        if (rightParticles != null) rightParticles.Play();
    }

    protected override void OnHoldUpdate()
    {
        // Update X rotation based on mouse input when aiming
        if (cameraManager != null && cameraManager.IsAiming() && mouseLookAt != null)
        {
            Vector2 smoothedInput = mouseLookAt.GetSmoothedLookInput();
            // Negate the Y input to invert control
            targetXRotation -= smoothedInput.y * mouseSensitivity;
            targetXRotation = Mathf.Clamp(targetXRotation, minXRotation, maxXRotation);
        }
        else
        {
            // Return to neutral when not aiming
            targetXRotation = Mathf.Lerp(targetXRotation, 0f, 5f * Time.deltaTime);
        }
        
        // Calculate the rotated direction from aimReference
        Vector3 rotatedDirection = aimReference.forward;
        if (aimReference != null)
        {
            // Apply rotation around the right axis (X-axis rotation)
            // Negate to invert: looking down makes laser go up
            Quaternion rotationOffset = Quaternion.AngleAxis(-targetXRotation, aimReference.right);
            rotatedDirection = rotationOffset * aimReference.forward;
        }
        
        // Calculate convergence point using the rotated direction
        Ray forwardRay = new Ray(aimReference.position, rotatedDirection);
        RaycastHit hit;
        
        if (Physics.Raycast(forwardRay, out hit, laserDistance, ~ignoreMask))
        {
            convergencePoint = hit.point;
        }
        else
        {
            convergencePoint = aimReference.position + rotatedDirection * laserDistance;
        }
        
        // Update both lasers to converge at that point
        UpdateLaser(leftLineRenderer, leftParticles, ref leftTarget, ref isDamagingLeft);
        UpdateLaser(rightLineRenderer, rightParticles, ref rightTarget, ref isDamagingRight);
    }

    protected override void OnDeactivate()
    {
        if (leftLineRenderer != null) leftLineRenderer.enabled = false;
        if (rightLineRenderer != null) rightLineRenderer.enabled = false;

        StopDamaging(ref leftTarget, ref isDamagingLeft);
        StopDamaging(ref rightTarget, ref isDamagingRight);
        
        if (leftParticles != null) leftParticles.Stop();
        if (rightParticles != null) rightParticles.Stop();
    }

    private void UpdateLaser(LineRenderer lineRenderer, ParticleSystem particles, ref AppleEnemy currentTarget, ref bool isDamaging)
    {
        if (lineRenderer == null) return;

        // Start point is the line renderer's position
        Vector3 startPoint = lineRenderer.transform.position;
        
        // End point is the convergence point
        Vector3 endPoint = convergencePoint;
        
        // Calculate direction from this laser to convergence point
        Vector3 direction = (endPoint - startPoint).normalized;
        float maxDistance = Vector3.Distance(startPoint, endPoint);
        
        // Raycast along this laser's path to convergence point
        Ray ray = new Ray(startPoint, direction);
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(ray, out hit, maxDistance, ~ignoreMask);

        if (hitSomething)
        {
            endPoint = hit.point;

            AppleEnemy hitApple = hit.collider.GetComponentInParent<AppleEnemy>();

            if (hitApple != null)
            {
                if (currentTarget != hitApple)
                {
                    StopDamaging(ref currentTarget, ref isDamaging);
                    currentTarget = hitApple;
                    StartDamaging(ref isDamaging);
                }

                if (isDamaging && currentTarget != null)
                {
                    currentTarget.TakeDamage(damage * Time.deltaTime);
                }
            }
            else
            {
                StopDamaging(ref currentTarget, ref isDamaging);
            }
        }
        else
        {
            StopDamaging(ref currentTarget, ref isDamaging);
        }

        // Set line renderer positions
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        // Position particles at end point
        if (particles != null)
        {
            particles.transform.position = endPoint;
        }
    }

    private void StartDamaging(ref bool isDamaging)
    {
        isDamaging = true;
    }

    private void StopDamaging(ref AppleEnemy target, ref bool isDamaging)
    {
        if (isDamaging)
        {
            isDamaging = false;
            target = null;
        }
    }

    private void OnDisable()
    {
        if (isActive)
        {
            StopUsing();
        }
    }
}