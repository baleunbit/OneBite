using System.Linq;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Room : MonoBehaviour
{
    [SerializeField] public int roomID;
    Collider2D[] triggerInners;
    Collider2D[] solidColliders;
    Bounds aabb;
    Transform[] spawnPoints;
    bool mobsActivated = false;  // 방의 몹들이 이미 활성화되었는지

    void Awake() => Init();
    
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
        if (mobsActivated) return;
        
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        
        Vector2 playerPos = player.transform.position;
        
        // 1. 트리거 콜라이더로 체크
        if (triggerInners != null && triggerInners.Length > 0)
        {
            foreach (var trigger in triggerInners)
            {
                if (trigger && trigger.OverlapPoint(playerPos))
                {
                    ActivateMobs();
                    return;
                }
            }
        }
        
        // 2. 트리거가 없으면 AABB bounds로 체크
        if (aabb.Contains(playerPos))
        {
            ActivateMobs();
        }
    }
    
    // 플레이어가 방에 들어오면 몹들 활성화
    void OnTriggerEnter2D(Collider2D other)
    {
        if (mobsActivated) return;
        if (!other.CompareTag("Player")) return;

        ActivateMobs();
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
