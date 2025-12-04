using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BossBar : MonoBehaviour
{
    [Header("UI")]
    public Image hpFill;             // ä���� HP �� �̹���
    public TextMeshProUGUI nameText; // ���� �̸�

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void Show(string bossName, int maxHP)
    {
        if (nameText) nameText.text = bossName;
        if (hpFill) hpFill.fillAmount = 1f;
        gameObject.SetActive(true);
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        if (hpFill && maxHP > 0)
            hpFill.fillAmount = (float)currentHP / maxHP;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
