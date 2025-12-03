using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossBar : MonoBehaviour
{
    [Header("UI")]
    public Image bossFill;          // 실제 HP바
    public Image bossWhiteFill;     // 딜레이 필
    public TextMeshProUGUI bossText;

    float whiteDelaySpeed = 3f;

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void Show(string bossName, int maxHP)
    {
        gameObject.SetActive(true);
        bossText.text = bossName;

        bossFill.fillAmount = 1;
        bossWhiteFill.fillAmount = 1;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdateHP(int curHP, int maxHP)
    {
        float target = (float)curHP / maxHP;

        bossFill.fillAmount = target;

        StopAllCoroutines();
        StartCoroutine(WhiteFollow(target));
    }

    IEnumerator WhiteFollow(float target)
    {
        yield return new WaitForSeconds(0.1f);

        while (bossWhiteFill.fillAmount > target)
        {
            bossWhiteFill.fillAmount -= Time.deltaTime * whiteDelaySpeed;
            yield return null;
        }

        bossWhiteFill.fillAmount = target;
    }
}
