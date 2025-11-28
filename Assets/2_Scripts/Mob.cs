using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Mob : MonoBehaviour
{
    [Header("이동/추격")] public float Speed = 7f;

    [Header("공격")] public int minDamage = 3; public int maxDamage = 5; public float attackCooldown = 1f;

    [Header("탐지")]
    public float detectRadius = 4f;       // 경계 시작 범위 (부채꼴 회전 시작)
    public float viewDistance = 6f;       // 부채꼴 길이
    [Range(0, 180)] public float fovAngle = 80f;

    [Header("시야 회전")]
    public float rotationSpeed = 360f; // 초당 회전 각도 (도/초)
    [HideInInspector] public Vector2 currentViewDirection = Vector2.up; // 현재 부채꼴이 바라보는 방향

    [Header("참조")] public Rigidbody2D target; [SerializeField] Animator anim;

    [Header("표식 프리팹(자식 오브젝트는 안 씀)")]
    public GameObject questionMarkPrefab;
    public GameObject exclamationMarkPrefab;

    [Header("마커 표시 옵션")]
    public Vector2 markerOffset = new Vector2(0f, 0.9f);
    public float markerScale = 0.8f;
    public bool keepUpright = true;

    [Header("체력")] public int maxHP = 30;

    [Header("SFX")] public AudioClip hitSfx; [Range(0f, 1f)] public float hitSfxVolume = 0.8f;
    public AudioClip deathSfx; [Range(0f, 1f)] public float deathSfxVolume = 1f;

    public bool IsAlerted => hasSpotted;
    public bool IsAlive => isLive;

    int currentHP; bool isLive = true; bool hasSpotted = false;
    float nextAttackTime = 0f; bool dealtThisFixed = false;
    public bool isSensing = false; // 경계 상태 변수 추가

    Rigidbody2D rb; SpriteRenderer sr;
    int hashIsWalk, Attack;

    // 내부에서만 관리하는 마커 인스턴스
    GameObject _qm, _em;

    MobSenseVisualize mobSenseVisualize;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        mobSenseVisualize = GetComponent<MobSenseVisualize>();
        currentHP = Mathf.Max(1, maxHP);
        
        currentViewDirection = Vector2.up;

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.GetComponent<Rigidbody2D>();
        }

        if (!anim) anim = GetComponentInChildren<Animator>(true);
        if (anim) { hashIsWalk = Animator.StringToHash("isWalk"); Attack = Animator.StringToHash("doAttack"); }

        if (questionMarkPrefab) _qm = Instantiate(questionMarkPrefab, transform);
        if (exclamationMarkPrefab) _em = Instantiate(exclamationMarkPrefab, transform);

        SetupMarker(_qm);
        SetupMarker(_em);

        ShowQuestion(false);
        ShowAlert(false);
    }

    void FixedUpdate()
    {
        dealtThisFixed = false;

        if (!isLive || !target)
        {
            rb.linearVelocity = Vector2.zero;
            if (anim) anim.SetBool(hashIsWalk, false);
            return;
        }

        // 1. 발각 여부 체크 (hasSpotted == false)
        if (!hasSpotted)
        {
            Vector2 toTarget = target.position - rb.position;
            float sqrDist = toTarget.sqrMagnitude;
            
            bool inDetectRange = sqrDist <= detectRadius * detectRadius;
            bool inViewRange = CanSeePlayerInFOV(sqrDist); // 회전 중인 부채꼴에 닿았는지 체크

            // 1-1. FOV에 닿으면 최종 발각 (Alerted)
            if (inViewRange) 
            {
                SetAlerted(); // hasSpotted = true, '!' 마커 표시
            }
            // 1-2. detectRadius 안에 들어왔으면 경계 (Sensing) 시작
            else if (inDetectRange) 
            {
                if (!isSensing)
                {
                    isSensing = true; // 경계 상태로 전환: MobSenseVisualize가 회전 시작
                    ShowQuestion(true); // '?' 마커 표시
                }
                // 경계 상태일 때: 정지 상태 유지 (바라보지 않음, 추격 안 함)
                rb.linearVelocity = Vector2.zero;
                if (anim) anim.SetBool(hashIsWalk, false);
                return;
            }
            // 1-3. 모든 범위 밖이면 대기 (Idle)
            else
            {
                isSensing = false; // 경계 상태 해제: MobSenseVisualize가 고정 방향(위)으로 돌아옴
                ShowQuestion(false);
                ShowAlert(false);
                rb.linearVelocity = Vector2.zero;
                if (anim) anim.SetBool(hashIsWalk, false);
                return;
            }
        }
        
        // 2. 발각된 상태 (hasSpotted == true) 일 때만 추격 로직 실행

        // 플레이어 위치로 자연스럽게 바라보기 (좌우만) 
        sr.flipX = target.position.x < rb.position.x;

        // 추격 이동
        Vector2 cur = rb.position;
        Vector2 dir = ((Vector2)target.position - cur).normalized;
        rb.MovePosition(cur + dir * Speed * Time.fixedDeltaTime);

        if (anim) anim.SetBool(hashIsWalk, true);
        rb.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        UpdateMarkerTransform(_qm);
        UpdateMarkerTransform(_em);
    }

    void OnCollisionEnter2D(Collision2D c) { TryAttack(c.collider); }
    void OnCollisionStay2D(Collision2D c) { TryAttack(c.collider); }
    void OnTriggerEnter2D(Collider2D c) { TryAttack(c); }
    void OnTriggerStay2D(Collider2D c) { TryAttack(c); }

    void TryAttack(Collider2D col)
    {
        if (!isLive || !hasSpotted) return;
        if (!col || !col.CompareTag("Player")) return;
        if (Time.time < nextAttackTime) return;
        if (dealtThisFixed) return;

        var player = col.GetComponentInParent<Player>();
        if (!player) return;

        if (anim) anim.SetTrigger(Attack);

        int dmg = Random.Range(minDamage, maxDamage + 1);
        player.TakeDamage(dmg);

        nextAttackTime = Time.time + attackCooldown;
        dealtThisFixed = true;
    }

    // ────────────────────────── FOV 감지 ──────────────────────────
    bool CanSeePlayerInFOV(float sqrDist)
    {
        // 1. 거리 체크: viewDistance (6m) 내에 들어왔는지 확인
        if (sqrDist > viewDistance * viewDistance)
            return false;

        // 2. 각도 체크: 시야 부채꼴 안에 들어왔는지 확인
        Vector2 toTarget = (Vector2)target.position - rb.position;
        Vector2 directionToTarget = toTarget.normalized;
        
        // MobSenseVisualize에서 부드럽게 회전 중인 시야 방향 사용
        Vector2 viewDir = currentViewDirection; 
        
        float angleToTarget = Vector2.Angle(viewDir, directionToTarget);

        if (angleToTarget <= fovAngle * 0.5f)
        {
            return true;
        }

        return false;
    }
    // ───────────────────────────────────────────────────────────────────────

    void SetAlerted()
    {
        hasSpotted = true;
        isSensing = false;
        ShowQuestion(false);
        ShowAlert(true);
    }

    public void TakeDamage(int damage)
    {
        if (!isLive) return;
        if (hitSfx) AudioSource.PlayClipAtPoint(hitSfx, transform.position, hitSfxVolume);
        currentHP -= Mathf.Max(1, damage);
        SetAlerted();
        if (currentHP <= 0) Die();
    }

    public void KillSilently()
    {
        if (!isLive) return;
        isLive = false;
        foreach (var c in GetComponentsInChildren<Collider2D>(true)) if (c) c.enabled = false;
        if (rb) rb.simulated = false;
        Destroy(gameObject);
    }

    void Die()
    {
        if (!isLive) return;
        isLive = false;

        // EatBar.Instance?.AddFromEat(1); 
        if (deathSfx) AudioSource.PlayClipAtPoint(deathSfx, transform.position, deathSfxVolume);
        ShowQuestion(false); ShowAlert(false);
        foreach (var c in GetComponentsInChildren<Collider2D>(true)) if (c) c.enabled = false;
        if (rb) rb.simulated = false;
        Destroy(gameObject);
    }

    // ── 마커 유틸 ─────────────────────────
    void ShowQuestion(bool on) { if (_qm && _qm.activeSelf != on) _qm.SetActive(on); }
    void ShowAlert(bool on) { if (_em && _em.activeSelf != on) _em.SetActive(on); }

    void SetupMarker(GameObject go)
    {
        if (!go) return;
        go.transform.SetParent(transform, worldPositionStays: true);
        go.transform.localPosition = (Vector3)markerOffset;
        go.transform.localScale = Vector3.one * Mathf.Abs(markerScale);
        if (keepUpright) go.transform.localRotation = Quaternion.identity;

        var mSr = go.GetComponent<SpriteRenderer>();
        var meSr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
        if (mSr && meSr) { mSr.sortingLayerID = meSr.sortingLayerID; mSr.sortingOrder = meSr.sortingOrder + 1; }
    }

    void UpdateMarkerTransform(GameObject go)
    {
        if (!go) return;
        go.transform.localPosition = (Vector3)markerOffset;
        go.transform.localScale = Vector3.one * Mathf.Abs(markerScale);
        if (keepUpright) go.transform.localRotation = Quaternion.identity;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
#endif
}