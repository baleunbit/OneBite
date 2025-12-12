using UnityEngine;
using System.Collections;

/// <summary>
/// 모든 보스의 공통 기능을 담당하는 기본 클래스
/// 새로운 보스를 만들 때 이 클래스를 상속받아서 PatternRoutine()을 구현하세요
/// </summary>
public abstract class BossBase : MonoBehaviour
{
    [Header("Boss Status")]
    public int maxHP = 100;
    protected int hp;
    public int contactDamage = 3;

    [Header("Boss UI")]
    public BossBar bossBar;
    public string bossName = "BOSS";

    [Header("Animation")]
    public Animator anim;

    /// <summary>
    /// 보스가 행동할 수 있는지 여부
    /// </summary>
    public bool canAct = false;

    /// <summary>
    /// 현재 HP (읽기 전용)
    /// </summary>
    public int CurrentHP => hp;
    
    /// <summary>
    /// HP 비율 (0~1)
    /// </summary>
    public float HPRatio => (float)hp / maxHP;

    protected virtual void Start()
    {
        hp = maxHP;

        if (!bossBar)
            bossBar = FindFirstObjectByType<BossBar>(FindObjectsInactive.Include);

        if (!anim)
            anim = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// 보스 패턴 시작 (외부에서 호출)
    /// </summary>
    public virtual void StartPattern()
    {
        if (!canAct)
        {
            canAct = true;
            StartCoroutine(PatternRoutine());
        }
    }

    /// <summary>
    /// 각 보스가 구현해야 할 패턴 루틴
    /// </summary>
    protected abstract IEnumerator PatternRoutine();

    /// <summary>
    /// 데미지 처리
    /// </summary>
    public virtual void TakeDamage(int dmg)
    {
        hp -= dmg;

        if (bossBar != null)
            bossBar.UpdateHP(hp, maxHP);

        OnDamaged(dmg);

        if (hp <= 0)
            Die();
    }

    /// <summary>
    /// 데미지를 받았을 때 호출 (오버라이드용)
    /// </summary>
    protected virtual void OnDamaged(int dmg) { }

    /// <summary>
    /// 보스 사망 처리
    /// </summary>
    protected virtual void Die()
    {
        canAct = false;
        StopAllCoroutines();

        // 보스 바 숨기기
        if (bossBar != null)
            bossBar.Hide();

        OnDeath();

        Destroy(gameObject);
    }

    /// <summary>
    /// 사망 시 호출 (오버라이드용 - 아이템 드롭, 이펙트 등)
    /// </summary>
    protected virtual void OnDeath() { }

    /// <summary>
    /// 분노 모드 여부 (기본: 30% 이하)
    /// </summary>
    protected virtual bool IsRageMode => HPRatio <= 0.3f;
}

