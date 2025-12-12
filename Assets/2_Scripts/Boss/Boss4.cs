using UnityEngine;
using System.Collections;

/// <summary>
/// 4스테이지 보스 - 세로 방향 돌진 패턴
/// Warning으로 공격 경로 표시 후 아래로 돌진, Wall에 부딪히면 원래 위치로 복귀
/// </summary>
public class Boss4 : BossBase
{
    [Header("돌진 설정")]
    public float chargeSpeed = 20f;           // 돌진 속도
    public float chargeInterval = 5f;         // 돌진 사이 휴식 시간 (5초)

    [Header("돌진 대미지")]
    public int chargeDamage = 10;             // 돌진 시 플레이어 대미지

    [Header("Warning 설정")]
    public GameObject warningObject;          // 씬에 배치된 Warning 오브젝트
    public float warningDuration = 1f;        // Warning 표시 시간 (1초)

    [Header("복귀 설정")]
    public float fadeOutDuration = 0.3f;      // 사라지는 시간
    public float fadeInDuration = 0.5f;       // 나타나는 시간
    public float returnDelay = 0.5f;          // 복귀 전 대기 시간

    [Header("충돌 설정")]
    public LayerMask wallLayer;               // Wall 레이어
    public LayerMask playerLayer;             // Player 레이어
    public float collisionCheckDistance = 0.5f; // 충돌 체크 거리

    // 내부 변수
    SpriteRenderer sr;
    SpriteRenderer warningSr;
    Collider2D myCollider;
    Color boss4OriginalColor;
    
    bool isCharging = false;
    Vector3 originalPosition;  // 원래 위치
    float targetX;             // 돌진할 X 위치

    protected override void Start()
    {
        base.Start();

        sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) boss4OriginalColor = sr.color;

        myCollider = GetComponent<Collider2D>();

        // 원래 위치 저장
        originalPosition = transform.position;

