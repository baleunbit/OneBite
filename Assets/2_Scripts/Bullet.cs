using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 1.0f;

    [SerializeField] public float damage;
    int pierce;                 // 남은 관통 횟수
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        // 콜라이더는 프리팹에서 IsTrigger = On 권장
    }

    public void Init(float damage, int pierce, Vector2 _)
    {
        this.damage = Mathf.Max(0f, damage);
        this.pierce = Mathf.Max(1, pierce);
    }

    public void Setup(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;
        Invoke(nameof(SelfDestruct), lifeTime);
    }

    void SelfDestruct()
    {
        if (this) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // 플레이어/룸은 무시
        if (col.CompareTag("Player") || col.CompareTag("Room")) return;

        // 몹이면 데미지 + 관통 처리
        var mob = col.GetComponentInParent<Mob>() ?? col.GetComponent<Mob>();
        if (mob != null)
        {
            mob.TakeDamage(Mathf.RoundToInt(damage));
            pierce--;
            if (pierce <= 0) Destroy(gameObject);
            return;
        }

        if (col.CompareTag("Boss"))
        {
            col.GetComponent<Boss>()?.TakeDamage((int)damage);
            Destroy(gameObject);
            return;
        }

        // 🔽 환경 오브젝트(총알 막는 용) 태그로 처리
        if (col.CompareTag("GameObject") ||
            (col.transform.parent && col.transform.parent.CompareTag("GameObject")))
        {
            Destroy(gameObject);
            return;
        }

        // 필요하면 여기서 기본 처리(전부 제거)도 가능
        // Destroy(gameObject);
    }
}
