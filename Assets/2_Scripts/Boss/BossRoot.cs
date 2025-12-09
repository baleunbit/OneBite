using UnityEngine;
using System.Collections;

public class BossRoot : MonoBehaviour
{
    [Header("스프라이트 자식 오브젝트")]
    public Transform visual;

    [Header("등장 설정")]
    public float appearHeight = 15f;  // 더 위에서 등장
    public float appearSpeed = 1.0f;
    public Boss bossAI;

    [Header("이동 설정")]
    public bool isInfinity = false;       // false = Idle, true = Infinity
    public float idleHeight = 0.5f;       // Idle 상하 이동 범위
    public float idleSpeed = 2f;          // Idle 이동 속도
    public float infinitySize = 1.0f;     // 무한 이동 크기
    public float infinitySpeed = 2.0f;    // 무한 속도

    Vector3 basePosition;  // 기준 위치 저장
    bool initialized = false;

    void Awake()
    {
        // 현재 위치를 기준점으로 저장
        basePosition = transform.position;
        initialized = true;
    }

    public void StartAppear()
    {
        StartCoroutine(AppearRoutine());
    }

    public IEnumerator AppearRoutine()
    {
        if (bossAI) bossAI.canAct = false;

        Vector3 startPos = basePosition + Vector3.up * appearHeight;
        Vector3 endPos = basePosition;

        transform.position = startPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * appearSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        if (bossAI) bossAI.canAct = true;
    }

    void Update()
    {
        if (!initialized) return;
        
        // ===========================
        // 1) 이동 처리 (기준 위치 + 오프셋)
        // ===========================
        if (!isInfinity)
            MoveIdle();
        else
            MoveInfinity();
    }

    // ---- Idle 상하 이동 ----
    void MoveIdle()
    {
        float y = Mathf.Sin(Time.time * idleSpeed) * idleHeight;
        transform.position = basePosition + new Vector3(0, y, 0);
    }

    // ---- Infinity 무한 이동 ----
    void MoveInfinity()
    {
        float t = Time.time * infinitySpeed;

        float x = Mathf.Sin(t) * infinitySize;
        float y = Mathf.Sin(t * 2f) * (infinitySize * 0.5f);

        transform.position = basePosition + new Vector3(x, y, 0);
    }
}
