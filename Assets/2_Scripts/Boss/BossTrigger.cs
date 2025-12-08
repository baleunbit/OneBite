using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class BossTrigger : MonoBehaviour
{
    public BossRoot bossRoot;
    public Boss bossAI;
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
            bossBar = FindFirstObjectByType<BossBar>();

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

        // ===============================
        // 🔥 스테이지별 무기 대미지 처리 (복구됨)
        // ===============================
        var weaponManager = FindFirstObjectByType<WeaponManager>();
        if (weaponManager != null)
        {
            int stage = 1;
            string roomName = gameObject.name;

            if (!string.IsNullOrEmpty(roomName) && roomName.Length > 0)
                int.TryParse(roomName.Substring(0, 1), out stage);

            if (roomName.Contains("BossRoom"))
            {
                if (stage == 1)
                {
                    weaponManager.stage1WeaponDamage[0] = 12f;
                    weaponManager.stage1WeaponDamage[1] = 6f;
                    weaponManager.stage1WeaponDamage[2] = 6f;
                    weaponManager.ApplyStageRules(1);
                }
                else if (stage == 2)
                {
                    weaponManager.ApplyStageRules(2);
                }
                else if (stage == 3)
                {
                    weaponManager.ApplyStageRules(3);
                }
                else if (stage == 4)
                {
                    weaponManager.stage4WeaponDamage[0] = 6f;
                    weaponManager.stage4WeaponDamage[1] = 6f;
                    weaponManager.stage4WeaponDamage[2] = 12f;
                    weaponManager.ApplyStageRules(4);
                }
            }
        }
        // ===============================

        if (!entered)
        {
            entered = true;

            // 🔥 보스 바 활성화
            if (bossBar)
            {
                string bossName = bossAI ? bossAI.bossName : "BOSS";
                int maxHP = bossAI ? bossAI.maxHP : 500;
                bossBar.Show(bossName, maxHP);
            }

            // 🔥 등장
            if (bossRoot) bossRoot.StartAppear();

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
