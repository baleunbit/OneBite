using UnityEngine;
using System.Collections;

/// <summary>
/// 1스테이지 보스 - 총알 발사 패턴
/// BossBase를 상속받아 고유 패턴 구현
/// </summary>
public class Boss1 : BossBase
{
    [Header("Projectile Settings")]
    public GameObject bossBullet;
    public Transform[] firePoints;  // 발사 위치 배열 (최대 3개)
    public float bulletSpeed = 12f;
    public int bulletDamage = 5;

    [Header("발사 설정")]
    public int bulletsPerShot = 3;        // 한번에 발사하는 총알 수
    public float spreadAngle = 15f;       // 퍼지는 각도
    public float burstDelay = 0.15f;      // 연속 발사 간격

    [Header("분노 모드 (50% 이하)")]
    public int rageBulletsPerPoint = 3;   // 분노 시 각 포인트당 발사 수
    public bool useAllFirePoints = true;  // 모든 발사 포인트 사용
    
    // 1스테이지 보스는 50%부터 분노 모드
    protected override bool IsRageMode => HPRatio <= 0.5f;

    [Header("Boss Root (movement/pattern)")]
    public BossRoot bossRoot;

    //-------------------------------------------------------------------
    //  패턴 루틴 (BossBase 구현)
    //-------------------------------------------------------------------
    protected override IEnumerator PatternRoutine()
    {
        while (hp > 0 && canAct)
        {
            // 1) Idle 상태 --------------------------------------------------
            SetAttacking(false);  // Bool 기반

            if (bossRoot)
                bossRoot.isInfinity = false;

            yield return new WaitForSeconds(1.5f);

            // 2) Attack 준비 --------------------------------------------------
            if (bossRoot)
                bossRoot.isInfinity = true;

            yield return new WaitForSeconds(0.3f);

            // 3) Attack 모션 실행 ---------------------------------------------
            SetAttacking(true);  // Bool 기반

            yield return new WaitForSeconds(0.1f);
            Shoot();                            // 공격 타이밍에 따라 조절 가능

            // Attack 애니메이션 끝날 시간 기다리기
            yield return new WaitForSeconds(0.6f);

            // 4) 다시 Idle로 돌아가는 구간 ------------------------------------
            SetAttacking(false);  // Bool 기반
            
            if (bossRoot)
                bossRoot.isInfinity = true;

            yield return new WaitForSeconds(1.2f);
        }
    }
    
    /// <summary>
    /// 애니메이션 상태 설정 (Bool 파라미터 사용)
    /// </summary>
    void SetAttacking(bool isAttacking)
    {
        if (!anim) return;

        // BossAttack 파라미터 사용 (Bool)
        foreach (var param in anim.parameters)
        {
            if (param.name == "BossAttack" && param.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool("BossAttack", isAttacking);
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

    //-------------------------------------------------------------------
    //  🔥 보스 총알 발사 방식
    //-------------------------------------------------------------------
    void Shoot()
    {
        if (!bossBullet || firePoints == null || firePoints.Length == 0) return;

        if (IsRageMode && useAllFirePoints)
        {
            // 분노 모드: 한 방향 3발 + 전방향 발사
            StartCoroutine(ShootRageModeRoutine());
        }
        else
        {
            // 일반 모드: 플레이어 방향으로 일자 3발 연속 발사
            StartCoroutine(ShootNormalModeRoutine());
        }
    }

    IEnumerator ShootNormalModeRoutine()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) yield break;

        Transform fp = firePoints[0];
        if (!fp) yield break;

        // 3발 일자로 연속 발사 (같은 방향)
        for (int i = 0; i < bulletsPerShot; i++)
        {
            // 매번 플레이어 위치 갱신
            if (player)
            {
                Vector2 dir = (player.transform.position - fp.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                SpawnBullet(fp.position, dir, angle);
            }

            if (i < bulletsPerShot - 1)
                yield return new WaitForSeconds(burstDelay);
        }
    }

    IEnumerator ShootRageModeRoutine()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // 1) 먼저 한 방향으로 3발 발사
        if (player && firePoints.Length > 0 && firePoints[0])
        {
            Transform fp = firePoints[0];
            for (int i = 0; i < bulletsPerShot; i++)
            {
                if (player)
                {
                    Vector2 dir = (player.transform.position - fp.position).normalized;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    SpawnBullet(fp.position, dir, angle);
                }

                if (i < bulletsPerShot - 1)
                    yield return new WaitForSeconds(burstDelay);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // 2) 전방향 발사 (모든 발사 포인트에서)
        foreach (Transform fp in firePoints)
        {
            if (!fp) continue;

            // 각 포인트당 rageBulletsPerPoint개 발사 (전방향)
            float angleStep = 360f / rageBulletsPerPoint;
            for (int i = 0; i < rageBulletsPerPoint; i++)
            {
                float angle = i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                SpawnBullet(fp.position, dir, angle);
            }
        }
    }

    void SpawnBullet(Vector3 pos, Vector2 direction, float angle)
    {
        GameObject b = Instantiate(bossBullet, pos, Quaternion.Euler(0, 0, angle));

        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        if (rb == null) rb = b.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.linearVelocity = direction * bulletSpeed;

        Collider2D col = b.GetComponent<Collider2D>();
        if (col == null)
        {
            col = b.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
        }

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet == null)
            bullet = b.AddComponent<Bullet>();

        bullet.damage = bulletDamage;
        bullet.isPlayerBullet = false;
    }
}

