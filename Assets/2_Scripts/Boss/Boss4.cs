using UnityEngine;
using System.Collections;

/// <summary>
/// 4스테이지 보스 - 세로 방향 돌진 패턴 (상태 기반)
/// </summary>
public class Boss4 : BossBase
{
    // 보스 상태 정의
    public enum BossState { Idle, Warning, Attacking, Fading }
    public BossState currentState = BossState.Idle;

    [Header("돌진 설정")]
    public float chargeSpeed = 100f;
    public float chargeInterval = 3f;         // Idle 대기 시간

    [Header("돌진 대미지")]
    public int chargeDamage = 10;

    [Header("Warning 설정")]
    public GameObject warningObject;
    public float warningDuration = 1f;

    [Header("복귀 설정")]
    public float fadeOutDuration = 0.3f;
    public float fadeInDuration = 0.5f;
    public float returnDelay = 0.5f;

    [Header("충돌 설정")]
    public float collisionCheckDistance = 0.5f;
    public int maxHitsPerCharge = 3;          // 돌진 중 최대 피격 횟수
    public float hitCooldown = 0.3f;          // 피격 간 쿨타임

    [Header("비주얼")]
    public Transform visualTransform;

    // 내부 변수
    SpriteRenderer sr;
    SpriteRenderer warningSr;
    Collider2D myCollider;
    Color boss4OriginalColor;
    Vector3 originalPosition;
    float targetX;
    int hitCount;
    float lastHitTime;

    Transform MoveTarget => visualTransform ? visualTransform : transform;

    protected override void Start()
    {
        base.Start();

        if (visualTransform)
        {
            sr = visualTransform.GetComponent<SpriteRenderer>();
            myCollider = visualTransform.GetComponent<Collider2D>();
            anim = visualTransform.GetComponent<Animator>();
        }

        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (!myCollider) myCollider = GetComponent<Collider2D>();
        if (!anim) anim = GetComponentInChildren<Animator>();

        if (sr) boss4OriginalColor = sr.color;
        originalPosition = MoveTarget.position;

        if (warningObject)
        {
            warningSr = warningObject.GetComponent<SpriteRenderer>();
            warningObject.SetActive(false);
        }
    }

    /// <summary>
    /// 메인 패턴 루틴 - 상태 순환
    /// </summary>
    protected override IEnumerator PatternRoutine()
    {
        while (hp > 0 && canAct)
        {
            // 1. IDLE 상태 - 대기
            yield return StartCoroutine(State_Idle());

            if (!canAct || hp <= 0) break;

            // 2. WARNING 상태 - 경고 1초
            yield return StartCoroutine(State_Warning());

            if (!canAct || hp <= 0) break;

            // 3. ATTACKING 상태 - 돌진
            yield return StartCoroutine(State_Attacking());

            if (!canAct || hp <= 0) break;

            // 4. FADING 상태 - 페이드 아웃/인 + 복귀
            yield return StartCoroutine(State_Fading());
        }
    }

    // ==================== 상태별 처리 ====================

    /// <summary>
    /// IDLE 상태 - 대기
    /// </summary>
    IEnumerator State_Idle()
    {
        currentState = BossState.Idle;
        SetAnim("Idle");
        Debug.Log("[Boss4] 상태: IDLE");

        yield return new WaitForSeconds(chargeInterval);
    }

