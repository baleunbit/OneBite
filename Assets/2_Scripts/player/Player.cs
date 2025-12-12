using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class Player : MonoBehaviour
{
    [Header("이동")] public float moveSpeed = 10f;
    
    [Header("체력")]
    public int maxHealth = 100;
    public int health = 100;
    public Image healthBarImage;


    [Header("경험치 / 레벨")]
    [SerializeField] private int level = 1;
    [SerializeField] private int exp = 0;

    [Header("강화 스탯 (현재 값)")]
    public int weaponDamageBonus = 0;      // 무기 공격력 +X (레벨업으로 증가)
    public float moveSpeedBonus = 1f;      // 이동속도 +X%
    public float biteRangeBonus = 0.5f;    // 한입 범위 증가
    public float quietStepBonus = 1f;      // 조용한 발걸음(적 ? 범위 감소)
    
    [Header("강화 시 증가량 (Inspector에서 조절)")]
    public int weaponDamageUpgrade = 1;        // 무기 공격력 증가량
    public float biteRangeUpgrade = 1f;        // 한입 범위 증가량
    public float moveSpeedUpgradePercent = 0.05f;  // 이동속도 증가 비율 (5% = 0.05)
    public float quietStepUpgrade = 1f;        // 조용한 발걸음 증가량

    [Header("HP Follow")]
    public Image whiteHealthBar;
    public float whiteBarFollowSpeed = 1.5f; // 기존 4f → 1~2 추천

    [Header("피격 효과")]
    public Color hitColor = Color.red;       // 피격 시 색상
    public float hitFlashDuration = 0.1f;    // 빨간색 유지 시간

    [Header("카메라 흔들림")]
    public CinemachineImpulseSource impulseSource;  // Inspector에서 연결
    public float hitImpulseForce = 0.5f;            // 흔들림 강도


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
    Color originalColor;
    Coroutine hitFlashCoroutine;
    
    // 색상 오버라이드 (SlowField 등에서 사용)
    Color? colorOverride = null;
    
    // 속도 수정자 (SlowField 등에서 사용) - 직접 설정 방식
    float speedModifier = 1f;
    int slowFieldCount = 0;  // 현재 밟고 있는 SlowField 개수
    float currentSlowMultiplier = 1f;  // 현재 적용된 SlowField의 multiplier

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        ani = GetComponent<Animator>();

        // 원래 색상 저장
        if (spriter) originalColor = spriter.color;

        // Impulse Source 자동 탐색 (없으면 직접 연결 필요)
        if (!impulseSource)
            impulseSource = GetComponent<CinemachineImpulseSource>();

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
        rb.linearVelocity = input * moveSpeed * speedModifier;
    }

    void LateUpdate()
    {
        ani?.SetFloat("Speed", input.sqrMagnitude);
        if (input.x > 0) spriter.flipX = false; else if (input.x < 0) spriter.flipX = true;

        // === White HP bar delayed follow ===
        if (whiteHealthBar && healthBarImage)
        {
            float target = healthBarImage.fillAmount;

            if (whiteHealthBar.fillAmount > target)
            {
                whiteHealthBar.fillAmount =
                    Mathf.Lerp(whiteHealthBar.fillAmount, target, Time.deltaTime * whiteBarFollowSpeed);
            }
            else
            {
                whiteHealthBar.fillAmount = target;
            }
        }
    }

    // ===== 체력 =====
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        StartCoroutine(DamageOverFrames(dmg));

        // 피격 효과
        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlashCoroutine());

        if (impulseSource)
            impulseSource.GenerateImpulse(hitImpulseForce);
    }

    IEnumerator DamageOverFrames(int dmg)
    {
        int steps = dmg; // 1씩 나눔
        for (int i = 0; i < steps; i++)
        {
            health = Mathf.Max(health - 1, 0);
            UpdateHealthBar();
            yield return null; // 한 프레임
        }

        if (health <= 0) Die();
    }

    IEnumerator HitFlashCoroutine()
    {
        if (spriter)
        {
            spriter.color = hitColor;
            yield return new WaitForSeconds(hitFlashDuration);
            // 오버라이드 색상이 있으면 그 색상으로, 없으면 원래 색상으로
            spriter.color = colorOverride ?? originalColor;
        }
    }
    
    // ===== 색상 오버라이드 (SlowField 등) =====
    public void SetColorOverride(Color color)
    {
        colorOverride = color;
        if (spriter) spriter.color = color;
    }
    
    public void ClearColorOverride()
    {
        colorOverride = null;
        if (spriter) spriter.color = originalColor;
    }
    
    public bool HasColorOverride() => colorOverride.HasValue;
    public void DieFromHunger() { if (isDead) return; health = 0; UpdateHealthBar(); Die(); }
    void UpdateHealthBar()
    {
        if (healthBarImage)
            healthBarImage.fillAmount = (float)health / maxHealth;
        Debug.Log($"[Player] 체력: {health}/{maxHealth}");
    }

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
                weaponDamageBonus += weaponDamageUpgrade;
                Debug.Log($"기본 무기 강화 (+{weaponDamageUpgrade} damage)");
                break;

            case 2:
                biteRangeBonus += biteRangeUpgrade;
                var bite = GetComponent<Bite>();
                if (bite) bite.biteRange += biteRangeUpgrade;
                Debug.Log($"한입 범위 +{biteRangeUpgrade}");
                break;

            case 3:
                float speedIncrease = moveSpeed * moveSpeedUpgradePercent;
                moveSpeedBonus += speedIncrease;
                moveSpeed += speedIncrease;
                Debug.Log($"이동속도 +{moveSpeedUpgradePercent * 100}%");
                break;

            case 4:
                quietStepBonus += quietStepUpgrade;
                ReduceMobDetectRadius(quietStepUpgrade);
                Debug.Log($"조용한 발걸음 (detectRadius -{quietStepUpgrade})");
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

            // 파란 원 감소
            m.detectRadius = Mathf.Max(0.1f, m.detectRadius - amount);

            // 노란 시야 거리 감소
            m.viewDistance = Mathf.Max(0.5f, m.viewDistance - amount);

            // 시야각도 감소
            m.fovAngle = Mathf.Clamp(m.fovAngle - (amount * 0.5f), 10f, 180f);
        }
    }

    // ===== 속도 관리 =====
    private float baseMoveSpeed;
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public void RestoreMoveSpeed()
    {
        if (baseMoveSpeed > 0)
            moveSpeed = baseMoveSpeed;
    }
    
    public void SaveBaseMoveSpeed()
    {
        if (baseMoveSpeed <= 0)
            baseMoveSpeed = moveSpeed;
    }
    
    public float GetBaseMoveSpeed() => baseMoveSpeed > 0 ? baseMoveSpeed : moveSpeed;

    public void ResetSpeedModifier()
    {
        speedModifier = 1f;
        slowFieldCount = 0;
        currentSlowMultiplier = 1f;
    }
    
    public float GetSpeedModifier() => speedModifier;
    
    // 속도 수정자 (SlowField 등에서 사용) - 새로운 방식
    public void ApplySpeedModifier(float multiplier)
    {
        slowFieldCount++;
        currentSlowMultiplier = multiplier;
        speedModifier = multiplier;
        Debug.Log($"[Player] SlowField 진입! count: {slowFieldCount}, speedModifier: {speedModifier}");
    }

    public void RemoveSpeedModifier(float multiplier)
    {
        slowFieldCount = Mathf.Max(0, slowFieldCount - 1);
        
        // 모든 SlowField에서 나왔으면 속도 복구
        if (slowFieldCount <= 0)
        {
            speedModifier = 1f;
            currentSlowMultiplier = 1f;
            slowFieldCount = 0;
        }
        Debug.Log($"[Player] SlowField 이탈! count: {slowFieldCount}, speedModifier: {speedModifier}");
    }
}