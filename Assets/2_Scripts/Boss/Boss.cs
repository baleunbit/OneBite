using UnityEngine;
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("���� ����")]
    public int maxHP = 300;
    int hp;

    public int contactDamage = 3;           // �̵� ���� �� ���� �����
    public int projectileDamage = 5;        // ����ü �����

    [Header("����ü ����")]
    public GameObject projectilePrefab;
    public Transform[] firePoints;          // �߻� ��ġ��
    public float shootInterval = 4f;        // ����ü ����
    public float projectileSpeed = 6f;      // (���� ����)

    public BossBar bossBar;
    public string bossName = "초콜릿 보스";
    public bool canAct = false;   // ← 보스 행동 가능 여부

    void Start()
    {
        hp = maxHP;
        StartCoroutine(ShootRoutine());
    }
    void Update()
    {
        if (!canAct) return;  // 연출 중엔 행동 금지

        // 평소 보스 행동 패턴
    }   
    public void StartPattern()
    {
        StartCoroutine(ShootRoutine());
    }

    IEnumerator ShootRoutine()
    {
        while (hp > 0)
        {
            Shoot();
            yield return new WaitForSeconds(shootInterval);
        }
    }

    void Shoot()
    {
        // 1) 투사체 생성
        GameObject proj = Instantiate(projectile, firePoint.position, firePoint.rotation);

        // 2) Rigidbody2D 로 속도 부여
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.right * projectileSpeed;   // Unity 6 기준
        }

        // 3) Bullet.cs에 데미지 전달
        Bullet b = proj.GetComponent<Bullet>();
        if (b != null)
        {
            b.damage = projectileDamage;   // public damage 이어야 함
        }
    }

    public void TakeDamage(int dmg)
    {
        Debug.Log($"[Boss] 피격됨! dmg={dmg}, hpBefore={hp}");

        hp -= dmg;

        Debug.Log($"[Boss] hpAfter={hp}");

        if (bossBar != null)
            bossBar.UpdateHP(hp, maxHP);

        if (hp <= 0)
        {
            Debug.Log("[Boss] 사망 함수 호출");
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
