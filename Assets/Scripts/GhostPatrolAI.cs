using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GhostPatrolAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform[] patrolPoints;
    public Animator animator;
    public CharacterController characterController;
    public Image attackImage;
    public RawImage attackRawImage;

    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float turnSpeed = 6f;
    public float waypointArrivalDistance = 0.5f;
    public float gravity = -9.81f;

    [Header("Detection and Attack")]
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public float attackHitDelay = 0.2f;
    public float attackImageDuration = 1.5f;
    public UnityEvent onAttack;

    private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
    private static readonly int MoveState = Animator.StringToHash("Base Layer.move");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.attack_shift");
    private static readonly int AttackTag = Animator.StringToHash("Attack");

    private int currentPatrolPoint;
    private float verticalSpeed;
    private float nextAttackTime;
    private Coroutine attackImageCoroutine;
    private bool attackInProgress;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (characterController == null) characterController = GetComponent<CharacterController>();

        if (patrolPoints != null)
        {
            foreach (Transform patrolPoint in patrolPoints)
            {
                if (patrolPoint != null && patrolPoint.IsChildOf(transform))
                {
                    patrolPoint.SetParent(null, true);
                }
            }
        }

        if (attackImage != null) attackImage.gameObject.SetActive(false);
        if (attackRawImage != null)
        {
            Canvas canvas = attackRawImage.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.transform.IsChildOf(transform))
            {
                canvas.transform.SetParent(null, false);
                canvas.transform.localScale = Vector3.one;
            }

            attackRawImage.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("GhostPatrolAI requires a Player reference.", this);
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogError("GhostPatrolAI requires an Animator with the ghost animation controller assigned.", this);
            enabled = false;
            return;
        }

        if (characterController == null)
        {
            Debug.LogError("GhostPatrolAI requires a CharacterController.", this);
            enabled = false;
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("GhostPatrolAI has no patrol points; the ghost will wait until it detects the player.", this);
        }
    }

    void Update()
    {
        float distanceToPlayer = GetHorizontalDistance(transform.position, player.position);
        bool playerDetected = distanceToPlayer <= detectionRange;
        bool attacking = IsPlayingAttack();

        if (playerDetected && distanceToPlayer <= attackRange)
        {
            FaceTarget(player.position);
            Move(Vector3.zero);

            if (!attacking && !attackInProgress && Time.time >= nextAttackTime)
            {
                Attack();
            }
        }
        else if (playerDetected)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        ApplyGravity();
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            PlayAnimation(IdleState);
            Move(Vector3.zero);
            return;
        }

        Transform target = patrolPoints[currentPatrolPoint];
        if (target == null)
        {
            PlayAnimation(IdleState);
            Move(Vector3.zero);
            return;
        }

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        if (offset.magnitude <= waypointArrivalDistance)
        {
            currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;
            return;
        }

        MoveTowards(offset.normalized, patrolSpeed);
    }

    void ChasePlayer()
    {
        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        MoveTowards(offset.normalized, chaseSpeed);
    }

    void MoveTowards(Vector3 direction, float speed)
    {
        FaceDirection(direction);
        PlayAnimation(MoveState);
        Move(direction * speed);
    }

    void Move(Vector3 horizontalMotion)
    {
        characterController.Move((horizontalMotion + Vector3.up * verticalSpeed) * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (characterController.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }
        else
        {
            verticalSpeed += gravity * Time.deltaTime;
        }
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        FaceDirection(direction);
    }

    void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;
        attackInProgress = true;
        animator.CrossFadeInFixedTime(AttackState, 0.1f);
        StartCoroutine(ResolveAttackHit());
    }

    IEnumerator ResolveAttackHit()
    {
        yield return new WaitForSeconds(attackHitDelay);
        attackInProgress = false;

        if (GetHorizontalDistance(transform.position, player.position) > attackRange) yield break;

        if (attackImage != null || attackRawImage != null)
        {
            if (attackImageCoroutine != null) StopCoroutine(attackImageCoroutine);
            if (attackImage != null) attackImage.gameObject.SetActive(true);
            if (attackRawImage != null) attackRawImage.gameObject.SetActive(true);
            attackImageCoroutine = StartCoroutine(HideAttackImageAfterDelay());
        }

        onAttack?.Invoke();
    }

    IEnumerator HideAttackImageAfterDelay()
    {
        yield return new WaitForSeconds(attackImageDuration);
        if (attackImage != null) attackImage.gameObject.SetActive(false);
        if (attackRawImage != null) attackRawImage.gameObject.SetActive(false);
        attackImageCoroutine = null;
    }

    static float GetHorizontalDistance(Vector3 first, Vector3 second)
    {
        Vector2 firstPosition = new Vector2(first.x, first.z);
        Vector2 secondPosition = new Vector2(second.x, second.z);
        return Vector2.Distance(firstPosition, secondPosition);
    }

    void OnDrawGizmosSelected()
    {
        DrawRangeGizmo(detectionRange, Color.yellow);
        DrawRangeGizmo(attackRange, Color.red);
    }

    void DrawRangeGizmo(float range, Color color)
    {
        if (range <= 0f) return;

        Gizmos.color = color;
        Vector3 center = transform.position + Vector3.up * 0.05f;
        const int segments = 48;
        Vector3 previousPoint = center + Vector3.forward * range;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 nextPoint = center + new Vector3(Mathf.Sin(angle) * range, 0f, Mathf.Cos(angle) * range);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    bool IsPlayingAttack()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        return state.tagHash == AttackTag || state.fullPathHash == AttackState;
    }

    void PlayAnimation(int state)
    {
        if (IsPlayingAttack()) return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.fullPathHash != state)
        {
            animator.CrossFadeInFixedTime(state, 0.15f);
        }
    }
}
