using UnityEngine;

[RequireComponent(typeof(Mob))]
public class MobPatrolAuto2D : MonoBehaviour
{
    public float patrolSpeed = 1f;
    
    // 순찰 타이밍
    public float moveTime = 1.5f;   // 이동 시간 (1~2초)
    public float stopTime = 1f;     // 멈추는 시간
    
    [Header("회전")]
    public float turnSpeed = 90f;   // 순찰 중 회전 속도 (도/초)
    
    Mob mob;
    float timer;
    bool isMoving = true;
    
    Vector2 targetDirection;  // 목표 방향 (벽 감지 시 반대 방향)
    bool isTurning = false;   // 회전 중인지

    void Awake()
    {
        mob = GetComponent<Mob>();
        timer = moveTime;
    }
    
    void Start()
    {
        targetDirection = mob.currentViewDirection;
    }

    public void Tick()
    {
        if (mob.isSensing || mob.IsAlerted) return;

        // 타이머 처리 (이동/멈춤 반복)
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            isMoving = !isMoving;
            timer = isMoving ? moveTime : stopTime;
        }
        
        // detectRadius로 전방 벽/장애물 감지
        Vector2 avoidDir;
        if (DetectWallInRadius(out avoidDir))
        {
            targetDirection = avoidDir;
            isTurning = true;
        }
        
        // 시야 회전 (자연스럽게 목표 방향으로)
        if (isTurning)
        {
            float angle = Vector2.SignedAngle(mob.currentViewDirection, targetDirection);
            
            if (Mathf.Abs(angle) < 1f)
            {
                // 거의 도달함
                mob.currentViewDirection = targetDirection;
                isTurning = false;
            }
            else
            {
                // 천천히 회전
                float step = turnSpeed * Time.deltaTime;
                float rotateAngle = Mathf.MoveTowards(0f, angle, step);
                mob.currentViewDirection = (Quaternion.Euler(0, 0, rotateAngle) * mob.currentViewDirection).normalized;
            }
        }
        
        // 이동 (회전 중에도 천천히 이동)
        if (isMoving)
        {
            float speed = isTurning ? patrolSpeed * 0.3f : patrolSpeed;
            transform.position += (Vector3)(mob.currentViewDirection * speed * Time.deltaTime);
        }
    }
    
    bool DetectWallInRadius(out Vector2 avoidDirection)
    {
        avoidDirection = Vector2.zero;
        
        Vector2 origin = transform.position;
        float radius = mob.detectRadius;
        
        // detectRadius 내의 모든 콜라이더 감지
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius);
        
        Vector2 closestWallDir = Vector2.zero;
        float closestDist = float.MaxValue;
        bool foundWall = false;
        
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            
            // 플레이어 무시
            if (hit.CompareTag("Player")) continue;
            
            // 자기 자신 무시
            if (hit.transform.IsChildOf(transform) || hit.gameObject == gameObject) continue;
            
            // Edge Collider (벽) 또는 "GameObject" 태그 (장애물)
            bool isWall = hit is EdgeCollider2D || hit.CompareTag("GameObject");
            if (!isWall) continue;
            
            // 벽까지의 방향과 거리
            Vector2 toWall = (Vector2)hit.ClosestPoint(origin) - origin;
            float dist = toWall.magnitude;
            
            // 진행 방향 앞에 있는 벽만 고려 (뒤에 있는 건 무시)
            float dotProduct = Vector2.Dot(mob.currentViewDirection, toWall.normalized);
            if (dotProduct < 0.3f) continue;  // 앞쪽 약 70도 범위
            
            if (dist < closestDist)
            {
                closestDist = dist;
                closestWallDir = toWall.normalized;
                foundWall = true;
            }
        }
        
        if (foundWall && closestDist < radius * 0.7f)  // 70% 거리 이내면 회피
        {
            // 벽 반대 방향으로 회피
            avoidDirection = -closestWallDir;
            return true;
        }
        
        return false;
    }
}
