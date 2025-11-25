using System.Collections;
using UnityEngine;

public class BasicContactAttack : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float mouthOpenDelay = 0.2f;
    [SerializeField] private float mouthCloseDelay = 0.3f;

    [Header("Detection")]
    [SerializeField] private float nearbyRange = 3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Animation")]
    [SerializeField] private AttackManager attackManager;
    [SerializeField] private string mouthOpenBool = "mouthOpen";

    private bool mouthOpen;
    private bool waitingToOpen;
    private float closeTimer;

    private void OnTriggerEnter(Collider other)
    {
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
        if (enemy == null || enemy.isMetal) return;

        if (!mouthOpen && !waitingToOpen)
        {
            waitingToOpen = true;
            StartCoroutine(OpenAfterDelay(enemy));
        }
    }

    private IEnumerator OpenAfterDelay(AppleEnemy enemy)
    {
        yield return new WaitForSeconds(mouthOpenDelay);

        waitingToOpen = false;
        mouthOpen = true;

        if (attackManager != null)
            attackManager.SetBool(mouthOpenBool, true);

        if (enemy != null)
            enemy.Die();
    }

    private void OnTriggerStay(Collider other)
    {
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
        if (enemy == null || enemy.isMetal) return;

        if (mouthOpen)
            enemy.Die();
        
    }

    private void Update()
    {
        if (!mouthOpen) return;

        bool enemyNearby = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, nearbyRange, enemyLayer);

        foreach (var h in hits)
        {
            AppleEnemy e = h.GetComponentInParent<AppleEnemy>();
            if (e != null && !e.isMetal)
            {
                enemyNearby = true;
                break;
            }
        }

        if (enemyNearby)
        {
            closeTimer = Time.time + mouthCloseDelay;
        }
        else
        {
            if (Time.time >= closeTimer)
            {
                mouthOpen = false;

                if (attackManager != null)
                    attackManager.SetBool(mouthOpenBool, false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, nearbyRange);
    }
}
