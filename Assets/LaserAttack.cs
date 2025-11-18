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
    [SerializeField] private float laserDuration = 5f;
    
    
    [Header("References")]
    [SerializeField] private Transform leftLaserOrigin;
    [SerializeField] private Transform rightLaserOrigin;
    [SerializeField] private AttackManager attackManager;
    
    private bool isLaserActive = false;
    private float laserActiveTimer = 0f;
    
    private AppleEnemy leftTarget;
    private AppleEnemy rightTarget;
    private bool isDamagingLeft = false;
    private bool isDamagingRight = false;

    private void Awake()
    {
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
            lr.useWorldSpace = true;
        }
    }

    private void Update()
    {
        if (isLaserActive)
        {
            laserActiveTimer += Time.deltaTime;
            
            if (laserActiveTimer >= laserDuration)
            {
                DeactivateLaser();
            }
            else
            {
                UpdateLaser(leftLaserOrigin, leftLineRenderer, leftParticles, ref leftTarget, ref isDamagingLeft);
                UpdateLaser(rightLaserOrigin, rightLineRenderer, rightParticles, ref rightTarget, ref isDamagingRight);
            }
        }
    }

    protected override void Use()
    {
        if (leftLaserOrigin == null || rightLaserOrigin == null) return;

        ActivateLaser();
    }

    private void ActivateLaser()
    {
        isLaserActive = true;
        laserActiveTimer = 0f;
        
        if (leftLineRenderer != null) leftLineRenderer.enabled = true;
        if (rightLineRenderer != null) rightLineRenderer.enabled = true;
        
        Debug.Log("Lasers activated!");
    }

    private void DeactivateLaser()
    {
        isLaserActive = false;
        laserActiveTimer = 0f;
        
        if (leftLineRenderer != null) leftLineRenderer.enabled = false;
        if (rightLineRenderer != null) rightLineRenderer.enabled = false;
        
        StopDamaging(ref leftTarget, ref isDamagingLeft, leftParticles);
        StopDamaging(ref rightTarget, ref isDamagingRight, rightParticles);
        
        Debug.Log("Lasers deactivated!");
    }

    private void UpdateLaser(Transform origin, LineRenderer lineRenderer, ParticleSystem particles, ref AppleEnemy currentTarget, ref bool isDamaging)
    {
        if (origin == null || lineRenderer == null) return;
        
        Vector3 startPos = origin.position;
        Vector3 direction = origin.forward;
        Ray ray = new Ray(startPos, direction);
        
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(ray, out hit, laserDistance, ~ignoreMask);
        
        Vector3 endPos;
        
        if (hitSomething)
        {
            endPos = hit.point;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
            
            AppleEnemy hitApple = hit.collider.GetComponentInParent<AppleEnemy>();
            
            if (hitApple != null)
            {
                if (currentTarget != hitApple)
                {
                    StopDamaging(ref currentTarget, ref isDamaging, particles);
                    currentTarget = hitApple;
                    StartDamaging(ref isDamaging, particles);
                }
                
                if (isDamaging && currentTarget != null)
                {
                    currentTarget.TakeDamage(damage * Time.deltaTime);
                }
            }
            else
            {
                StopDamaging(ref currentTarget, ref isDamaging, particles);
            }
        }
        else
        {
            endPos = startPos + direction * laserDistance;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
            
            StopDamaging(ref currentTarget, ref isDamaging, particles);
        }
        
        // Always position particles at laser endpoint
        if (particles != null)
        {
            particles.transform.position = endPos;
        }
    }

    private void StartDamaging(ref bool isDamaging, ParticleSystem particles)
    {
        isDamaging = true;
        
        if (particles != null)
        {
            particles.Play();
        }
    }

    private void StopDamaging(ref AppleEnemy target, ref bool isDamaging, ParticleSystem particles)
    {
        if (isDamaging)
        {
            isDamaging = false;
            target = null;
            
            if (particles != null)
            {
                particles.Stop();
            }
        }
    }

    private void OnDisable()
    {
        DeactivateLaser();
    }
}