using UnityEngine;
using System.Collections;

public class BossRoot : MonoBehaviour
{
    [Header("스프라이트 자식 오브젝트")]
    public Transform visual;        // 보스 그래픽

    [Header("회전 연출")]
    public float rotateSpeed = 90f; // 보스 조각 회전 속도
    public bool rotateEnabled = false; // 등장 이후에만 돌기

    [Header("등장 연출")]
    public float appearHeight = 5f; // 얼마나 위에서 떨어질지
    public float appearSpeed = 1.0f; // 내려오는 속도
    public Boss bossAI;

    // BossTrigger에서 호출될 함수
    public void StartAppear()
    {
        StartCoroutine(AppearRoutine());
    }

    public IEnumerator AppearRoutine()
    {
        // 1) 시작할 때 보스 액션 OFF
        bossAI.canAct = false;
        rotateEnabled = false;

        Vector3 startPos = transform.position + Vector3.up * appearHeight;
        Vector3 endPos = transform.position;

        transform.position = startPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * appearSpeed;  // TimeScale 사용하지 않음
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // 2) 등장 끝 → 보스 액션 ON
        rotateEnabled = true;
        bossAI.canAct = true;
    }


    void Update()
    {
        if (rotateEnabled && visual != null)
            visual.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
}
