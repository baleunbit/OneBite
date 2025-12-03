using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Mob : MonoBehaviour
{
    [Header("이동/추격")]
    public float Speed = 7f;

    [Header("탐지")]
    public float detectRadius = 4f;
    public float viewDistance = 6f;
    [Range(0, 180)] public float fovAngle = 80f;

    [Header("시야 회전 속도")]
    public float alertRotationSpeed = 150f;

    [Header("공격")]
    public int minDamage = 3;
    public int maxDamage = 5;
    public float attackCooldown = 1f;

    [Header("체력")]
    public int maxHP = 30;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;

    float nextAttack = 0f;
    int currentHP;
    bool isLive = true;

    // 상태
    public bool hasSpotted = false;
    public bool isSensing = false;

    // 목표
    public Transform target;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        currentHP = maxHP;
        if (!target)
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void FixedUpdate()
    {
        if (!isLive || !target) return;

        float dist = (target.position - transform.position).sqrMagnitude;

        // → 부채꼴 체크는 MobSenseVisualize가 계산한 값을 사용한다.
        bool inDetect = dist <= detectRadius * detectRadius;

        if (!hasSpotted)
        {
            if (MobSenseVisualize.PlayerInFOV(this))
            {
                hasSpotted = true;
            }
            else if (inDetect)
            {
                isSensing = true;
                rb.linearVelocity = Vector2.zero;
                return;
            }
            else
            {
                isSensing = false;
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        // 추격
        Vector2 dir = (target.position - transform.position).normalized;
        rb.MovePosition(rb.position + dir * Speed * Time.fixedDeltaTime);

        if (anim) anim.SetBool("isWalk", true);

        sr.flipX = dir.x < 0;

        rb.linearVelocity = Vector2.zero;
    }

    void OnCollisionStay2D(Collision2D c) => TryAttack(c.collider);
    void OnTriggerStay2D(Collider2D c) => TryAttack(c);

    void TryAttack(Collider2D col)
    {
        if (!hasSpotted || !isLive) return;
        if (!col.CompareTag("Player")) return;
        if (Time.time < nextAttack) return;

        Player p = col.GetComponent<Player>();
        if (!p) return;

        p.TakeDamage(Random.Range(minDamage, maxDamage + 1));
        nextAttack = Time.time + attackCooldown;
    }

    public void TakeDamage(int dmg)
    {
        if (!isLive) return;
        currentHP -= dmg;
        hasSpotted = true;

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        isLive = false;
        Destroy(gameObject);
    }
}
