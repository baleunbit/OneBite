// ClearMgr.cs
using UnityEngine;
using System.Collections;

public class ClearMgr : MonoBehaviour
{
    [Header("설정")]
    public float checkDelay = 1.0f;      // 게임 시작 후 몇 초 뒤부터 검사 시작
    public float checkInterval = 0.5f;   // 검사 간격
    public float delayBeforeLoad = 1.5f; // 전멸 감지 후 엔드씬 전환까지 대기

    bool started;
    bool done;
    bool everSawMobs; // 한 번이라도 몹이 있었는지

    void Start()
    {
        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        yield return new WaitForSeconds(checkDelay);
        started = true;

        while (!done)
        {
            if (started)
            {
                var mobs = FindObjectsByType<Mob>(FindObjectsSortMode.None);

                // 한 번이라도 몹을 본 적이 있으면 플래그 세움
                if (mobs.Length > 0) everSawMobs = true;

                int alive = 0;
                foreach (var m in mobs)
                {
                    if (!m) continue;
                    if (m.IsAlive) alive++;
                }

                // 최소 한 번이라도 몹이 있었고, 현재 살아있는 몹이 0이면 클리어
                if (everSawMobs && alive == 0)
                {
                    done = true;
                    yield return new WaitForSeconds(delayBeforeLoad);
                    yield break;
                }
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
}
