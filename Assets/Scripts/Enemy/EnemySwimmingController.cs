using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyAgentContext))]
[RequireComponent(typeof(EnemyAnimationController))]
public sealed class EnemySwimmingController : MonoBehaviour
{
    [Header("Patrol Sampling")]

    [Tooltip(
        "Độ cao tối đa enemy có thể random lên hoặc xuống " +
        "so với vị trí spawn.")]
    [SerializeField, Min(0.1f)]
    private float verticalPatrolRadius = 3f;

    [Tooltip(
        "Không chọn patrol point quá gần enemy.")]
    [SerializeField, Min(0f)]
    private float minimumPatrolDistance = 2f;

    [Tooltip(
        "Số lần thử tìm patrol point hợp lệ.")]
    [SerializeField, Min(1)]
    private int maxAttempts = 10;

    [Header("Arrival")]

    [Tooltip(
        "Khoảng cách được xem là đã đến patrol destination.")]
    [SerializeField, Min(0.01f)]
    private float arrivalDistance = 0.25f;

    [Header("Obstacle Avoidance")]

    [Tooltip(
        "Chỉ chọn các layer môi trường như Terrain, Rock, Wall.")]
    [SerializeField]
    private LayerMask obstacleMask;

    [Tooltip(
        "Bán kính thân enemy dùng để kiểm tra khoảng trống.")]
    [SerializeField, Min(0.01f)]
    private float bodyClearanceRadius = 0.5f;

    [Header("Stuck Detection")]

    [Tooltip(
        "Enemy không tiến triển trong khoảng thời gian này " +
        "thì movement bị Failed.")]
    [SerializeField, Min(0.1f)]
    private float stuckTimeout = 2f;

    [Tooltip(
        "Khoảng cách tối thiểu phải di chuyển trong một frame " +
        "để không bị xem là đứng yên.")]
    [SerializeField, Min(0.0001f)]
    private float minimumProgressDistance = 0.001f;

    private EnemyAgentContext enemyContext;
    private EnemyAnimationController animationController;

    // Tâm patrol được cập nhật mỗi lần enemy được spawn từ pool.
    private Vector3 patrolOrigin;

    private Vector3 patrolDestination;
    private bool hasPatrolDestination;

    private Vector3 previousPosition;
    private float stuckElapsed;

    private bool HasValidContext =>
        enemyContext != null &&
        enemyContext.Stats != null;

    private void Awake()
    {
        enemyContext =
            GetComponent<EnemyAgentContext>();

        animationController =
            GetComponent<EnemyAnimationController>();

        patrolOrigin = transform.position;
        previousPosition = transform.position;
    }

    #region Chase

    public bool TryChase(GameObject target)
    {
        if (!HasValidContext ||
            target == null ||
            !target.activeInHierarchy)
        {
            Stop();
            return false;
        }

        // Khi bắt đầu chase, patrol destination cũ không còn giá trị.
        ClearPatrolState();

        Vector3 destination =
            target.transform.position;

        Vector3 offset =
            destination - transform.position;

        // Enemy đã ở đúng vị trí target.
        if (offset.sqrMagnitude <= 0.0001f)
        {
            animationController.SetMoving(false);
            return true;
        }

        float speed =
            Mathf.Max(0f, enemyContext.Stats.runSpeed);

        if (!TryMoveTowards(destination, speed))
        {
            Stop();
            return false;
        }

        animationController.SetMoving(true);
        return true;
    }

    #endregion

    #region Patrol

    public bool TryStartPatrol()
    {
        if (!HasValidContext)
        {
            Stop();
            return false;
        }

        if (!TryGetRandomPatrolPoint(
                out Vector3 destination))
        {
            Stop();
            return false;
        }

        patrolDestination = destination;
        hasPatrolDestination = true;

        previousPosition = transform.position;
        stuckElapsed = 0f;

        animationController.SetMoving(true);

        return true;
    }

    public EnemyMovementProgress TickPatrol()
    {
        if (!isActiveAndEnabled ||
            !HasValidContext ||
            !hasPatrolDestination)
        {
            Stop();
            return EnemyMovementProgress.Failed;
        }

        Vector3 offset =
            patrolDestination - transform.position;

        float arrivalDistanceSqr =
            arrivalDistance * arrivalDistance;

        // Kiểm tra trước khi di chuyển.
        if (offset.sqrMagnitude <= arrivalDistanceSqr)
        {
            Stop();
            return EnemyMovementProgress.Succeeded;
        }

        float speed =
            Mathf.Max(0f, enemyContext.Stats.walkSpeed);

        if (!TryMoveTowards(
                patrolDestination,
                speed))
        {
            Stop();
            return EnemyMovementProgress.Failed;
        }

        if (IsStuck())
        {
            Stop();
            return EnemyMovementProgress.Failed;
        }

        animationController.SetMoving(true);

        // Kiểm tra lại sau khi đã di chuyển trong frame này.
        offset =
            patrolDestination - transform.position;

        if (offset.sqrMagnitude <= arrivalDistanceSqr)
        {
            Stop();
            return EnemyMovementProgress.Succeeded;
        }

        return EnemyMovementProgress.Running;
    }

