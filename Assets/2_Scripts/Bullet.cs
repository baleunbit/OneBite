using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 1.0f;

    [SerializeField] public float damage;
    int pierce;                 // 남은 관통 횟수
    Rigidbody2D rb;
    
    // 발사자 구분 (true = 플레이어 총알, false = 적/보스 총알)
    public bool isPlayerBullet = true;

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
        // Room은 항상 무시
        if (col.CompareTag("Room")) return;
        
        // 플레이어 총알: 플레이어 무시, 몹/보스에게 데미지
        if (isPlayerBullet)
        {
            if (col.CompareTag("Player")) return;
            
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
        }
        // 적/보스 총알: 플레이어에게 데미지
        else
        {
            if (col.CompareTag("Player"))
            {
                var player = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
                if (player != null)
                {
                    player.TakeDamage(Mathf.RoundToInt(damage));
                }
                Destroy(gameObject);
                return;
            }
            
            // 몹/보스는 무시
            if (col.CompareTag("Mob") || col.CompareTag("Boss")) return;
        }

        // 🔽 환경 오브젝트(총알 막는 용) 태그로 처리
        if (col.CompareTag("GameObject") ||
            (col.transform.parent && col.transform.parent.CompareTag("GameObject")))
        {
            Destroy(gameObject);
            return;
        }
    }
}
