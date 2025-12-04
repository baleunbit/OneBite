using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class BossTrigger : MonoBehaviour
{
    public BossRoot bossRoot;   // 보스 루트 스크립트
    public Boss bossAI;         // 보스 AI (Boss.cs)
    public BossBar bossBar;     // 체력바 UI

    [Header("카메라 줌 설정")]
    public CinemachineCamera cinemachineCamera;  // 시네머신 카메라
    public float bossRoomZoom = 50f;    // 보스 방 줌 (크게)
    public float normalZoom = 25f;       // 일반 줌
    public float zoomSpeed = 2f;         // 줌 전환 속도

    private bool triggered = false;
    bool entered = false;
    float targetZoom;

    void Start()
    {
        // 시네머신 카메라 자동 찾기
        if (!cinemachineCamera)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        
        targetZoom = normalZoom;
    }

    void Update()
    {
        // 부드러운 줌 전환
        if (cinemachineCamera)
        {
            float currentSize = cinemachineCamera.Lens.OrthographicSize;
            if (Mathf.Abs(currentSize - targetZoom) > 0.1f)
            {
                cinemachineCamera.Lens.OrthographicSize = Mathf.Lerp(currentSize, targetZoom, zoomSpeed * Time.deltaTime);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        if (!entered)
        {
            entered = true;
            Debug.Log("[BossTrigger] 플레이어 진입 - 보스 시작");

            // 1) 보스 바 켜기
            if (bossBar && bossAI) bossBar.Show(bossAI.bossName, bossAI.maxHP);

            // 2) 보스 등장 시작
            if (bossRoot) bossRoot.StartAppear();

            // 3) 보스 패턴 시작
            if (bossAI) bossAI.StartPattern();
        }

        // 카메라 줌 아웃 (보스 방)
        targetZoom = bossRoomZoom;
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        // 카메라 줌 인 (일반)
        targetZoom = normalZoom;
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
