using UnityEngine;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    public BossRoot bossRoot;  // 보스 등장 연출 담당
    public Boss bossAI;        // 보스 공격/패턴 담당
    public BossBar bossBar;    // 보스 HP UI 담당

    bool isTriggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;
        if (!other.CompareTag("Player")) return;

        isTriggered = true;

        // UI 켜기
        bossBar.Show(bossAI.bossName, bossAI.maxHP);

        // 등장 연출 시작
        bossRoot.StartAppear();

        // 등장 연출 후 AI 시작
        StartCoroutine(StartBossAfterDelay());
    }

    IEnumerator StartBossAfterDelay()
    {
        // TimeScale = 0일 때도 작동하도록
        yield return new WaitForSecondsRealtime(1.5f);
        bossAI.StartPattern();   // 너가 Boss.cs 안에 StartPattern() 만들면 여기가 실행됨
    }
}
