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
        if (firePoints.Length == 0) return;

        foreach (var p in firePoints)
        {
            GameObject proj = Instantiate(projectilePrefab, p.position, p.rotation);
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();

            if (rb)
                rb.linearVelocity = p.right * projectileSpeed;
        }
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
