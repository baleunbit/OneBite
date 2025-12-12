using UnityEngine;
using System.Collections;

/// <summary>
/// Boss3 - 일직선 돌진 패턴
/// Warning으로 공격 경로 표시 후 돌진, Wall에 부딪히면 원래 위치로 복귀
/// </summary>
public class Boss3 : BossBase
{
    [Header("돌진 설정")]
    public float chargeSpeed = 20f;           // 돌진 속도
    public float chargeInterval = 2.5f;       // 돌진 사이 휴식 시간

    [Header("돌진 대미지")]
    public int chargeDamage = 10;             // 돌진 시 플레이어 대미지

    [Header("Warning 설정")]
    public GameObject warningPrefab;          // Warning 프리팹 (사각형 스프라이트)
    public float warningDuration = 2f;        // Warning 표시 시간
    public float warningWidth = 1.5f;         // Warning 너비
    public LayerMask wallLayer;               // Wall 레이어 (Raycast용)

    [Header("복귀 설정")]
    public float fadeOutDuration = 0.3f;      // 사라지는 시간
    public float fadeInDuration = 0.5f;       // 나타나는 시간
    public float returnDelay = 0.5f;          // 복귀 전 대기 시간

    [Header("둔화 장판")]
    public GameObject slowFieldPrefab;        // 둔화 장판 프리팹
    public float slowFieldSpawnInterval = 0.3f; // 장판 생성 간격
    public float slowFieldDuration = 5f;      // 장판 지속 시간

    [Header("피격 효과")]
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.1f;

    // 내부 변수
    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform player;
    Color originalColor;
    Coroutine hitFlashCoroutine;
    
    bool isCharging = false;
    Vector3 originalPosition;  // 원래 위치

    protected override void Start()
    {
        base.Start();

        rb = GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) originalColor = sr.color;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;

