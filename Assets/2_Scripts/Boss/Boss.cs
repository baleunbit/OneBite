using UnityEngine;
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("Boss Status")]
    public int maxHP = 300;
    int hp;

    public int contactDamage = 3;
    public int bulletDamage = 5;

    [Header("Projectile Settings")]
    public GameObject bossBullet;
    public Transform[] firePoints;  // 발사 위치 배열 (최대 3개)
    public float bulletSpeed = 12f;
    public float shootInterval = 4f;
    
    [Header("발사 설정")]
    public int bulletsPerShot = 3;        // 한번에 발사하는 총알 수
    public float spreadAngle = 15f;       // 퍼지는 각도
    
    [Header("분노 모드 (30% 이하)")]
    public int rageBulletsPerPoint = 3;   // 분노 시 각 포인트당 발사 수
    public bool useAllFirePoints = true;  // 모든 발사 포인트 사용

    [Header("Boss UI")]
    public BossBar bossBar;
    public string bossName = "BOSS";

    public bool canAct = false;

    [Header("Boss Root (movement/pattern)")]
    public BossRoot bossRoot;

    [Header("Animation")]
    public Animator anim;
    // Animator에 반드시 Bool/Trigger:
    // Trigger : BossAttack

    void Start()
    {
        hp = maxHP;

        if (!bossBar)
            bossBar = FindFirstObjectByType<BossBar>();

        if (!anim)
            anim = GetComponentInChildren<Animator>();
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
        while (hp > 0)
        {
            // 1) Idle 상태 --------------------------------------------------
            anim.ResetTrigger("BossAttack");   // 혹시 남은 trigger 제거
            anim.Play("1_BossIdle");

            if (bossRoot)
            {
                bossRoot.isInfinity = false;
            }

            yield return new WaitForSeconds(1.5f);


            // 2) Attack 준비 --------------------------------------------------
            if (bossRoot)
            {
                bossRoot.isInfinity = true;
            }

            yield return new WaitForSeconds(0.3f);


            // 3) Attack 모션 실행 ---------------------------------------------
            anim.SetTrigger("BossAttack");

            yield return new WaitForSeconds(0.1f);
            Shoot();                            // 공격 타이밍에 따라 조절 가능

            // Attack 애니메이션 끝날 시간 기다리기
            yield return new WaitForSeconds(0.6f);


            // 4) 다시 Idle로 돌아가는 구간 ------------------------------------
            if (bossRoot)
            {
                bossRoot.isInfinity = true;
            }

            yield return new WaitForSeconds(1.2f);
        }
    }

    //-------------------------------------------------------------------
    //  🔥 보스 총알 발사 방식
    //-------------------------------------------------------------------
    void Shoot()
    {
        if (!bossBullet || firePoints == null || firePoints.Length == 0) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        // 분노 모드 체크 (30% 이하)
        bool isRageMode = (float)hp / maxHP <= 0.3f;
        
        if (isRageMode && useAllFirePoints)
        {
            // 분노 모드: 모든 발사 포인트에서 전방향 발사
            ShootRageMode();
        }
        else
        {
            // 일반 모드: 첫 번째 발사 포인트에서 플레이어 방향으로 3발
            ShootNormalMode(player.transform.position);
        }
    }
    
    void ShootNormalMode(Vector3 targetPos)
    {
        Transform fp = firePoints[0];
        if (!fp) return;
        
        Vector2 baseDir = (targetPos - fp.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        
        // 3발 퍼지면서 발사
        for (int i = 0; i < bulletsPerShot; i++)
        {
            float offset = (i - (bulletsPerShot - 1) / 2f) * spreadAngle;
            float angle = baseAngle + offset;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            
            SpawnBullet(fp.position, dir, angle);
        }
    }
    
    void ShootRageMode()
    {
        // 모든 발사 포인트에서 전방향으로 발사
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

    //-------------------------------------------------------------------
    //  🔥 데미지 처리
    //-------------------------------------------------------------------
    public void TakeDamage(int dmg)
    {
        hp -= dmg;

        if (bossBar != null)
            bossBar.UpdateHP(hp, maxHP);

        if (hp <= 0)
            Die();
    }

    void Die()
    {
        canAct = false;
        StopAllCoroutines();
        
        // 보스 바 숨기기
        if (bossBar != null)
            bossBar.Hide();
        
        Destroy(gameObject);
    }
}
