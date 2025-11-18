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
            lr.useWorldSpace = false; // now local to origin
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
        leftParticles.Play();
        rightParticles.Play();
    }

    private void DeactivateLaser()
    {
        isLaserActive = false;
        laserActiveTimer = 0f;

        if (leftLineRenderer != null) leftLineRenderer.enabled = false;
        if (rightLineRenderer != null) rightLineRenderer.enabled = false;

        StopDamaging(ref leftTarget, ref isDamagingLeft);
        StopDamaging(ref rightTarget, ref isDamagingRight);
        leftParticles.Stop();
        rightParticles.Stop();
        Debug.Log("Lasers deactivated!");
    }

    private void UpdateLaser(Transform origin, LineRenderer lineRenderer, ParticleSystem particles, ref AppleEnemy currentTarget, ref bool isDamaging)
    {
        if (origin == null || lineRenderer == null) return;

        Vector3 localStart = Vector3.zero; // start at origin
        Vector3 localEnd = Vector3.forward * laserDistance; // default end in local space

        // Raycast in world space
        Ray ray = new Ray(origin.position, origin.forward);
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(ray, out hit, laserDistance, ~ignoreMask);

        if (hitSomething)
        {
            Vector3 worldEnd = hit.point;
            // Convert world hit point to local space relative to origin
            localEnd = origin.InverseTransformPoint(worldEnd);

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

        // Set LineRenderer in local space
        lineRenderer.SetPosition(0, localStart);
        lineRenderer.SetPosition(1, localEnd);

        // Move particles to laser endpoint in world space
        if (particles != null)
        {
            particles.transform.position = origin.TransformPoint(localEnd);
            
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
        DeactivateLaser();
    }
}
