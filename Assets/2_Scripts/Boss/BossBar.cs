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
    CanvasGroup canvasGroup;

    void Awake()
    {
        // CanvasGroup으로 숨기기 (GameObject는 활성화 유지)
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // 시작 시 숨김
        SetVisible(false);
    }

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
        SetVisible(true);
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
        SetVisible(false);
    }
    
    void SetVisible(bool visible)
    {
        if (canvasGroup)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