    private bool TryGetRandomPatrolPoint(
        out Vector3 patrolPoint)
    {
        patrolPoint = default;

        if (!HasValidContext)
            return false;

        float horizontalRadius =
            Mathf.Max(
                minimumPatrolDistance,
                enemyContext.Stats.wanderRadius);

        float minimumDistanceSqr =
            minimumPatrolDistance *
            minimumPatrolDistance;

        int attempts =
            Mathf.Max(1, maxAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector3 random =
                Random.insideUnitSphere;

            // Ellipsoid: rộng theo X/Z, thấp hơn theo Y.
            Vector3 offset = new Vector3(
                random.x * horizontalRadius,
                random.y * verticalPatrolRadius,
                random.z * horizontalRadius);

            Vector3 candidate =
                patrolOrigin + offset;

            Vector3 path =
                candidate - transform.position;

            if (path.sqrMagnitude <
                minimumDistanceSqr)
            {
                continue;
            }

            if (IsPointBlocked(candidate))
                continue;

            if (IsPathBlocked(
                    transform.position,
                    candidate))
            {
                continue;
            }

            patrolPoint = candidate;
            return true;
        }

        return false;
    }

    #endregion

    #region Movement

    private bool TryMoveTowards(
        Vector3 destination,
        float speed)
    {
        if (speed <= 0f)
            return false;

        Vector3 currentPosition =
            transform.position;

        Vector3 direction =
            destination - currentPosition;

        float distance =
            direction.magnitude;

        if (distance <= Mathf.Epsilon)
            return true;

        RotateTowards(direction);

        float movementStep =
            speed * Time.deltaTime;

        if (movementStep <= 0f)
            return false;

        Vector3 nextPosition =
            Vector3.MoveTowards(
                currentPosition,
                destination,
                movementStep);

        // Kiểm tra đoạn ngắn mà enemy chuẩn bị đi trong frame này.
        if (IsPathBlocked(
                currentPosition,
                nextPosition))
        {
            return false;
        }

        transform.position = nextPosition;
        return true;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (!HasValidContext ||
            direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 normalizedDirection =
            direction.normalized;

        // Tránh LookRotation bị không xác định khi bơi thẳng đứng.
        Vector3 up =
            Mathf.Abs(
                Vector3.Dot(
                    normalizedDirection,
                    Vector3.up)) > 0.98f
                ? Vector3.forward
                : Vector3.up;

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                normalizedDirection,
                up);

        float angularSpeed =
            Mathf.Max(
                0f,
                enemyContext.Stats.angularSpeed);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                angularSpeed * Time.deltaTime);
    }

    #endregion

    #region Obstacle Checks

    private bool IsPointBlocked(Vector3 point)
    {
        if (obstacleMask.value == 0)
            return false;

        return Physics.CheckSphere(
            point,
            bodyClearanceRadius,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool IsPathBlocked(
        Vector3 start,
        Vector3 end)
    {
        if (obstacleMask.value == 0)
            return false;

        Vector3 path =
            end - start;

        float distance =
            path.magnitude;

        if (distance <= Mathf.Epsilon)
            return false;

        return Physics.SphereCast(
            start,
            bodyClearanceRadius,
            path / distance,
            out _,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    #endregion

    #region Stuck Detection

    private bool IsStuck()
    {
        float progressSqr =
            (transform.position - previousPosition)
            .sqrMagnitude;

        float minimumProgressSqr =
            minimumProgressDistance *
            minimumProgressDistance;

        if (progressSqr <= minimumProgressSqr)
        {
            stuckElapsed += Time.deltaTime;
        }
        else
        {
            stuckElapsed = 0f;
        }

        previousPosition = transform.position;

        return stuckElapsed >= stuckTimeout;
    }

    #endregion

    #region State And Pooling

    public void Stop()
    {
        ClearPatrolState();

        if (animationController != null)
        {
            animationController.SetMoving(false);
        }
    }

    private void ClearPatrolState()
    {
        hasPatrolDestination = false;
        patrolDestination = default;

        previousPosition = transform.position;
        stuckElapsed = 0f;
    }

    public void ResetState()
    {
        // PooledEnemy gọi ResetState sau khi đã đặt enemy
        // vào vị trí spawn mới.
        patrolOrigin = transform.position;

        ClearPatrolState();

        if (animationController != null)
        {
            animationController.SetMoving(false);
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        EnemyAgentContext context =
            enemyContext != null
                ? enemyContext
                : GetComponent<EnemyAgentContext>();

        float horizontalRadius =
            context != null &&
            context.Stats != null
                ? Mathf.Max(
                    minimumPatrolDistance,
                    context.Stats.wanderRadius)
                : 5f;

        Vector3 center =
            Application.isPlaying
                ? patrolOrigin
                : transform.position;

        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        Gizmos.color =
            new Color(0f, 0.8f, 1f, 0.8f);

        Gizmos.matrix = Matrix4x4.TRS(
            center,
            Quaternion.identity,
            new Vector3(
                horizontalRadius,
                verticalPatrolRadius,
                horizontalRadius));

        Gizmos.DrawWireSphere(
            Vector3.zero,
            1f);

        Gizmos.matrix = previousMatrix;

        if (Application.isPlaying &&
            hasPatrolDestination)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                patrolDestination,
                arrivalDistance);

            Gizmos.DrawLine(
                transform.position,
                patrolDestination);
        }
    }

    #endregion
}