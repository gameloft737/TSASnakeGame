
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

    [Header("References")]
    [SerializeField] private Transform leftLaserOrigin;
    [SerializeField] private Transform rightLaserOrigin;

    private AppleEnemy leftTarget;
    private AppleEnemy rightTarget;
    private bool isDamagingLeft = false;
    private bool isDamagingRight = false;

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
            lr.useWorldSpace = false;
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
        UpdateLaser(leftLaserOrigin, leftLineRenderer, leftParticles, ref leftTarget, ref isDamagingLeft);
        UpdateLaser(rightLaserOrigin, rightLineRenderer, rightParticles, ref rightTarget, ref isDamagingRight);
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

    private void UpdateLaser(Transform origin, LineRenderer lineRenderer, ParticleSystem particles, ref AppleEnemy currentTarget, ref bool isDamaging)
    {
        if (origin == null || lineRenderer == null) return;

        Vector3 localStart = Vector3.zero;
        Vector3 localEnd = Vector3.forward * laserDistance;

        Ray ray = new Ray(origin.position, origin.forward);
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(ray, out hit, laserDistance, ~ignoreMask);

        if (hitSomething)
        {
            Vector3 worldEnd = hit.point;
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

        lineRenderer.SetPosition(0, localStart);
        lineRenderer.SetPosition(1, localEnd);

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
        if (isActive)
        {
            StopUsing();
        }
    }
}