    /// <summary>
    /// WARNING 상태 - 경고 표시 1초
    /// </summary>
    IEnumerator State_Warning()
    {
        currentState = BossState.Warning;
        SetAnim("Warning");
        Debug.Log("[Boss4] 상태: WARNING");

        // 플레이어 X 위치 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        targetX = player ? player.transform.position.x : MoveTarget.position.x;

        // 보스를 플레이어 X 위치 상단으로 이동
        MoveTarget.position = new Vector3(targetX, originalPosition.y, MoveTarget.position.z);

        // Warning 표시
        if (warningObject)
        {
            Vector3 warnPos = warningObject.transform.position;
            warningObject.transform.position = new Vector3(targetX, warnPos.y, warnPos.z);
            warningObject.SetActive(true);

            // 깜빡임
            float elapsed = 0f;
            Color baseColor = warningSr ? warningSr.color : Color.red;

            while (elapsed < warningDuration)
            {
                if (warningSr)
                {
                    float alpha = (Mathf.Sin(elapsed * 15f) + 1f) * 0.5f;
                    alpha = Mathf.Lerp(0.2f, 0.8f, alpha);
                    Color c = baseColor;
                    c.a = alpha;
                    warningSr.color = c;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (warningSr) warningSr.color = baseColor;
            warningObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(warningDuration);
        }
    }

    /// <summary>
    /// ATTACKING 상태 - 아래로 돌진 (최대 3회 피격)
    /// </summary>
    IEnumerator State_Attacking()
    {
        currentState = BossState.Attacking;
        SetAnim("Attack");
        hitCount = 0;
        lastHitTime = -999f;
        Debug.Log("[Boss4] 상태: ATTACKING");

        bool charging = true;

        while (charging && canAct && hp > 0)
        {
            // 아래로 이동
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
                    Debug.Log("[Boss4] Wall 충돌!");
                    charging = false;
                    break;
                }

                // 플레이어 충돌 → 최대 3회, 쿨타임 적용
                if (hitCount < maxHitsPerCharge && hit.CompareTag("Player"))
                {
                    if (Time.time - lastHitTime >= hitCooldown)
                    {
                        Player p = hit.GetComponent<Player>() ?? hit.GetComponentInParent<Player>();
                        if (p != null)
                        {
                            p.TakeDamage(chargeDamage);
                            hitCount++;
                            lastHitTime = Time.time;
                            Debug.Log($"[Boss4] 플레이어 {chargeDamage} 대미지! ({hitCount}/{maxHitsPerCharge})");
                        }
                    }
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// FADING 상태 - 페이드 아웃 → 복귀 → 페이드 인
    /// </summary>
    IEnumerator State_Fading()
    {
        currentState = BossState.Fading;
        SetAnim("Fade");
        Debug.Log("[Boss4] 상태: FADING");

        yield return new WaitForSeconds(returnDelay);

        // 페이드 아웃
        if (sr)
        {
            float t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
                sr.color = c;
                yield return null;
            }
        }

        // 원래 위치로 복귀
        MoveTarget.position = originalPosition;

        // 페이드 인
        if (sr)
        {
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                Color c = boss4OriginalColor;
                c.a = Mathf.Lerp(0f, 1f, t / fadeInDuration);
                sr.color = c;
                yield return null;
            }
            sr.color = boss4OriginalColor;
        }

        Debug.Log("[Boss4] 복귀 완료!");
    }

    // ==================== 애니메이션 ====================

    void SetAnim(string state)
    {
        if (!anim) return;

        bool isAttacking = (state == "Attack");

        // BossAttack 파라미터 사용 (Bool)
        foreach (var param in anim.parameters)
        {
            if (param.name == "BossAttack" && param.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool("BossAttack", isAttacking);
                Debug.Log($"[Boss4] 애니메이션 BossAttack: {isAttacking}");
                return;
            }
        }

        // 파라미터 없으면 직접 Play
        if (isAttacking)
        {
            anim.Play("BossAttack");
        }
        else
        {
            anim.Play("BossIdle");
        }
    }

    // ==================== 충돌 (백업) ====================

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Player p = col.gameObject.GetComponent<Player>();
            if (p != null)
            {
                int dmg = currentState == BossState.Attacking ? chargeDamage : contactDamage;
                p.TakeDamage(dmg);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            Player p = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
            if (p != null)
            {
                int dmg = currentState == BossState.Attacking ? chargeDamage : contactDamage;
                p.TakeDamage(dmg);
            }
        }
    }

    // ==================== 오버라이드 ====================

    protected override void OnDeath()
    {
        Debug.Log("[Boss4] 처치됨!");
    }
}
