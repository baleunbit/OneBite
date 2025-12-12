using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class BossTrigger : MonoBehaviour
{
    public BossRoot bossRoot;
    public BossBase bossAI;  // Boss, Boss3 등 모든 보스 할당 가능
    public BossBar bossBar;

    [Header("카메라 줌 설정")]
    public CinemachineCamera cinemachineCamera;
    public float bossRoomZoom = 70f;
    public float normalZoom = 25f;
    public float zoomSpeed = 2f;

    bool entered = false;
    float targetZoom;

    void Start()
    {
        if (!cinemachineCamera)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (!bossBar)
            bossBar = FindFirstObjectByType<BossBar>(FindObjectsInactive.Include);

        targetZoom = normalZoom;
    }

    void Update()
    {
        if (cinemachineCamera)
        {
            float currentSize = cinemachineCamera.Lens.OrthographicSize;
            if (Mathf.Abs(currentSize - targetZoom) > 0.1f)
            {
                cinemachineCamera.Lens.OrthographicSize =
                    Mathf.Lerp(currentSize, targetZoom, zoomSpeed * Time.deltaTime);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        // 스테이지 번호 파싱
        int stage = 1;
        string roomName = gameObject.name;
        if (!string.IsNullOrEmpty(roomName) && roomName.Length > 0)
            int.TryParse(roomName.Substring(0, 1), out stage);

        // ===============================
        // 🔥 스테이지별 무기 대미지 처리 (복구됨)
        // ===============================
        var weaponManager = FindFirstObjectByType<WeaponManager>();
        if (weaponManager != null)
        {
            if (roomName.Contains("BossRoom"))
            {
                // 1스테이지 보스룸: 포크 강화 (3/1 그대로)
                if (stage == 1)
                {
                    weaponManager.ApplyStageRules(1);
                }
                // 4스테이지 보스룸: 젓가락 강화 (3/1 그대로)
                else if (stage == 4)
                {
                    // 4스테이지 보스룸만 젓가락 강화로 변경
                    weaponManager.stage4WeaponDamage[0] = 1f;
                    weaponManager.stage4WeaponDamage[1] = 1f;
                    weaponManager.stage4WeaponDamage[2] = 3f;  // 젓가락 강화
                    weaponManager.ApplyStageRules(4);
                }
                // 그 외 보스룸: 해당 스테이지 규칙 그대로 적용
                else
                {
                    weaponManager.ApplyStageRules(stage);
                }
            }
        }
        // ===============================

        if (!entered)
        {
            entered = true;
            Debug.Log("[BossTrigger] 플레이어 진입! 보스 시작");

            // 🔥 보스 바 활성화
            if (bossBar)
            {
                string bossName = bossAI ? bossAI.bossName : "BOSS";
                int maxHP = bossAI ? bossAI.maxHP : 500;
                bossBar.Show(bossName, maxHP);
            }
            else
            {
                Debug.LogWarning("[BossTrigger] bossBar가 null입니다!");
            }

            // 🔥 등장 연출 (4스테이지 보스는 등장 연출 없음)
            if (bossRoot && stage != 4) 
            {
                bossRoot.StartAppear();
            }

            // 🔥 패턴 시작
            if (bossAI) bossAI.StartPattern();
        }

        targetZoom = bossRoomZoom;
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        targetZoom = normalZoom;
    }
}
