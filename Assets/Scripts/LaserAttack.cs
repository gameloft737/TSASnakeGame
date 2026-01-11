
using UnityEngine;

public class LaserAttack : Attack
{
    [Header("Laser References")]
    [SerializeField] private LineRenderer leftLineRenderer;
    [SerializeField] private LineRenderer rightLineRenderer;
    [SerializeField] private ParticleSystem leftParticles;
    [SerializeField] private ParticleSystem rightParticles;
    
    [Header("Evolution Side Laser References (Left Eye)")]
    [SerializeField] private LineRenderer leftSideLineRenderer;      // Left eye shoots left (90° CCW)
    [SerializeField] private LineRenderer leftBackLineRenderer;      // Left eye shoots back-left (135° CCW)
    [SerializeField] private ParticleSystem leftSideParticles;
    [SerializeField] private ParticleSystem leftBackParticles;
    
    [Header("Evolution Side Laser References (Right Eye)")]
    [SerializeField] private LineRenderer rightSideLineRenderer;     // Right eye shoots right (90° CW)
    [SerializeField] private LineRenderer rightBackLineRenderer;     // Right eye shoots back-right (135° CW)
    [SerializeField] private ParticleSystem rightSideParticles;
    [SerializeField] private ParticleSystem rightBackParticles;

    [Header("Laser Settings (Base - overridden by upgrade data if assigned)")]
    [SerializeField] private float laserDistance = 8f;
    [SerializeField] private float laserWidth = 0.1f;
    [SerializeField] private LayerMask ignoreMask;
    [SerializeField] private Transform aimReference; // The object that aims forward (e.g., head)
    
    [Header("Evolution Settings")]
    [SerializeField] private bool sideLasersEnabled = false; // Enabled when evolved
    
    // Custom stat names for upgrade data
    private const string STAT_LASER_DISTANCE = "laserDistance";
    private const string STAT_LASER_WIDTH = "laserWidth";
    private const string STAT_SIDE_LASERS_ENABLED = "sideLasersEnabled";

    // Main laser targets
    private AppleEnemy leftTarget;
    private AppleEnemy rightTarget;
    private bool isDamagingLeft = false;
    private bool isDamagingRight = false;
    
    // Side laser targets (left eye)
    private AppleEnemy leftSideTarget;
    private AppleEnemy leftBackTarget;
    private bool isDamagingLeftSide = false;
    private bool isDamagingLeftBack = false;
    
    // Side laser targets (right eye)
    private AppleEnemy rightSideTarget;
    private AppleEnemy rightBackTarget;
    private bool isDamagingRightSide = false;
    private bool isDamagingRightBack = false;
    
    private Vector3 convergencePoint;
    private float targetXRotation; // Current X rotation from mouse input

    private void Awake()
    {
        attackType = AttackType.Continuous;
        
        // Setup main lasers
        SetupLineRenderer(leftLineRenderer);
        SetupLineRenderer(rightLineRenderer);
        
        // Setup evolution lasers
        SetupLineRenderer(leftSideLineRenderer);
        SetupLineRenderer(leftBackLineRenderer);
        SetupLineRenderer(rightSideLineRenderer);
        SetupLineRenderer(rightBackLineRenderer);

        // Stop main particles
        if (leftParticles != null) leftParticles.Stop();
        if (rightParticles != null) rightParticles.Stop();
        
        // Stop evolution particles
        if (leftSideParticles != null) leftSideParticles.Stop();
        if (leftBackParticles != null) leftBackParticles.Stop();
        if (rightSideParticles != null) rightSideParticles.Stop();
        if (rightBackParticles != null) rightBackParticles.Stop();
    }
    
    /// <summary>
    /// Apply custom stats from upgrade data
    /// </summary>
    protected override void ApplyCustomStats(AttackLevelStats stats)
    {
        // Apply laser-specific stats from upgrade data
        laserDistance = GetCustomStat(STAT_LASER_DISTANCE, laserDistance);
        laserWidth = GetCustomStat(STAT_LASER_WIDTH, laserWidth);
        
        // Check if side lasers should be enabled (evolution)
        float sideLasersValue = GetCustomStat(STAT_SIDE_LASERS_ENABLED, sideLasersEnabled ? 1f : 0f);
        sideLasersEnabled = sideLasersValue > 0f;
        
        // Update line renderer widths
        UpdateLineRendererWidth();
    }
    
