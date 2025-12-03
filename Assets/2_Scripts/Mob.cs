using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Mob : MonoBehaviour
{
    [Header("이동/추격")] public float Speed = 7f;

    [Header("공격")] public int minDamage = 3; public int maxDamage = 5; public float attackCooldown = 1f;

    [Header("탐지")]
    public float detectRadius = 4f;
    public float viewDistance = 6f;
    [Range(0, 180)] public float fovAngle = 80f;

    [Header("회전 속도")]
    public float rotationSpeed = 360f;
    public float alertRotationSpeed = 60f;

    [Header("참조")]
    public Rigidbody2D target;
    [SerializeField] Animator anim;

    [Header("표식 프리팹")]
    public GameObject questionMarkPrefab;
    public GameObject exclamationMarkPrefab;

    [Header("마커 옵션")]
    public Vector2 markerOffset = new Vector2(0f, 0.9f);
    public float markerScale = 0.8f;
    public bool keepUpright = true;

    [Header("체력")] public int maxHP = 30;

    public bool IsAlerted => hasSpotted;
    public bool IsAlive => isLive;

    public Vector2 currentViewDirection = Vector2.up;
    public bool isSensing = false;

    Rigidbody2D rb; SpriteRenderer sr;
    GameObject _qm, _em;
    int currentHP;
    bool isLive = true;
    bool hasSpotted = false;
    float nextAttackTime = 0f;
    bool dealtThisFixed = false;

    int hashIsWalk, Attack;

    MobSenseVisualize sense;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHP = maxHP;
        sense = GetComponent<MobSenseVisualize>();

        // ★ 랜덤 시야 방향 (4방향 중 하나)
        Vector2[] dirs = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        currentViewDirection = dirs[Random.Range(0, dirs.Length)];

        // 시각화 시스템에도 초기 방향 전달
        if (sense) sense.currentForward = currentViewDirection;

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.GetComponent<Rigidbody2D>();
        }

        if (!anim) anim = GetComponentInChildren<Animator>(true);
        if (anim)
        {
            hashIsWalk = Animator.StringToHash("isWalk");
            Attack = Animator.StringToHash("doAttack");
        }

        if (questionMarkPrefab) _qm = Instantiate(questionMarkPrefab, transform);
        if (exclamationMarkPrefab) _em = Instantiate(exclamationMarkPrefab, transform);

        SetupMarker(_qm);
        SetupMarker(_em);

        ShowQuestion(false);
        ShowAlert(false);
    }

    public void RefreshSense()
    {
        if (sense != null) sense.ForceRedraw();
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

        if (!hasSpotted)
        {
            var patrol = GetComponent<MobPatrolAuto2D>();
            if (patrol) patrol.Tick();

            Vector2 toTarget = target.position - rb.position;
            float sqrDist = toTarget.sqrMagnitude;

            bool inDetectRange = sqrDist <= detectRadius * detectRadius;
            bool inViewRange = CanSeePlayerInFOV(sqrDist);

            if (inViewRange)
            {
                SetAlerted();
            }
            else if (inDetectRange)
            {
                if (!isSensing)
                {
                    isSensing = true;
                    ShowQuestion(true);
                }

                rb.linearVelocity = Vector2.zero;
                if (anim) anim.SetBool(hashIsWalk, false);
                return;
            }
            else
            {
                isSensing = false;
                ShowQuestion(false);
                ShowAlert(false);
                rb.linearVelocity = Vector2.zero;
                if (anim) anim.SetBool(hashIsWalk, false);
                return;
            }
        }

        // 추격
        sr.flipX = target.position.x < rb.position.x;

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

    void OnCollisionEnter2D(Collision2D c) => TryAttack(c.collider);
    void OnCollisionStay2D(Collision2D c) => TryAttack(c.collider);
    void OnTriggerEnter2D(Collider2D c) => TryAttack(c);
    void OnTriggerStay2D(Collider2D c) => TryAttack(c);

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

    bool CanSeePlayerInFOV(float sqrDist)
    {
        if (sqrDist > viewDistance * viewDistance)
            return false;

        Vector2 toTarget = (Vector2)target.position - rb.position;
        Vector2 dir = toTarget.normalized;

        float angle = Vector2.Angle(currentViewDirection, dir);
        return angle <= fovAngle * 0.5f;
    }

    void SetAlerted()
    {
        hasSpotted = true;
        isSensing = false;
        ShowQuestion(false);
        ShowAlert(true);
    }

    void ShowQuestion(bool on) { if (_qm) _qm.SetActive(on); }
    void ShowAlert(bool on) { if (_em) _em.SetActive(on); }

    void Die()
    {
        isLive = false;
        ShowAlert(false); ShowQuestion(false);
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        rb.simulated = false;
        Destroy(gameObject);
    }

    public void TakeDamage(int damage)
    {
        if (!isLive) return;

        currentHP -= Mathf.Max(1, damage);

        // 병아리 → 의심/발각 전환
        SetAlerted();

        if (currentHP <= 0)
            Die();
    }

    public void KillSilently()
    {
        if (!isLive) return;
        isLive = false;

        foreach (var c in GetComponentsInChildren<Collider2D>(true))
            if (c) c.enabled = false;

        if (rb) rb.simulated = false;

        Destroy(gameObject);
    }

    void SetupMarker(GameObject go)
    {
        if (!go) return;
        go.transform.localPosition = markerOffset;
        go.transform.localScale = Vector3.one * markerScale;
        if (keepUpright) go.transform.localRotation = Quaternion.identity;
    }

    void UpdateMarkerTransform(GameObject go)
    {
        if (!go) return;
        go.transform.localPosition = markerOffset;
        go.transform.localScale = Vector3.one * markerScale;
        if (keepUpright) go.transform.localRotation = Quaternion.identity;
    }
}