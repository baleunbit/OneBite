using UnityEngine;
using System.Collections;

public class Boss2 : MonoBehaviour
{
    [Header("Boss Status")]
    public int maxHP = 100;
    int hp;
    public int contactDamage = 5;

    [Header("Boss UI")]
    public BossBar bossBar;
    public string bossName = "BOSS 2";

    [Header("돌진 설정")]
    public float chargeSpeed = 15f;           // 돌진 속도
    public float chargeDuration = 1.5f;       // 돌진 지속 시간
    public float trackingStrength = 3f;       // 유도성 (높을수록 플레이어를 잘 따라감)
    public float chargeInterval = 3f;         // 돌진 사이 대기 시간

    [Header("둔화 장판")]
    public GameObject slowFieldPrefab;        // 둔화 장판 프리팹
    public float slowFieldSpawnInterval = 0.3f; // 장판 생성 간격
    public float slowFieldDuration = 5f;      // 장판 지속 시간

    [Header("피격 효과")]
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.1f;

    public bool canAct = false;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform player;
    Color originalColor;
    Coroutine hitFlashCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) originalColor = sr.color;
    }

    void Start()
    {
        hp = maxHP;

        if (!bossBar)
            bossBar = FindFirstObjectByType<BossBar>(FindObjectsInactive.Include);

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;
    }

    public void StartPattern()
    {
        if (!canAct)
        {
            canAct = true;
            StartCoroutine(PatternRoutine());
        }
    }

    IEnumerator PatternRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 시작 대기

        while (hp > 0 && canAct)
        {
            // 대기
            yield return new WaitForSeconds(chargeInterval);

            if (!canAct || hp <= 0) break;

            // 돌진 실행
            yield return StartCoroutine(ChargeAttack());
        }
    }

    IEnumerator ChargeAttack()
    {
        if (!player) yield break;

        float elapsed = 0f;
        float spawnTimer = 0f;

        // 초기 방향 설정 (플레이어 방향)
        Vector2 direction = ((Vector2)player.position - rb.position).normalized;

        Debug.Log("[Boss2] 돌진 시작!");

        while (elapsed < chargeDuration && canAct)
        {
            // 유도성: 플레이어 방향으로 서서히 회전
            if (player)
            {
                Vector2 toPlayer = ((Vector2)player.position - rb.position).normalized;
                direction = Vector2.Lerp(direction, toPlayer, trackingStrength * Time.deltaTime).normalized;
            }

            // 이동
            rb.linearVelocity = direction * chargeSpeed;

            // 스프라이트 플립
            if (sr) sr.flipX = direction.x < 0;

            // 둔화 장판 생성
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= slowFieldSpawnInterval)
            {
                spawnTimer = 0f;
                SpawnSlowField(transform.position);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 돌진 종료
        rb.linearVelocity = Vector2.zero;

        Debug.Log("[Boss2] 돌진 종료");
    }

    void SpawnSlowField(Vector3 position)
    {
        if (!slowFieldPrefab) return;

        GameObject field = Instantiate(slowFieldPrefab, position, Quaternion.identity);
        
        // 장판 자동 삭제
        Destroy(field, slowFieldDuration);
    }

    // 플레이어와 충돌 시 데미지
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            var playerComp = col.gameObject.GetComponent<Player>();
            if (playerComp) playerComp.TakeDamage(contactDamage);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            var playerComp = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
            if (playerComp) playerComp.TakeDamage(contactDamage);
        }
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;

        // 피격 효과
        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlashCoroutine());

        if (bossBar != null)
            bossBar.UpdateHP(hp, maxHP);

        if (hp <= 0)
            Die();
    }

    IEnumerator HitFlashCoroutine()
    {
        if (sr)
        {
            sr.color = hitColor;
            yield return new WaitForSecondsRealtime(hitFlashDuration);
            sr.color = originalColor;
        }
    }

    void Die()
    {
        canAct = false;
        StopAllCoroutines();

        if (bossBar != null)
            bossBar.Hide();

        Debug.Log("[Boss2] 처치됨!");
        Destroy(gameObject);
    }
}

