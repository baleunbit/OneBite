// StageDirector.cs
// - 스테이지별 기믹 관리 (이동속도, 미끄러움, 화상 등)

using UnityEngine;

[DefaultExecutionOrder(-200)]
public class StageDirector : MonoBehaviour
{
    private static StageDirector _inst;
    public static StageDirector Instance
    {
        get
        {
            if (_inst) return _inst;
            _inst = FindFirstObjectByType<StageDirector>(FindObjectsInactive.Include);
            if (_inst) return _inst;
            var go = new GameObject("StageDirector");
            _inst = go.AddComponent<StageDirector>();
            return _inst;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDomain() { _inst = null; }
    
    // 게임 상태 완전 초기화 (메뉴로 돌아갈 때 호출)
    public static void ResetGameState()
    {
        if (_inst)
        {
            _inst.CurrentStage = 1;
            _inst.StopBurn();
        }
        Room.ResetStageCounter();
        Time.timeScale = 1f;
    }

    [Header("3스테이지 (미끄러움)")]
    [Tooltip("낮을수록 미끄러움 (0에 가까울수록 빙판)")]
    public float stage3_PlayerDrag = 0.05f;
    public float stage3_PlayerAngularDrag = 0.05f;

    [Header("4스테이지 (화상)")]
    public int stage4_BurnDamage = 1;           // 틱당 데미지
    public float stage4_BurnInterval = 5f;      // 데미지 간격 (초)

    public int CurrentStage { get; set; } = 1;
    
    // 화상 관련 변수
    private bool burnActive = false;
    private Transform burnTarget;
    private Room burnRoom;
    private float nextBurnTime;

    void Awake()
    {
        if (_inst && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        var room = FindRoomByPosition(player.transform.position);
        if (!room) return;
        ApplyStage(ParseStageFromName(room.gameObject.name), room.gameObject, player);
    }
    
    void Update()
    {
        // 4스테이지 화상 처리
        if (burnActive && burnTarget && burnRoom)
        {
            // 플레이어가 방 안에 있는지 체크
            if (!IsInsideRoom(burnTarget.position, burnRoom)) return;
            
            // 일정 간격으로 데미지
            if (Time.time >= nextBurnTime)
            {
                nextBurnTime = Time.time + stage4_BurnInterval;
                
                var player = burnTarget.GetComponent<Player>();
                if (player)
                {
                    player.TakeDamage(stage4_BurnDamage);
                }
            }
        }
    }

    public void ApplyStage(int stage, GameObject roomGO, GameObject playerGO)
    {
        if (stage <= 0) stage = 1;
        CurrentStage = stage;

        Debug.Log($"[StageDirector] ========== 스테이지 {stage} 진입 ==========");
        Debug.Log($"[StageDirector] 방: {roomGO.name}");

        // 무기/타입 규칙
        var wm = playerGO.GetComponent<WeaponManager>();
        if (wm) wm.ApplyStageRules(stage);

        // 상태 초기화
        Time.timeScale = 1f;
        var status = playerGO.GetComponent<PlayerStatusEffects>();
        if (!status) status = playerGO.AddComponent<PlayerStatusEffects>();
        status.ClearAll();
        
        // 화상 중지
        StopBurn();

        // 플레이어 속도 복원
        var player = playerGO.GetComponent<Player>();
        if (player)
        {
            player.SaveBaseMoveSpeed();  // 기본 속도 저장 (최초 1회)
            player.RestoreMoveSpeed();   // 원래 속도로 복원
            player.ResetSpeedModifier();
        }
        
        // 스테이지별 기믹
        switch (stage)
        {
            case 1:
            case 2:
                Debug.Log("[StageDirector] 기믹: 없음 (기본)");
                break;

            case 3:
                // 마찰력 감소로 미끄러짐
                status.SetSlippery(true, stage3_PlayerDrag, stage3_PlayerAngularDrag);
                Debug.Log($"[StageDirector] 기믹: 미끄러움 (Drag: {stage3_PlayerDrag}, AngularDrag: {stage3_PlayerAngularDrag})");
                break;

            case 4:
                // 화상 시작
                StartBurn(playerGO.transform, roomGO.GetComponent<Room>());
                Debug.Log($"[StageDirector] 기믹: 화상 (데미지: {stage4_BurnDamage}, 간격: {stage4_BurnInterval}초)");
                break;
        }
        
        Debug.Log($"[StageDirector] ==========================================");
    }
    
    // ===== 화상 관리 =====
    private void StartBurn(Transform target, Room room)
    {
        burnActive = true;
        burnTarget = target;
        burnRoom = room;
        nextBurnTime = Time.time + stage4_BurnInterval;
    }
    
    private void StopBurn()
    {
        burnActive = false;
        burnTarget = null;
        burnRoom = null;
    }
    
    private bool IsInsideRoom(Vector2 wp, Room room)
    {
        if (!room) return false;
        var cols = room.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) 
            if (c && c.OverlapPoint(wp)) return true;
        return false;
    }

    public static int ParseStageFromName(string roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return 1;
        int us = roomName.IndexOf('_');
        string head = us > 0 ? roomName[..us] : roomName;
        return int.TryParse(head, out int s) ? s : 1;
    }

    private Room FindRoomByPosition(Vector2 pos)
    {
        var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        Room best = null;
        float bestDist = float.MaxValue;

        foreach (var r in rooms)
        {
            if (!r) continue;
            var cols = r.GetComponentsInChildren<Collider2D>(true);
            foreach (var c in cols)
                if (c && c.OverlapPoint(pos)) return r;

            float d = ((Vector2)r.transform.position - pos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = r; }
        }
        return best;
    }
}
