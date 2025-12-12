using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class Room : MonoBehaviour
{
    [SerializeField] public int roomID;
    Collider2D[] triggerInners;
    Collider2D[] solidColliders;
    Bounds aabb;
    Transform[] spawnPoints;
    bool mobsActivated = false;  // 방의 몹들이 이미 활성화되었는지
    bool stageUpdated = false;   // 스테이지 텍스트 업데이트 여부
    
    // 스테이지별 방문한 방 순서 추적 (static)
    static Dictionary<int, int> stageRoomCounter = new Dictionary<int, int>();
    static int lastVisitedStage = 0;
    static bool initialized = false;
    int myStage = 0;
    int myRoomIndex = 0;

    void Awake()
    {
        // 씬 로드 이벤트 등록 (한 번만)
        if (!initialized)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            initialized = true;
        }
        Init();
    }
    
    void Start()
    {
        // 몹 스폰 후에 체크하도록 딜레이
        StartCoroutine(DelayedCheckPlayer());
    }
    
    IEnumerator DelayedCheckPlayer()
    {
        // 몹 스폰 대기 (MobSpawner가 스폰할 시간)
        yield return new WaitForSeconds(1f);
        CheckPlayerAlreadyInRoom();
    }
    
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) Init(); }
#endif

    void Init()
    {
        var all = GetComponentsInChildren<Collider2D>(true);
        triggerInners = all.Where(c => c && c.isTrigger).ToArray();
        solidColliders = all.Where(c => c && !c.isTrigger).ToArray();

        Bounds? b = null;
        foreach (var c in all) { if (c == null) continue; b = b == null ? c.bounds : Enc(b.Value, c.bounds); }
        if (b == null)
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) { if (r == null) continue; b = b == null ? r.bounds : Enc(b.Value, r.bounds); }
        }
        aabb = b ?? new Bounds(transform.position, Vector3.one);

        spawnPoints = GetComponentsInChildren<SpawnPoint>(true).Select(s => s.transform).ToArray();
    }

    Bounds Enc(Bounds a, Bounds add) { a.Encapsulate(add); return a; }

    public Bounds AABB => aabb;
    public Transform[] SpawnPoints => spawnPoints;

    public bool ContainsForSpawnPoints(Vector2 wp)
    {
        if (triggerInners != null && triggerInners.Length > 0)
            return triggerInners.Any(t => t.OverlapPoint(wp));
        return true;
    }

    public bool ContainsForRandom(Vector2 wp)
    {
        if (triggerInners != null && triggerInners.Length > 0)
            return triggerInners.Any(t => t.OverlapPoint(wp));
        return false;
    }

    public Vector2 SnapInside(Vector2 wp)
    {
        var cols = (triggerInners != null && triggerInners.Length > 0) ? triggerInners : solidColliders;
        if (cols == null || cols.Length == 0) return wp;

        float best = float.MaxValue;
        Vector2 bestPt = wp;
        foreach (var c in cols)
        {
            if (c == null) continue;
            var cp = (Vector2)c.ClosestPoint(wp);
            float d = (cp - wp).sqrMagnitude;
            if (d < best) { best = d; bestPt = cp; }
        }
        return bestPt;
    }

    // 게임 시작 시 플레이어가 이미 방 안에 있는지 체크
    void CheckPlayerAlreadyInRoom()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        
        Vector2 playerPos = player.transform.position;
        bool playerInRoom = false;
        
        // 1. 트리거 콜라이더로 체크
        if (triggerInners != null && triggerInners.Length > 0)
        {
            foreach (var trigger in triggerInners)
            {
                if (trigger && trigger.OverlapPoint(playerPos))
                {
                    playerInRoom = true;
                    break;
                }
            }
        }
        // 2. 트리거가 없으면 AABB bounds로 체크
        else if (aabb.Contains(playerPos))
        {
            playerInRoom = true;
        }
        
        if (playerInRoom)
        {
            // 스테이지 텍스트 업데이트
            if (!stageUpdated)
            {
                UpdateStageText();
                stageUpdated = true;
            }
            
            if (!mobsActivated)
            {
                ActivateMobs();
            }
        }
    }
    
    // 플레이어가 방에 들어오면 몹들 활성화
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 스테이지 텍스트 업데이트 (한 번만)
        if (!stageUpdated)
        {
            UpdateStageText();
            stageUpdated = true;
        }
        
        if (mobsActivated) return;
        ActivateMobs();
    }
    
    void UpdateStageText()
    {
        // 방 이름에서 스테이지 번호 추출 (예: "1_ForestRoom" -> 1)
        myStage = ParseStageFromName();
        if (myStage <= 0) return;
        
        // 새 스테이지면 카운터 리셋
        if (myStage != lastVisitedStage)
        {
            if (!stageRoomCounter.ContainsKey(myStage))
                stageRoomCounter[myStage] = 0;
            lastVisitedStage = myStage;
        }
        
        // 방 번호 증가
        stageRoomCounter[myStage]++;
        myRoomIndex = stageRoomCounter[myStage];
        
        // UI 업데이트
        UIManager.Instance?.UpdateStageText(myStage, myRoomIndex);
    }
    
    int ParseStageFromName()
    {
        string roomName = gameObject.name;
        if (string.IsNullOrEmpty(roomName)) return -1;
        
        int under = roomName.IndexOf('_');
        if (under <= 0) return -1;
        
        var prefix = roomName.Substring(0, under);
        if (int.TryParse(prefix, out int stage)) return stage;
        return -1;
    }
    
    // 씬 로드 시 static 변수 리셋 (씬 전환마다 호출)
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetStageCounter();
    }
    
    // 게임 상태 초기화 (외부에서도 호출 가능)
    public static void ResetStageCounter()
    {
        stageRoomCounter.Clear();
        lastVisitedStage = 0;
    }
    
    // 도메인 리로드 시 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDomain()
    {
        stageRoomCounter.Clear();
        lastVisitedStage = 0;
        initialized = false;
    }

    void ActivateMobs()
    {
        mobsActivated = true;
        
        var mobs = GetComponentsInChildren<Mob>(true);
        
        foreach (var mob in mobs)
        {
            if (mob && mob.IsAlive)
            {
                mob.Activate();
            }
        }
    }
}
