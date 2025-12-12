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
        if (rb)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        var col = GetComponent<Collider2D>();
        if (!isPlayerBullet)
        {
            Debug.Log($"[BossBullet] 생성됨 - Rigidbody2D: {rb != null}, Collider2D: {col != null}, IsTrigger: {(col ? col.isTrigger : false)}");
        }
    }
    
    void Start()
    {
        if (!isPlayerBullet)
        {
            // 자기 자신 무시 설정
            var myCol = GetComponent<Collider2D>();
            if (myCol)
            {
                // 보스와 충돌 무시 (모든 BossBase 상속 클래스)
                var bosses = FindObjectsByType<BossBase>(FindObjectsSortMode.None);
                foreach (var boss in bosses)
                {
                    var bossCol = boss.GetComponent<Collider2D>();
                    if (bossCol) Physics2D.IgnoreCollision(myCol, bossCol);
                }
            }
        }
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
        if (!isPlayerBullet)
            Debug.Log($"[BossBullet] Trigger 충돌: {col.name}, Tag: {col.tag}");
        HandleCollision(col.gameObject);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isPlayerBullet)
            Debug.Log($"[BossBullet] Collision 충돌: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");
        HandleCollision(collision.gameObject);
    }
    
    void HandleCollision(GameObject obj)
    {
        string tag = obj.tag;
        
        // Room은 항상 무시
        if (tag == "Room") return;
        
        // 플레이어 총알: 플레이어 무시, 몹/보스에게 데미지
        if (isPlayerBullet)
        {
            if (tag == "Player") return;
            
            // 몹이면 데미지 + 관통 처리
            var mob = obj.GetComponentInParent<Mob>() ?? obj.GetComponent<Mob>();
            if (mob != null)
            {
                mob.TakeDamage(Mathf.RoundToInt(damage));
                pierce--;
                if (pierce <= 0) Destroy(gameObject);
                return;
            }

            if (tag == "Boss")
            {
                // BossBase를 상속받는 모든 보스 처리 (Boss, Boss3 등)
                var bossBase = obj.GetComponent<BossBase>();
                if (bossBase != null)
                {
                    bossBase.TakeDamage((int)damage);
                }
                Destroy(gameObject);
                return;
            }
        }
        // 적/보스 총알: 플레이어에게 데미지
        else
        {
            if (tag == "Player")
            {
                var player = obj.GetComponent<Player>() ?? obj.GetComponentInParent<Player>();
                if (player != null)
                {
                    player.TakeDamage(Mathf.RoundToInt(damage));
                    Debug.Log($"[BossBullet] 플레이어 피격! 데미지: {damage}");
                }
                Destroy(gameObject);
                return;
            }
            
            // 몹/보스는 무시
            if (tag == "Mob" || tag == "Boss") return;
        }

        // 🔽 환경 오브젝트(총알 막는 용) 태그로 처리
        if (tag == "GameObject" ||
            (obj.transform.parent && obj.transform.parent.CompareTag("GameObject")))
        {
            Destroy(gameObject);
            return;
        }
    }
}
