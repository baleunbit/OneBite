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
    public float bulletSpeed = 12f;
    public float shootInterval = 4f;

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
                bossRoot.rotateEnabled = false;
            }

            yield return new WaitForSeconds(1.5f);


            // 2) Attack 준비 --------------------------------------------------
            if (bossRoot)
            {
                bossRoot.isInfinity = true;
                bossRoot.rotateEnabled = true;
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
                bossRoot.rotateEnabled = false;
            }

            yield return new WaitForSeconds(1.2f);
        }
    }

    //-------------------------------------------------------------------
    //  🔥 보스 총알 발사 방식
    //-------------------------------------------------------------------
    void Shoot()
    {
        if (!bossBullet || !firePoint) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        Vector2 direction = (player.transform.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject b = Instantiate(bossBullet, firePoint.position, Quaternion.Euler(0, 0, angle));

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
        Destroy(gameObject);
    }
}
