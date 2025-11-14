using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Mob : MonoBehaviour
{
    [Header("이동/추격")] public float Speed = 7f;

    [Header("공격")] public int minDamage = 3; public int maxDamage = 5; public float attackCooldown = 1f;

    [Header("탐지")]
    public float detectRadius = 4f;          // ? 표시 범위
    public float viewDistance = 6f;          // 발각 판단 거리
    [Range(0, 180)] public float fovAngle = 80f;

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

    Rigidbody2D rb; SpriteRenderer sr;
    int hashIsWalk, Attack;

    // 내부에서만 관리하는 마커 인스턴스
    GameObject _qm, _em;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHP = Mathf.Max(1, maxHP);

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.GetComponent<Rigidbody2D>();
        }

        if (!anim) anim = GetComponentInChildren<Animator>(true);
        if (anim) { hashIsWalk = Animator.StringToHash("isWalk"); Attack = Animator.StringToHash("doAttack"); }

        // 프리팹에서만 생성
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

        // 감지/발각
        if (!hasSpotted)
        {
            float sqr = (target.position - rb.position).sqrMagnitude;
            bool inProximity = sqr <= detectRadius * detectRadius; // ?
            bool inFov = InFovAndVisible();                        // !

            if (inFov) SetAlerted();
            else { ShowQuestion(inProximity); ShowAlert(false); }

            if (!hasSpotted)
            {
                rb.linearVelocity = Vector2.zero;
                if (anim) anim.SetBool(hashIsWalk, false);
                return;
            }
        }

        // 추격 이동
        Vector2 cur = rb.position;
        Vector2 dir = ((Vector2)target.position - cur).normalized;
        rb.MovePosition(cur + dir * Speed * Time.fixedDeltaTime);

        sr.flipX = target.position.x < rb.position.x;
        if (anim) anim.SetBool(hashIsWalk, true);
        rb.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        // 위치/크기 유지
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

    bool InFovAndVisible()
    {
        Vector2 myPos = rb.position;
        Vector2 to = target.position - myPos;
        float dist = to.magnitude;
        if (dist > viewDistance) return false;

        Vector2 forward = (sr && sr.flipX) ? Vector2.left : Vector2.right;
        float ang = Vector2.Angle(forward, to.normalized);
        if (ang > (fovAngle * 0.5f)) return false;

        // 필요 시 Linecast로 가림 처리 추가
        return true;
    }

    void SetAlerted()
    {
        hasSpotted = true;
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

        EatBar.Instance?.AddFromEat(1);
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
