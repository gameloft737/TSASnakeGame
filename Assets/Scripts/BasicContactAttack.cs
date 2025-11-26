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
    [SerializeField] private SnakeBody snakeBody;
    [SerializeField] private string mouthOpenBool = "mouthOpen";

    private bool mouthOpen;
    private bool waitingToOpen;
    private bool waitingToClose;
    private float lastEnemySeenTime;
    private AppleEnemy trackedEnemy; // Track the enemy that triggered opening

    private void OnTriggerEnter(Collider other)
    {
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
        if (enemy == null || enemy.isMetal) return;

        if (!mouthOpen && !waitingToOpen)
        {
            trackedEnemy = enemy; // Store reference to this enemy
            waitingToOpen = true;
            StartCoroutine(OpenAfterDelay());
        }
    }

    private IEnumerator OpenAfterDelay()
    {
        yield return new WaitForSeconds(mouthOpenDelay);

        waitingToOpen = false;
        mouthOpen = true;
        lastEnemySeenTime = Time.time;

        if (attackManager != null)
            attackManager.SetBool(mouthOpenBool, true);

        // Kill the tracked enemy even if it left the trigger
        if (trackedEnemy != null)
        {
            snakeBody.TriggerSwallowAnimation();
            trackedEnemy.Die();
            trackedEnemy = null; // Clear reference after killing
        }
    }

    private void OnTriggerStay(Collider other)
    {
        AppleEnemy enemy = other.GetComponentInParent<AppleEnemy>();
        if (enemy == null || enemy.isMetal) return;

        if (mouthOpen)
        {
            snakeBody.TriggerSwallowAnimation();
            enemy.Die();
        }
    }

    private void Update()
    {
        if (!mouthOpen || waitingToClose) return;

        bool enemyNearby = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, nearbyRange, enemyLayer);

        foreach (var h in hits)
        {
            AppleEnemy e = h.GetComponentInParent<AppleEnemy>();
            if (e != null && !e.isMetal)
            {
                enemyNearby = true;
                lastEnemySeenTime = Time.time;
                break;
            }
        }

        // Check if enough time has passed since last enemy was seen
        if (!enemyNearby && Time.time >= lastEnemySeenTime + mouthCloseDelay)
        {
            waitingToClose = true;
            StartCoroutine(CloseMouth());
        }
    }

    private IEnumerator CloseMouth()
    {
        yield return null;

        mouthOpen = false;
        waitingToClose = false;

        if (attackManager != null)
            attackManager.SetBool(mouthOpenBool, false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, nearbyRange);
    }
}