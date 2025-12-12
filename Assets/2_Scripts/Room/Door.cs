// Door.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] RoomGenerator generator;
    [SerializeField] Transform player;

    [Header("동작")]
    [SerializeField] float activateDelay = 0.75f;
    [SerializeField] float reenterCooldown = 0.4f;
    [SerializeField] Vector2 exitOffset = new(0f, 0.75f);

    [Header("조건")]
    [SerializeField] bool requireClearRoom = true;
    [SerializeField] string blockedMessage = "아직 적이 남아 있어!";

    [Header("애니메이션")]
    [SerializeField] Animator doorAnimator;
    bool openedOnce = false;

    [SerializeField] Animator anim;

    float startTime;
    bool requireExit;
    static float nextGlobalAllowedTime = 0f;
    Room ownerRoom;
    
    [Header("문 열림 체크 딜레이")]
    [SerializeField] float doorCheckDelay = 1f;  // 몹 스폰 대기 시간
    bool canCheckDoor = false;

    void Awake()
    {
        startTime = Time.time;

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!generator) generator = FindFirstObjectByType<RoomGenerator>();
        ownerRoom = FindTopmostParentRoom(transform);

        if (!doorAnimator) doorAnimator = GetComponent<Animator>();
        ownerRoom = FindTopmostParentRoom(transform);
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 시작 후 딜레이가 지나야 문 열림 체크 시작 (몹 스폰 대기)
        if (!canCheckDoor)
        {
            if (Time.time - startTime >= doorCheckDelay)
                canCheckDoor = true;
            else
                return;
        }
        
        if (!openedOnce && IsRoomCleared(ownerRoom))
        {
            openedOnce = true;
            if (doorAnimator) doorAnimator.SetTrigger("OpenDoor");
            
            // 보스 방이 클리어되면 보스 바 숨기기
            if (ownerRoom && ownerRoom.gameObject.name.Contains("BossRoom"))
            {
                var bossBar = FindFirstObjectByType<BossBar>(FindObjectsInactive.Include);
                if (bossBar) bossBar.Hide();
            }
        }
    }

    bool IsRoomCleared(Room room)
    {
        if (!room) return false;

        // 방 안의 모든 자식 오브젝트 중 "Mob" 태그가 있는 것 찾기
        var children = room.GetComponentsInChildren<Transform>(true);
        int mobCount = 0;

        foreach (var child in children)
        {
            if (child.CompareTag("Mob") || child.CompareTag("Boss"))
            {
                mobCount++;
            }
        }

        return mobCount == 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!player || !generator) return;
        if (Time.time - startTime < activateDelay) return;
        if (Time.time < nextGlobalAllowedTime) return;
        if (requireExit) return;

        // 현재 방
        Room currentRoom = ownerRoom ? ownerRoom : FindRoomByPosition(player.position);
        if (!currentRoom) return;

        // 조건: 전멸 필요 시
        if (requireClearRoom && !IsRoomCleared(currentRoom))
        {
            Debug.Log(blockedMessage);
            return;
        }

        // 체인 인덱스
        int curIndex = generator.FindChainIndexByRoom(currentRoom);
        if (curIndex < 0) { Debug.LogWarning("[Door] 현재 방 인덱스를 찾지 못함"); return; }

        // 다음 방 조회
        var nextRoomGO = generator.GetChainedRoom(curIndex + 1);

        // 🔚 다음 방이 없으면 엔드씬
        if (!nextRoomGO)
        {
            Debug.Log("[Door] 다음 방 없음 → 엔드씬으로 전환");
            GoToEndScene();
            return;
        }

        // 이동
        Vector3 targetPos = nextRoomGO.transform.position + (Vector3)exitOffset;
        
        // 보스 방이면 Y좌표를 300으로 고정
        if (nextRoomGO.name.Contains("BossRoom"))
        {
            targetPos.y = 300f;
        }
        
        player.position = targetPos;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        // 🔥 새 방 진입 시 스테이지 규칙 적용
        var nextRoom = nextRoomGO.GetComponent<Room>();
        if (nextRoom)
        {
            int stage = StageDirector.ParseStageFromName(nextRoomGO.name);
            Debug.Log($"[Door] 방 이동: {currentRoom.name} -> {nextRoomGO.name}");
            Debug.Log($"[Door] ParseStageFromName 결과: {stage}");
            StageDirector.Instance?.ApplyStage(stage, nextRoomGO, player.gameObject);
        }
        else
        {
            Debug.LogWarning($"[Door] nextRoom 컴포넌트가 없음: {nextRoomGO.name}");
        }

        requireExit = true;
        nextGlobalAllowedTime = Time.time + reenterCooldown;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        requireExit = false;
    }

    // ── 유틸 ──
    Room FindTopmostParentRoom(Transform t)
    {
        Room found = null;
        Transform cur = t;
        while (cur)
        {
            var r = cur.GetComponent<Room>();
            if (r) found = r;
            cur = cur.parent;
        }
        return found;
    }

    Room FindRoomByPosition(Vector2 pos)
    {
        var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        Room best = null;
        float bestDist = float.MaxValue;

        foreach (var r in rooms)
        {
            if (!r) continue;

            // 포함 판정
            var cols = r.GetComponentsInChildren<Collider2D>(true);
            foreach (var c in cols)
            {
                if (c && c.OverlapPoint(pos))
                    return r;
            }

            // 가장 가까운 방
            float d = ((Vector2)r.transform.position - pos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = r; }
        }
        return best;
    }
    
    void GoToEndScene()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SoundManager.I?.PlayMenu();
        
        // SceneMgr가 있으면 사용, 없으면 직접 로드
        if (SceneMgr.I)
        {
            SceneMgr.I.GoToEndScene();
        }
        else
        {
            SceneManager.LoadScene("3_End");
        }
    }
}
