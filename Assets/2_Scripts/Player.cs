using System;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("이동")] public float moveSpeed = 10f;
    [Header("체력")] public int maxHealth = 100; public int health = 100; public Image healthBarImage;

    [Header("경험치 / 레벨")]
    [SerializeField] private int level = 1;
    [SerializeField] private int exp = 0;

    [Header("강화 스탯")]
    public int weaponDamageBonus = 0;      // 무기 공격력 +X
    public float moveSpeedBonus = 0f;      // 이동속도 +X%
    public float biteRangeBonus = 0f;      // 한입 범위 증가
    public float quietStepBonus = 0f;      // 조용한 발걸음(적 ? 범위 감소)


    // Player 쪽에 상태 플래그
    public bool IsBusyWithBite { get; private set; }
    public void SetBiteState(bool on) => IsBusyWithBite = on;


    public int Level => level;
    public int Exp => exp;
    public int ExpToNext => GetExpToNext(level);

    public event Action<int, int, int> OnExpChanged;
    public event Action<int> OnLeveledUp;

    Rigidbody2D rb; SpriteRenderer spriter; Animator ani;
    Vector2 input; bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        ani = GetComponent<Animator>();

        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthBar();

        UIManager.Instance?.SetExpUI(level, exp, ExpToNext);
        OnExpChanged?.Invoke(level, exp, ExpToNext);
    }

    void Update()
    {
        if (isDead) return;

        // 🔥 바이트 중에는 입력 자체를 0으로
        if (IsBusyWithBite)
        {
            input = Vector2.zero;
            return;
        }

        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Bite 중이면 이동 멈추기 (입력은 유지됨)
        if (IsBusyWithBite)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Bite 끝나면 여기 코드 실행 → Held input으로 다시 움직임
        rb.linearVelocity = input * moveSpeed;
    }

    void LateUpdate()
    {
        ani?.SetFloat("Speed", input.sqrMagnitude);
        if (input.x > 0) spriter.flipX = false; else if (input.x < 0) spriter.flipX = true;
    }

    // ===== 체력 =====
    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        health = Mathf.Clamp(health - Mathf.Max(0, dmg), 0, maxHealth);
        UpdateHealthBar();
        if (health <= 0) Die();
    }
    public void DieFromHunger() { if (isDead) return; health = 0; UpdateHealthBar(); Die(); }
    void UpdateHealthBar() { if (healthBarImage) healthBarImage.fillAmount = (float)health / maxHealth; }
    void Die()
    {
        if (isDead) return;
        isDead = true; rb.linearVelocity = Vector2.zero; ani?.SetTrigger("Dead");
        UIManager.Instance?.ShowDiedPanel();
    }

    // ===== Bite로만 Exp 획득 =====
    public void AddExpFromBite(int amount = 1)
    {
        if (amount <= 0) return;
        exp += amount;

        while (exp >= ExpToNext)
        {
            exp -= ExpToNext;
            level++;
            OnLeveledUp?.Invoke(level);
            UIManager.Instance?.ShowLevelUpPanel();
        }
        UIManager.Instance?.SetExpUI(level, exp, ExpToNext);
        OnExpChanged?.Invoke(level, exp, ExpToNext);
    }

    // 1~3:6, 4~9:12, 10~14:15, 15+:18
    public int GetExpToNext(int lv)
    {
        if (lv <= 3) return 6;
        if (lv <= 9) return 12;
        if (lv <= 14) return 15;
        return 18;
    }

    public void ApplyLevelUpChoice(int choiceIndex)
    {
        switch (choiceIndex)
        {
            case 1:
                weaponDamageBonus += 2;
                Debug.Log("기본 무기 강화 (+2 damage)");
                break;

            case 2:
                quietStepBonus += 2f;
                ReduceMobDetectRadius(2f);
                Debug.Log("조용한 발걸음 (detectRadius -2)");
                break;

            case 3:
                moveSpeedBonus += moveSpeed * 0.05f;
                moveSpeed += moveSpeed * 0.05f;
                Debug.Log("이동속도 +5%");
                break;

            case 4:
                biteRangeBonus += 1f;
                var bite = GetComponent<Bite>();
                if (bite) bite.biteRange += 1f;
                Debug.Log("한입 범위 +1");
                break;
        }
        UIManager.Instance?.HideLevelUpPanel();
    }

    void ReduceMobDetectRadius(float amount)
    {
        Mob[] mobs = FindObjectsByType<Mob>(FindObjectsSortMode.None);

        foreach (var m in mobs)
        {
            if (!m) continue;

            // ? 표시 범위 감소
            m.detectRadius = Mathf.Max(0.1f, m.detectRadius - amount);

            // 발각 거리(!) 감소
            m.viewDistance = Mathf.Max(0.5f, m.viewDistance - amount);

            // 시야각도 줄여서 쉽게 못 봄 (예: -5°)
            m.fovAngle = Mathf.Clamp(m.fovAngle - (amount * 0.5f), 10f, 180f);
        }
    }

}