using UnityEngine;

[RequireComponent(typeof(Mob))]
public class MobPatrolAuto2D : MonoBehaviour
{
    public float patrolSpeed = 1f;
    
    // 순찰 타이밍
    public float moveTime = 1.5f;   // 이동 시간 (1~2초)
    public float stopTime = 1f;     // 멈추는 시간
    
    Mob mob;
    float timer;
    bool isMoving = true;

    void Awake()
    {
        mob = GetComponent<Mob>();
        timer = moveTime;
    }

    public void Tick()
    {
        if (mob.isSensing || mob.hasSpotted) return;

        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            isMoving = !isMoving;
            timer = isMoving ? moveTime : stopTime;
        }
        
        if (isMoving)
        {
            transform.position += (Vector3)(mob.patrolDir * patrolSpeed * Time.deltaTime);
        }
    }
}
