using UnityEngine;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    public BossRoot bossRoot;   // 보스 연출 스크립트
    public Boss bossAI;         // 보스 AI (Boss.cs)
    public BossBar bossBar;     // 체력바 UI

    private bool triggered = false;
    bool entered = false;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (entered) return;
        if (!col.CompareTag("Player")) return;

        entered = true;

        Debug.Log("[BossTrigger] 플레이어 보스 방 입장 → 보스 시작");

        // 1) 보스 바 켜기
        bossBar.Show(bossAI.bossName, bossAI.maxHP);

        // 2) 등장 연출 시작
        bossRoot.StartAppear();

        // 3) 보스 패턴 시작
        bossAI.StartPattern();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTrigger(other);
    }

    void TryTrigger(Collider2D other)
    {
        if (triggered) return;                    // 이미 발동했으면 무시
        if (!other.CompareTag("Player")) return;  // 플레이어 아니면 무시

        triggered = true;

        Debug.Log("[BossTrigger] 플레이어 보스룸 감지 → 보스 시작");

        // 체력바 표시
        bossBar.Show(bossAI.bossName, bossAI.maxHP);

        // 보스 등장 연출
        bossRoot.StartAppear();

        // 등장 연출 이후 패턴 시작
        StartCoroutine(StartBossAfterDelay());
    }

    IEnumerator StartBossAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        bossAI.StartPattern();
    }
}
