using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BossBar : MonoBehaviour
{
    [Header("UI")]
    public Image hpFill;             // 채워질 HP 바 이미지
    public TextMeshProUGUI nameText; // 보스 이름

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void Show(string bossName, int maxHP)
    {
        nameText.text = bossName;
        hpFill.fillAmount = 1f;
        gameObject.SetActive(true);
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        hpFill.fillAmount = (float)currentHP / maxHP;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
