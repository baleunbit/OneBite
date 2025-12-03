using UnityEngine;
using System.Collections;

public class BossRoot : MonoBehaviour
{
    [Header("스프라이트 자식 오브젝트")]
    public Transform visual;

    [Header("회전 연출")]
    public float rotateSpeed = 90f;
    public bool rotateEnabled = false;

    [Header("등장 연출")]
    public float appearHeight = 5f;
    public float appearSpeed = 1.0f;
    public Boss bossAI;

    [Header("이동 패턴")]
    public bool isInfinity = false;       // false = Idle, true = Infinity
    public float idleHeight = 0.5f;       // Idle 위/아래 이동 범위
    public float idleSpeed = 2f;          // Idle 이동 속도
    public float infinitySize = 1.0f;     // 팔자 이동 크기
    public float infinitySpeed = 2.0f;    // 팔자 속도

    public void StartAppear()
    {
        StartCoroutine(AppearRoutine());
    }

    public IEnumerator AppearRoutine()
    {
        bossAI.canAct = false;
        rotateEnabled = false;

        Vector3 startPos = transform.position + Vector3.up * appearHeight;
        Vector3 endPos = transform.position;

        transform.position = startPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * appearSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        rotateEnabled = true;
        bossAI.canAct = true;
    }

    void Update()
    {
        // ===========================
        // 1) 이동 처리
        // ===========================
        if (!isInfinity)
            MoveIdle();
        else
            MoveInfinity();

        // ===========================
        // 2) 회전 처리 (스프라이트만)
        // ===========================
        if (rotateEnabled && visual != null)
            visual.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }

    // ---- Idle 상하 이동 ----
    void MoveIdle()
    {
        float y = Mathf.Sin(Time.time * idleSpeed) * idleHeight;
        transform.localPosition = new Vector3(transform.localPosition.x, y, 0);
    }

    // ---- Infinity 팔자 이동 ----
    void MoveInfinity()
    {
        float t = Time.time * infinitySpeed;

        float x = Mathf.Sin(t) * infinitySize;
        float y = Mathf.Sin(t * 2f) * (infinitySize * 0.5f);

        transform.localPosition = new Vector3(x, y, 0);
    }
}
