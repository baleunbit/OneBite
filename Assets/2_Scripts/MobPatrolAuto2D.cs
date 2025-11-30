using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MobPatrolAuto2D : MonoBehaviour
{
    public enum AutoPattern { Rectangle, Cross }

    [Header("자동 경로")]
    public AutoPattern pattern = AutoPattern.Rectangle;
    public Vector2 halfSize = new(1.5f, 1.0f);
    public float crossRadius = 1.5f;

    [Header("이동/대기")]
    public float patrolSpeed = 10f;
    public float arriveDist = 0.05f;
    public float waitAtPoint = 0.4f;
    public bool pingPong = true;

    [Header("의심 접근(발각 전)")]
    public bool approachOnProximity = true;
    public float suspicionSpeed = 2.0f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Mob mob;

    Vector2[] waypoints;
    int idx = 0, dir = +1;
    float waitTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>(true);
        mob = GetComponent<Mob>();

        BuildAutoPath();

        if (waypoints != null && waypoints.Length > 1)
        {
            idx = Random.Range(0, waypoints.Length);
            dir = Random.value < 0.5f ? +1 : -1;
            waitTimer = Random.Range(0f, waitAtPoint);
            patrolSpeed *= Random.Range(0.9f, 1.1f);
        }
    }

    void BuildAutoPath()
    {
        Vector2 c = transform.position;

        if (pattern == AutoPattern.Rectangle)
        {
            waypoints = new Vector2[]
            {
                c + new Vector2(-halfSize.x,  0f),
                c + new Vector2( 0f,          halfSize.y),
                c + new Vector2( halfSize.x,  0f),
                c + new Vector2( 0f,         -halfSize.y),
            };
        }
        else // Cross
        {
            waypoints = new Vector2[]
            {
                c + Vector2.left  * crossRadius,
                c + Vector2.up    * crossRadius,
                c + Vector2.right * crossRadius,
                c + Vector2.down  * crossRadius,
            };
        }
    }

    public void Tick()
    {
        if (!mob || mob.IsAlerted)
        {
            StopMove();
            return;
        }

        if (approachOnProximity && mob.target != null)
        {
            float dist = Vector2.Distance(rb.position, mob.target.position);
            if (dist <= mob.detectRadius)
            {
                Vector2 toPlayer = mob.target.position - rb.position;
                Vector2 step = toPlayer.normalized * suspicionSpeed * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + step);
                rb.linearVelocity = Vector2.zero;

                if (sr && Mathf.Abs(step.x) > 0.001f)
                    sr.flipX = step.x < 0f;

                return;
            }
        }

        if (waypoints == null || waypoints.Length == 0)
            return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            StopMove();
            return;
        }

        Vector2 cur = rb.position;
        Vector2 target = waypoints[Mathf.Clamp(idx, 0, waypoints.Length - 1)];

        float distToWp = Vector2.Distance(cur, target);
        if (distToWp <= arriveDist)
        {
            AdvanceIndex();
            waitTimer = waitAtPoint;
            StopMove();
        }
        else
        {
            Vector2 step = (target - cur).normalized * patrolSpeed * Time.fixedDeltaTime;
            mob.currentViewDirection = step.normalized;
            rb.MovePosition(cur + step);
            rb.linearVelocity = Vector2.zero;

            // 수정된 sprite 방향 동기화 (시야 기준)
            if (sr && Mathf.Abs(mob.currentViewDirection.x) > 0.001f)
                sr.flipX = mob.currentViewDirection.x < 0f;
        }
    }

    void StopMove()
    {
        rb.linearVelocity = Vector2.zero;
    }

    void AdvanceIndex()
    {
        int len = waypoints.Length;
        if (len <= 1) return;

        if (pingPong)
        {
            idx += dir;
            if (idx >= len - 1) { idx = len - 1; dir = -1; }
            else if (idx <= 0) { idx = 0; dir = +1; }
        }
        else
        {
            idx = (idx + 1) % len;
        }
    }
}