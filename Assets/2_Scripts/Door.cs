// Door.cs
using UnityEngine;

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
    [SerializeField] float clearCheckInterval = 0.25f;

    float startTime;
    bool requireExit;
    static float nextGlobalAllowedTime = 0f;
    Room ownerRoom;

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
    }

    void Update()
    {
        if (!openedOnce && IsRoomCleared(ownerRoom))
        {
            openedOnce = true;
            doorAnimator.SetTrigger("OpenDoor");
        }
    }

    bool IsRoomCleared(Room room)
    {
        if (!room) return false;

        var mobs = room.GetComponentsInChildren<Mob>(true);

        Debug.Log("[DoorCheck] Mob Count = " + mobs.Length);

        foreach (var m in mobs)
        {
            Debug.Log("[DoorCheck] Mob: " + m.name + " / IsAlive = " + m.IsAlive);

            if (m && m.IsAlive)
                return false;
        }

        return true;
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
            SceneMgr.I?.GoToEndScene();
            return;
        }

        // 이동
        player.position = nextRoomGO.transform.position + (Vector3)exitOffset;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

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
}
