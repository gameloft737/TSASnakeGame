using UnityEngine;

public class LaserAttack : Attack
{
    [Header("Laser References")]
    [SerializeField] private LineRenderer leftLineRenderer;
    [SerializeField] private LineRenderer rightLineRenderer;
    [SerializeField] private ParticleSystem leftParticles;
    [SerializeField] private ParticleSystem rightParticles;

    [Header("Laser Settings")]
    [SerializeField] private float laserDistance = 8f;
    [SerializeField] private float laserWidth = 0.1f;
    [SerializeField] private LayerMask ignoreMask;
    [SerializeField] private Transform aimReference; // The object that aims forward (e.g., head)

    private AppleEnemy leftTarget;
    private AppleEnemy rightTarget;
    private bool isDamagingLeft = false;
    private bool isDamagingRight = false;
    private Vector3 convergencePoint;

    private void Awake()
    {
        attackType = AttackType.Continuous;
        
        SetupLineRenderer(leftLineRenderer);
        SetupLineRenderer(rightLineRenderer);

        if (leftParticles != null) leftParticles.Stop();
        if (rightParticles != null) rightParticles.Stop();
    }

    private void SetupLineRenderer(LineRenderer lr)
    {
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.enabled = false;
            lr.startWidth = laserWidth;
            lr.endWidth = laserWidth;
            lr.useWorldSpace = true; // Changed to world space for easier calculations
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
        // Calculate convergence point first (straight ahead from aim reference)
        if (aimReference != null)
        {
            Ray forwardRay = new Ray(aimReference.position, aimReference.forward);
            RaycastHit hit;
            
            if (Physics.Raycast(forwardRay, out hit, laserDistance, ~ignoreMask))
            {
                convergencePoint = hit.point;
            }
            else
            {
                convergencePoint = aimReference.position + aimReference.forward * laserDistance;
            }
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