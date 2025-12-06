using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BossBar : MonoBehaviour
{
    [Header("UI")]
    public Image hpFill;             // 빨간 HP 바
    public Image whiteFill;          // 흰색 바 (뒤늦게 따라감)
    public TextMeshProUGUI nameText; // 보스 이름

    [Header("화이트 바 설정")]
    public float whiteFollowSpeed = 1f;  // 따라가는 속도
    
    float targetFillAmount = 1f;

    void Update()
    {
        // 화이트 바가 빨간 바를 천천히 따라감
        if (whiteFill && whiteFill.fillAmount > targetFillAmount)
        {
            whiteFill.fillAmount = Mathf.MoveTowards(
                whiteFill.fillAmount, 
                targetFillAmount, 
                whiteFollowSpeed * Time.deltaTime
            );
        }
    }

    public void Show(string bossName, int maxHP)
    {
        if (nameText) nameText.text = bossName;
        if (hpFill) hpFill.fillAmount = 1f;
        if (whiteFill) whiteFill.fillAmount = 1f;
        targetFillAmount = 1f;
        gameObject.SetActive(true);
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        if (maxHP <= 0) return;
        
        targetFillAmount = (float)currentHP / maxHP;
        
        // 빨간 바는 즉시 감소
        if (hpFill) hpFill.fillAmount = targetFillAmount;
        
        // 화이트 바는 Update에서 천천히 따라감
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
