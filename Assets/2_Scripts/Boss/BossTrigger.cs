using UnityEngine;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    public BossRoot bossRoot;   // ���� ���� ��ũ��Ʈ
    public Boss bossAI;         // ���� AI (Boss.cs)
    public BossBar bossBar;     // ü�¹� UI

    private bool triggered = false;
    bool entered = false;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (entered) return;
        if (!col.CompareTag("Player")) return;

        entered = true;

        Debug.Log("[BossTrigger] 플레이어 진입 - 보스 시작");

        // 1) 보스 바 켜기
        if (bossBar && bossAI) bossBar.Show(bossAI.bossName, bossAI.maxHP);

        // 2) 보스 등장 시작
        if (bossRoot) bossRoot.StartAppear();

        // 3) 보스 패턴 시작
        if (bossAI) bossAI.StartPattern();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTrigger(other);
    }

    void TryTrigger(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        Debug.Log("[BossTrigger] 플레이어 감지 - 보스 시작");

        // 체력바 표시
        if (bossBar && bossAI) bossBar.Show(bossAI.bossName, bossAI.maxHP);

        // 보스 등장 시작
        if (bossRoot) bossRoot.StartAppear();

        // 보스 패턴 시작 (딜레이)
        StartCoroutine(StartBossAfterDelay());
    }

    IEnumerator StartBossAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        if (bossAI) bossAI.StartPattern();
    }
}
