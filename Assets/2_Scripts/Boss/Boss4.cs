using UnityEngine;
using System.Collections;

/// <summary>
/// 4스테이지 보스 - 세로 방향 돌진 패턴
/// Warning으로 공격 경로 표시 후 아래로 돌진, Wall에 부딪히면 원래 위치로 복귀
/// </summary>
public class Boss4 : BossBase
{
    [Header("돌진 설정")]
    public float chargeSpeed = 50f;           // 돌진 속도 (빠르게!)
    public float chargeInterval = 3f;         // 돌진 사이 휴식 시간

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
    public float collisionCheckDistance = 0.5f; // 충돌 체크 거리
    
    [Header("비주얼 (실제 움직일 오브젝트)")]
    public Transform visualTransform;         // 비주얼 오브젝트 (없으면 자기 자신)

    // 내부 변수
    SpriteRenderer sr;
    SpriteRenderer warningSr;
    Collider2D myCollider;
    Color boss4OriginalColor;
    
    bool isCharging = false;
    Vector3 originalPosition;  // 원래 위치
    float targetX;             // 돌진할 X 위치
    
    // 실제 움직일 Transform (visualTransform이 있으면 그것, 없으면 자신)
    Transform MoveTarget => visualTransform ? visualTransform : transform;

    protected override void Start()
    {
        base.Start();

        // 비주얼에서 컴포넌트 찾기
        if (visualTransform)
        {
            sr = visualTransform.GetComponent<SpriteRenderer>();
            myCollider = visualTransform.GetComponent<Collider2D>();
            anim = visualTransform.GetComponent<Animator>();
        }
        
        // 없으면 자신/자식에서 찾기
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (!myCollider) myCollider = GetComponent<Collider2D>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        
        if (sr) boss4OriginalColor = sr.color;

        // 원래 위치 저장 (비주얼 기준)
        originalPosition = MoveTarget.position;

        // Warning 오브젝트 초기화 (처음엔 비활성화)
        if (warningObject)
        {
            warningSr = warningObject.GetComponent<SpriteRenderer>();
            warningObject.SetActive(false);
        }
        
        // 시작 전에는 애니메이터 비활성화 (StartPattern 전까지)
        if (anim) anim.enabled = false;
    }
    
    /// <summary>
    /// 보스 패턴 시작 (오버라이드)
    /// </summary>
    public override void StartPattern()
    {
        // 애니메이터 활성화
        if (anim) anim.enabled = true;
        
        base.StartPattern();
    }
    
    /// <summary>
    /// 애니메이션 상태 설정 (Bool 파라미터 사용)
    /// </summary>
    void SetAnimState(string stateName)
    {
        if (anim && anim.enabled)
        {
            bool isAttacking = (stateName == "Attack");
            anim.SetBool("IsAttacking", isAttacking);
            Debug.Log($"[Boss4] 애니메이션 IsAttacking: {isAttacking}");
        }
    }

    /// <summary>
    /// 보스 패턴 루틴 - Warning 표시 후 돌진 (반복)
    /// </summary>
    protected override IEnumerator PatternRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 시작 대기
        
        // 시작 시 Idle 상태
        SetAnimState("Idle");

        while (hp > 0 && canAct)
        {
            Debug.Log("[Boss4] 패턴 루프 시작, 대기 중...");
            
            // 1. 휴식 - Idle 애니메이션
            SetAnimState("Idle");
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
        // 1. 플레이어 X 위치 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            targetX = player.transform.position.x;
        }
        else
        {
            targetX = MoveTarget.position.x;
        }

        // 2. Warning 표시 (플레이어 X 위치에)
        if (warningObject)
        {
            SetupWarning();
            warningObject.SetActive(true);
        }

        // 3. 보스를 플레이어 X 위치 상단으로 즉시 이동
        MoveTarget.position = new Vector3(targetX, originalPosition.y, MoveTarget.position.z);

        // 4. Warning 깜빡임 (1초 대기)
        if (warningObject)
        {
            yield return StartCoroutine(BlinkWarning());
            warningObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(warningDuration);
        }

        if (!canAct || hp <= 0) yield break;

        // 5. 아래로 돌진!
        yield return StartCoroutine(ChargeAttack());
    }

    /// <summary>
    /// Warning 오브젝트 위치 설정
    /// </summary>
    void SetupWarning()
    {
        if (!warningObject) return;
        
        // Warning의 X 위치를 보스(플레이어 X)에 맞춤
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
        bool hasHitPlayer = false;  // 플레이어 한 번만 대미지

        Debug.Log("[Boss4] 돌진 시작!");
        
        // 돌진 애니메이션
        SetAnimState("Attack");

        // 돌진 실행 - 태그 기반 충돌 감지
        while (isCharging && canAct && hp > 0)
        {
            // 비주얼을 아래로 이동 (먼저 이동!)
            float moveAmount = chargeSpeed * Time.deltaTime;
            MoveTarget.position += Vector3.down * moveAmount;
            
            // 충돌 체크
            Collider2D[] hits = Physics2D.OverlapCircleAll(MoveTarget.position, collisionCheckDistance);
            foreach (var hit in hits)
            {
                if (hit == myCollider) continue;
                
                // Wall 충돌 → 돌진 종료
                if (hit.CompareTag("Wall"))
                {
                    Debug.Log("[Boss4] Wall 충돌! 돌진 종료");
                    isCharging = false;
                    break;
                }
                
                // 플레이어 충돌 → 한 번만 대미지
                if (!hasHitPlayer && hit.CompareTag("Player"))
                {
                    Player playerComp = hit.GetComponent<Player>() ?? hit.GetComponentInParent<Player>();
                    if (playerComp != null)
                    {
                        playerComp.TakeDamage(chargeDamage);
                        hasHitPlayer = true;
                        Debug.Log($"[Boss4] 플레이어에게 {chargeDamage} 대미지!");
                    }
                }
            }
            
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

        // 비주얼을 원래 위치로 이동
        MoveTarget.position = originalPosition;

        // 페이드 인
        yield return StartCoroutine(FadeIn());
        
        // 복귀 완료 후 Idle 애니메이션
        SetAnimState("Idle");

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