    /// <summary>
    /// Called when the attack is upgraded
    /// </summary>
    protected override void OnUpgrade()
    {
        // Visual feedback for upgrade could go here
        Debug.Log($"Laser upgraded! Distance: {laserDistance}, Width: {laserWidth}, Side Lasers: {sideLasersEnabled}");
    }
    
    private void UpdateLineRendererWidth()
    {
        // Main lasers
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
        
        // Evolution lasers (left eye)
        if (leftSideLineRenderer != null)
        {
            leftSideLineRenderer.startWidth = laserWidth;
            leftSideLineRenderer.endWidth = laserWidth;
        }
        if (leftBackLineRenderer != null)
        {
            leftBackLineRenderer.startWidth = laserWidth;
            leftBackLineRenderer.endWidth = laserWidth;
        }
        
        // Evolution lasers (right eye)
        if (rightSideLineRenderer != null)
        {
            rightSideLineRenderer.startWidth = laserWidth;
            rightSideLineRenderer.endWidth = laserWidth;
        }
        if (rightBackLineRenderer != null)
        {
            rightBackLineRenderer.startWidth = laserWidth;
            rightBackLineRenderer.endWidth = laserWidth;
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
        // Enable main lasers
        if (leftLineRenderer != null) leftLineRenderer.enabled = true;
        if (rightLineRenderer != null) rightLineRenderer.enabled = true;
        if (leftParticles != null) leftParticles.Play();
        if (rightParticles != null) rightParticles.Play();
        
        // Enable evolution lasers if evolved
        if (sideLasersEnabled)
        {
            // Left eye evolution lasers
            if (leftSideLineRenderer != null) leftSideLineRenderer.enabled = true;
            if (leftBackLineRenderer != null) leftBackLineRenderer.enabled = true;
            if (leftSideParticles != null) leftSideParticles.Play();
            if (leftBackParticles != null) leftBackParticles.Play();
            
            // Right eye evolution lasers
            if (rightSideLineRenderer != null) rightSideLineRenderer.enabled = true;
            if (rightBackLineRenderer != null) rightBackLineRenderer.enabled = true;
            if (rightSideParticles != null) rightSideParticles.Play();
            if (rightBackParticles != null) rightBackParticles.Play();
        }
        
        // Play laser sound (looping)
        SoundManager.Play("Laser", gameObject);
    }

    protected override void OnHoldUpdate()
    {
        // Return to neutral (aim camera removed)
        targetXRotation = Mathf.Lerp(targetXRotation, 0f, 5f * Time.deltaTime);
        
        // Calculate the rotated direction from aimReference
        Vector3 rotatedDirection = aimReference.forward;
        if (aimReference != null)
        {
            // Apply rotation around the right axis (X-axis rotation)
            // Negate to invert: looking down makes laser go up
            Quaternion rotationOffset = Quaternion.AngleAxis(-targetXRotation, aimReference.right);
            rotatedDirection = rotationOffset * aimReference.forward;
        }
        
        // Get effective laser distance with range multiplier
        float effectiveLaserDistance = GetEffectiveLaserDistance();
        
        // Calculate convergence point using the rotated direction
        Ray forwardRay = new Ray(aimReference.position, rotatedDirection);
        RaycastHit hit;
        
        if (Physics.Raycast(forwardRay, out hit, effectiveLaserDistance, ~ignoreMask))
        {
            convergencePoint = hit.point;
        }
        else
        {
            convergencePoint = aimReference.position + rotatedDirection * effectiveLaserDistance;
        }
        
        // Update both main lasers to converge at that point
        UpdateLaser(leftLineRenderer, leftParticles, ref leftTarget, ref isDamagingLeft);
        UpdateLaser(rightLineRenderer, rightParticles, ref rightTarget, ref isDamagingRight);
        
        // Update evolution lasers if evolved
        if (sideLasersEnabled)
        {
            // Get the start positions from the left and right eye positions
            Vector3 leftEyePos = leftLineRenderer != null ? leftLineRenderer.transform.position : aimReference.position;
            Vector3 rightEyePos = rightLineRenderer != null ? rightLineRenderer.transform.position : aimReference.position;
            
            // Left eye: shoots left (90° CCW) and back-left (135° CCW from forward)
            Vector3 leftSideDirection = Quaternion.AngleAxis(-90f, aimReference.up) * rotatedDirection;
            Vector3 leftBackDirection = Quaternion.AngleAxis(-135f, aimReference.up) * rotatedDirection;
            UpdateSideLaser(leftSideLineRenderer, leftSideParticles, ref leftSideTarget, ref isDamagingLeftSide, leftEyePos, leftSideDirection);
            UpdateSideLaser(leftBackLineRenderer, leftBackParticles, ref leftBackTarget, ref isDamagingLeftBack, leftEyePos, leftBackDirection);
            
            // Right eye: shoots right (90° CW) and back-right (135° CW from forward)
            Vector3 rightSideDirection = Quaternion.AngleAxis(90f, aimReference.up) * rotatedDirection;
            Vector3 rightBackDirection = Quaternion.AngleAxis(135f, aimReference.up) * rotatedDirection;
            UpdateSideLaser(rightSideLineRenderer, rightSideParticles, ref rightSideTarget, ref isDamagingRightSide, rightEyePos, rightSideDirection);
            UpdateSideLaser(rightBackLineRenderer, rightBackParticles, ref rightBackTarget, ref isDamagingRightBack, rightEyePos, rightBackDirection);
        }
    }
    
    /// <summary>
    /// Gets the effective laser distance with range multiplier applied
    /// </summary>
    private float GetEffectiveLaserDistance()
    {
        float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.GetRangeMultiplier() : 1f;
        return laserDistance * multiplier;
    }

    protected override void OnDeactivate()
    {
        // Disable main lasers
        if (leftLineRenderer != null) leftLineRenderer.enabled = false;
        if (rightLineRenderer != null) rightLineRenderer.enabled = false;
        
        // Disable evolution lasers
        if (leftSideLineRenderer != null) leftSideLineRenderer.enabled = false;
        if (leftBackLineRenderer != null) leftBackLineRenderer.enabled = false;
        if (rightSideLineRenderer != null) rightSideLineRenderer.enabled = false;
        if (rightBackLineRenderer != null) rightBackLineRenderer.enabled = false;

        // Stop damaging for main lasers
        StopDamaging(ref leftTarget, ref isDamagingLeft);
        StopDamaging(ref rightTarget, ref isDamagingRight);
        
        // Stop damaging for evolution lasers
        StopDamaging(ref leftSideTarget, ref isDamagingLeftSide);
        StopDamaging(ref leftBackTarget, ref isDamagingLeftBack);
        StopDamaging(ref rightSideTarget, ref isDamagingRightSide);
        StopDamaging(ref rightBackTarget, ref isDamagingRightBack);
        
        // Stop main particles
        if (leftParticles != null) leftParticles.Stop();
        if (rightParticles != null) rightParticles.Stop();
        
        // Stop evolution particles
        if (leftSideParticles != null) leftSideParticles.Stop();
        if (leftBackParticles != null) leftBackParticles.Stop();
        if (rightSideParticles != null) rightSideParticles.Stop();
        if (rightBackParticles != null) rightBackParticles.Stop();
        
        // Fade out laser sound instead of abrupt stop
        SoundManager.FadeOut("Laser", gameObject, 0.3f);
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
                    currentTarget.TakeDamage(GetDamage());
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
    
    private void UpdateSideLaser(LineRenderer lineRenderer, ParticleSystem particles, ref AppleEnemy currentTarget, ref bool isDamaging, Vector3 startPoint, Vector3 direction)
    {
        if (lineRenderer == null) return;

        // Get effective laser distance with range multiplier
        float effectiveLaserDistance = GetEffectiveLaserDistance();
        
        // Calculate end point based on direction and laser distance
        Vector3 endPoint = startPoint + direction * effectiveLaserDistance;
        
        // Raycast along this laser's path
        Ray ray = new Ray(startPoint, direction);
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(ray, out hit, effectiveLaserDistance, ~ignoreMask);

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
                    currentTarget.TakeDamage(GetDamage());
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
    
    /// <summary>
    /// Enables or disables the side lasers (for evolution)
    /// </summary>
    public void SetSideLasersEnabled(bool enabled)
    {
        sideLasersEnabled = enabled;
    }
    
    /// <summary>
    /// Returns whether side lasers are currently enabled
    /// </summary>
    public bool AreSideLasersEnabled()
    {
        return sideLasersEnabled;
    }
}