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
    public Transform firePoint;
    public float bulletSpeed = 12f;  // 속도 증가
    public float shootInterval = 4f;

    [Header("Boss UI")]
    public BossBar bossBar;
    public string bossName = "BOSS";

    public bool canAct = false;

    // BossRoot(이동, 회전 담당)
    public BossRoot bossRoot;

    void Start()
    {
        hp = maxHP;
    }

    public void StartPattern()
    {
        canAct = true;
        StartCoroutine(PatternRoutine());
    }

    IEnumerator PatternRoutine()
    {
        while (hp > 0)
        {
            // 1) Idle(상하) 모드
            if (bossRoot)
            {
                bossRoot.isInfinity = false;
                bossRoot.rotateEnabled = false;
            }
            yield return new WaitForSeconds(1.5f);

            // 2) 공격 준비 (팔자 + 회전)
            if (bossRoot)
            {
                bossRoot.isInfinity = true;
                bossRoot.rotateEnabled = true;
            }
            yield return new WaitForSeconds(1.2f);

            // 3) 공격
            Shoot();
            yield return new WaitForSeconds(0.2f);

            // 4) 공격 후 팔자 유지
            if (bossRoot)
            {
                bossRoot.isInfinity = true;
                bossRoot.rotateEnabled = false;
            }
            yield return new WaitForSeconds(1.5f);
        }
    }

    void Shoot()
    {
        if (!bossBullet || !firePoint) return;
        
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        
        // 플레이어 방향 계산
        Vector2 direction = (player.transform.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 투사체 생성 (플레이어 방향으로 회전)
        GameObject b = Instantiate(bossBullet, firePoint.position, Quaternion.Euler(0, 0, angle));

        // Rigidbody2D 확인/추가
        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = b.AddComponent<Rigidbody2D>();
            Debug.Log("[Boss] 투사체에 Rigidbody2D 자동 추가됨");
        }
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * bulletSpeed;

        // Collider2D 확인/추가
        Collider2D col = b.GetComponent<Collider2D>();
        if (col == null)
        {
            col = b.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            Debug.Log("[Boss] 투사체에 CircleCollider2D 자동 추가됨");
        }

        // Bullet 컴포넌트 확인/추가
        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = b.AddComponent<Bullet>();
            Debug.Log("[Boss] 투사체에 Bullet 컴포넌트 자동 추가됨");
        }
        bullet.damage = bulletDamage;
        bullet.isPlayerBullet = false;  // 보스 총알 = 플레이어에게 데미지
    }

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
        Destroy(gameObject);
    }
}