        // Warning 오브젝트 초기화 (처음엔 비활성화)
        if (warningObject)
        {
            warningSr = warningObject.GetComponent<SpriteRenderer>();
            warningObject.SetActive(false);
        }
    }

    /// <summary>
    /// 보스 패턴 루틴 - Warning 표시 후 돌진 (반복)
    /// </summary>
    protected override IEnumerator PatternRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 시작 대기

        while (hp > 0 && canAct)
        {
            Debug.Log("[Boss4] 패턴 루프 시작, 대기 중...");
            
            // 1. 휴식 (5초)
            yield return new WaitForSeconds(chargeInterval);

            if (!canAct || hp <= 0) break;

            Debug.Log("[Boss4] 공격 시작!");

            // 2. Warning 표시 + 돌진 실행
            yield return StartCoroutine(WarningAndCharge());

            Debug.Log("[Boss4] 공격 완료, 다음 루프로...");
        }
    }

    /// <summary>
    /// Warning 표시 후 돌진
    /// </summary>
    IEnumerator WarningAndCharge()
    {
        // Warning 활성화 및 위치 설정
        if (warningObject)
        {
            // Warning 위치 설정 (보스의 현재 X)
            SetupWarning();
            warningObject.SetActive(true);
            
            // 깜빡임 효과
            yield return StartCoroutine(BlinkWarning());
            
            // Warning 비활성화
            warningObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(warningDuration);
        }

        if (!canAct || hp <= 0) yield break;

        // 돌진 실행
        yield return StartCoroutine(ChargeAttack());
    }

    /// <summary>
    /// Warning 오브젝트 위치 설정
    /// </summary>
    void SetupWarning()
    {
        if (!warningObject) return;

        // 보스의 현재 X 위치 저장 (돌진할 때 이 위치로)
        targetX = transform.position.x;
        
        // Warning의 X 위치를 보스에 맞춤
        Vector3 warningPos = warningObject.transform.position;
        warningObject.transform.position = new Vector3(targetX, warningPos.y, warningPos.z);
    }

    /// <summary>
    /// Warning 깜빡임 효과
    /// </summary>
    IEnumerator BlinkWarning()
    {
        if (!warningSr) yield break;

        float elapsed = 0f;
        float blinkSpeed = 15f;
        Color baseColor = warningSr.color;

        while (elapsed < warningDuration)
        {
            float alpha = (Mathf.Sin(elapsed * blinkSpeed) + 1f) * 0.5f;
            alpha = Mathf.Lerp(0.2f, 0.8f, alpha);

            Color c = baseColor;
            c.a = alpha;
            warningSr.color = c;

            elapsed += Time.deltaTime;
            yield return null;
        }

        warningSr.color = baseColor;
    }

    /// <summary>
    /// 세로 방향 돌진 공격
    /// </summary>
    IEnumerator ChargeAttack()
    {
        isCharging = true;

        Debug.Log("[Boss4] 돌진 시작!");

        // 돌진 실행 - Raycast로 충돌 감지하면서 이동
        while (isCharging && canAct && hp > 0)
        {
            // 이동량 계산
            float moveAmount = chargeSpeed * Time.deltaTime;
            
            // 아래로 Raycast 발사하여 Wall 체크
            Vector2 origin = transform.position;
            RaycastHit2D wallHit = Physics2D.Raycast(origin, Vector2.down, moveAmount + collisionCheckDistance, wallLayer);
            
            if (wallHit.collider != null)
            {
                // 벽에 닿음 - 벽 위치까지만 이동 후 멈춤
                transform.position = new Vector3(transform.position.x, wallHit.point.y + collisionCheckDistance, transform.position.z);
                Debug.Log("[Boss4] 벽과 충돌! 돌진 종료");
                isCharging = false;
                break;
            }
            
            // 플레이어 체크
            RaycastHit2D playerHit = Physics2D.Raycast(origin, Vector2.down, moveAmount + collisionCheckDistance, playerLayer);
            if (playerHit.collider != null)
            {
                Player playerComp = playerHit.collider.GetComponent<Player>();
                if (playerComp == null) playerComp = playerHit.collider.GetComponentInParent<Player>();
                
                if (playerComp != null)
                {
                    playerComp.TakeDamage(chargeDamage);
                    Debug.Log($"[Boss4] 플레이어에게 {chargeDamage} 대미지!");
                }
            }
            
            // 보스 주변 충돌 체크 (원형)
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, collisionCheckDistance);
            foreach (var hit in hits)
            {
                if (hit == myCollider) continue;
                
                // Wall 태그 체크
                if (hit.CompareTag("Wall"))
                {
                    Debug.Log("[Boss4] Wall 태그 충돌! 돌진 종료");
                    isCharging = false;
                    break;
                }
                
                // 플레이어 체크
                if (hit.CompareTag("Player"))
                {
                    Player playerComp = hit.GetComponent<Player>();
                    if (playerComp == null) playerComp = hit.GetComponentInParent<Player>();
                    
                    if (playerComp != null)
                    {
                        playerComp.TakeDamage(chargeDamage);
                        Debug.Log($"[Boss4] 플레이어에게 {chargeDamage} 대미지! (OverlapCircle)");
                    }
                }
            }
            
            if (!isCharging) break;
            
            // 아래로 이동
            transform.position += Vector3.down * moveAmount;
            
            yield return null;
        }

        isCharging = false;
        
        Debug.Log("[Boss4] 돌진 완료, 복귀 시작");

        // 원래 위치로 복귀
        yield return StartCoroutine(ReturnToOriginalPosition());
    }

    /// <summary>
    /// 원래 위치로 복귀 (페이드 아웃 → 이동 → 페이드 인)
    /// </summary>
    IEnumerator ReturnToOriginalPosition()
    {
        Debug.Log("[Boss4] 복귀 중...");

        // 잠시 대기
        yield return new WaitForSeconds(returnDelay);

        // 페이드 아웃
        yield return StartCoroutine(FadeOut());

        // 원래 위치로 이동
        transform.position = originalPosition;

        // 페이드 인
        yield return StartCoroutine(FadeIn());

        Debug.Log("[Boss4] 복귀 완료!");
    }

    /// <summary>
    /// 페이드 아웃
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
    /// 페이드 인
    /// </summary>
    IEnumerator FadeIn()
    {
        if (!sr) yield break;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            Color c = boss4OriginalColor;
            c.a = Mathf.Lerp(0f, 1f, t);
            sr.color = c;

            yield return null;
        }

        sr.color = boss4OriginalColor;
    }

    /// <summary>
    /// 충돌 감지 (백업용)
    /// </summary>
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Wall") && isCharging)
        {
            Debug.Log("[Boss4] OnCollision - 벽 충돌!");
            isCharging = false;
        }

        if (col.gameObject.CompareTag("Player"))
        {
            var playerComp = col.gameObject.GetComponent<Player>();
            if (playerComp)
            {
                int damage = isCharging ? chargeDamage : contactDamage;
                playerComp.TakeDamage(damage);
                Debug.Log($"[Boss4] OnCollision - 플레이어 {damage} 대미지!");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Wall") && isCharging)
        {
            Debug.Log("[Boss4] OnTrigger - 벽 충돌!");
            isCharging = false;
        }

        if (col.CompareTag("Player"))
        {
            var playerComp = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
            if (playerComp)
            {
                int damage = isCharging ? chargeDamage : contactDamage;
                playerComp.TakeDamage(damage);
                Debug.Log($"[Boss4] OnTrigger - 플레이어 {damage} 대미지!");
            }
        }
    }

    /// <summary>
    /// 피격 플래시 오버라이드 (페이드 중에도 색상 복원 처리)
    /// </summary>
    protected override IEnumerator HitFlashCoroutine()
    {
        if (sr)
        {
            Color prevColor = sr.color;
            sr.color = hitColor;
            yield return new WaitForSecondsRealtime(hitFlashDuration);
            sr.color = prevColor.a > 0.5f ? boss4OriginalColor : prevColor;
        }
    }

    /// <summary>
    /// 사망 처리 오버라이드
    /// </summary>
    protected override void OnDeath()
    {
        isCharging = false;
        Debug.Log("[Boss4] 처치됨!");
    }
}