        // 원래 위치 저장
        originalPosition = transform.position;
    }

    /// <summary>
    /// 보스 패턴 루틴 - Warning 표시 후 돌진
    /// </summary>
    protected override IEnumerator PatternRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 시작 대기

        while (hp > 0 && canAct)
        {
            // 1. 휴식
            yield return new WaitForSeconds(chargeInterval);

            if (!canAct || hp <= 0) break;

            // 2. Warning 표시 + 돌진 실행
            yield return StartCoroutine(WarningAndCharge());
        }
    }

    /// <summary>
    /// Warning 표시 후 돌진
    /// </summary>
    IEnumerator WarningAndCharge()
    {
        if (!player) yield break;

        // 플레이어 방향으로 Raycast해서 벽까지의 거리 계산
        Vector2 startPos = transform.position;
        Vector2 dirToPlayer = ((Vector2)player.position - startPos).normalized;
        
        // 벽까지의 거리 계산 (Raycast)
        float maxDistance = 50f;
        RaycastHit2D hit = Physics2D.Raycast(startPos, dirToPlayer, maxDistance, wallLayer);
        
        Vector2 endPos;
        if (hit.collider != null)
        {
            endPos = hit.point;
        }
        else
        {
            endPos = startPos + dirToPlayer * maxDistance;
        }

        // Warning 생성
        if (warningPrefab)
        {
            GameObject warning = Instantiate(warningPrefab, Vector3.zero, Quaternion.identity);
            BossWarning warningScript = warning.GetComponent<BossWarning>();
            
            if (warningScript)
            {
                warningScript.SetupLine(startPos, endPos, warningWidth);
                warningScript.StartWarning(warningDuration);
            }
            else
            {
                // BossWarning 스크립트가 없으면 수동으로 설정
                SetupWarningManual(warning, startPos, endPos);
            }
        }

        Debug.Log("[Boss3] Warning 표시 중...");

        // Warning 시간 동안 대기
        yield return new WaitForSeconds(warningDuration);

        if (!canAct || hp <= 0) yield break;

        // 돌진 실행
        yield return StartCoroutine(ChargeAttack(dirToPlayer));
    }

    /// <summary>
    /// Warning 프리팹에 BossWarning이 없을 경우 수동 설정
    /// </summary>
    void SetupWarningManual(GameObject warning, Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        warning.transform.position = (start + end) / 2f;
        warning.transform.rotation = Quaternion.Euler(0, 0, angle);
        warning.transform.localScale = new Vector3(distance, warningWidth, 1f);

        // 자동 삭제
        Destroy(warning, warningDuration);
    }

    /// <summary>
    /// 일직선 돌진 공격
    /// </summary>
    IEnumerator ChargeAttack(Vector2 chargeDirection)
    {
        isCharging = true;
        float spawnTimer = 0f;

        // 스프라이트 플립
        if (sr) sr.flipX = chargeDirection.x < 0;

        Debug.Log($"[Boss3] 돌진 시작! 방향: {chargeDirection}");

        // 돌진 실행 - Wall에 부딪힐 때까지
        while (isCharging && canAct)
        {
            rb.linearVelocity = chargeDirection * chargeSpeed;

            // 둔화 장판 생성
            if (slowFieldPrefab)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= slowFieldSpawnInterval)
                {
                    spawnTimer = 0f;
                    SpawnSlowField(transform.position);
                }
            }

            yield return null;
        }

        // Wall에 부딪혀서 멈춤 → 원래 위치로 복귀
        if (!isCharging)
        {
            yield return StartCoroutine(ReturnToOriginalPosition());
        }
    }

    /// <summary>
    /// 둔화 장판 생성
    /// </summary>
    void SpawnSlowField(Vector3 position)
    {
        if (!slowFieldPrefab) return;

        GameObject field = Instantiate(slowFieldPrefab, position, Quaternion.identity);
        Destroy(field, slowFieldDuration);
    }

    /// <summary>
    /// 원래 위치로 복귀 (페이드 아웃 → 이동 → 페이드 인)
    /// </summary>
    IEnumerator ReturnToOriginalPosition()
    {
        Debug.Log("[Boss3] 원래 위치로 복귀 중...");

        // 잠시 대기
        yield return new WaitForSeconds(returnDelay);

        // 페이드 아웃
        yield return StartCoroutine(FadeOut());

        // 원래 위치로 이동
        transform.position = originalPosition;
        rb.linearVelocity = Vector2.zero;

        // 페이드 인
        yield return StartCoroutine(FadeIn());

        Debug.Log("[Boss3] 복귀 완료!");
    }

    /// <summary>
    /// 페이드 아웃 (투명해짐)
    /// </summary>
    IEnumerator FadeOut()
    {
        if (!sr) yield break;

        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            sr.color = c;

            yield return null;
        }

        Color finalColor = startColor;
        finalColor.a = 0f;
        sr.color = finalColor;
    }

    /// <summary>
    /// 페이드 인 (나타남)
    /// </summary>
    IEnumerator FadeIn()
    {
        if (!sr) yield break;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            Color c = originalColor;
            c.a = Mathf.Lerp(0f, 1f, t);
            sr.color = c;

            yield return null;
        }

        sr.color = originalColor;
    }

    /// <summary>
    /// 돌진 멈춤
    /// </summary>
    void StopCharge()
    {
        isCharging = false;
        rb.linearVelocity = Vector2.zero;
        Debug.Log("[Boss3] 돌진 종료 (Wall 충돌)");
    }

    /// <summary>
    /// 충돌 감지 - Wall 태그와 플레이어
    /// </summary>
    void OnCollisionEnter2D(Collision2D col)
    {
        // Wall 태그와 충돌 시 돌진 멈춤
        if (col.gameObject.CompareTag("Wall"))
        {
            if (isCharging)
            {
                Debug.Log("[Boss3] 벽과 충돌!");
                StopCharge();
            }
        }

        // 플레이어와 충돌 시 대미지
        if (col.gameObject.CompareTag("Player"))
        {
            var playerComp = col.gameObject.GetComponent<Player>();
            if (playerComp)
            {
                int damage = isCharging ? chargeDamage : contactDamage;
                playerComp.TakeDamage(damage);
                Debug.Log($"[Boss3] 플레이어에게 {damage} 대미지!");
            }
        }
    }

    /// <summary>
    /// 트리거 충돌
    /// </summary>
    void OnTriggerEnter2D(Collider2D col)
    {
        // Wall 태그와 충돌 시 돌진 멈춤
        if (col.CompareTag("Wall"))
        {
            if (isCharging)
            {
                Debug.Log("[Boss3] 벽(트리거)과 충돌!");
                StopCharge();
            }
        }

        // 플레이어와 충돌 시 대미지
        if (col.CompareTag("Player"))
        {
            var playerComp = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
            if (playerComp)
            {
                int damage = isCharging ? chargeDamage : contactDamage;
                playerComp.TakeDamage(damage);
                Debug.Log($"[Boss3] 플레이어에게 {damage} 대미지!");
            }
        }
    }

    /// <summary>
    /// 데미지 처리 오버라이드
    /// </summary>
    protected override void OnDamaged(int dmg)
    {
        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlashCoroutine());
    }

    IEnumerator HitFlashCoroutine()
    {
        if (sr)
        {
            Color prevColor = sr.color;
            sr.color = hitColor;
            yield return new WaitForSecondsRealtime(hitFlashDuration);
            sr.color = prevColor.a > 0.5f ? originalColor : prevColor;
        }
    }

    /// <summary>
    /// 사망 처리 오버라이드
    /// </summary>
    protected override void OnDeath()
    {
        isCharging = false;
        rb.linearVelocity = Vector2.zero;
        Debug.Log("[Boss3] 처치됨!");
    }
}
