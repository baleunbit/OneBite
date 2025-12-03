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
    public float bulletSpeed = 6f;
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
            bossRoot.isInfinity = false;
            bossRoot.rotateEnabled = false;
            yield return new WaitForSeconds(1.5f);

            // 2) 공격 준비 (팔자 + 회전)
            bossRoot.isInfinity = true;
            bossRoot.rotateEnabled = true;
            yield return new WaitForSeconds(1.2f);

            // 3) 공격
            Shoot();
            yield return new WaitForSeconds(0.2f);

            // 4) 공격 후 팔자 유지
            bossRoot.isInfinity = true;
            bossRoot.rotateEnabled = false;
            yield return new WaitForSeconds(1.5f);
        }
    }

    void Shoot()
    {
        GameObject b = Instantiate(bossBullet, firePoint.position, firePoint.rotation);

        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = firePoint.right * bulletSpeed;

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet != null)
            bullet.damage = bulletDamage;
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
