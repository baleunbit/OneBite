// BurnDamageOverTime.cs (4스테이지 화상)

using UnityEngine;

public class BurnDamageOverTime : MonoBehaviour
{
    [Header("화상 설정")]
    [SerializeField] private int damagePerTick = 1;      // 틱당 데미지
    [SerializeField] private float tickInterval = 5f;  // 데미지 간격 (초)
    
    private Transform player;
    private Room room;
    private float nextTickTime;

    public void Init(Transform playerTr, int damage, float interval)
    {
        player = playerTr;
        damagePerTick = damage;
        tickInterval = interval;
        nextTickTime = Time.time + tickInterval;
    }
    
    // 기존 호환성 (dps 기반)
    public void Init(Transform playerTr, float dps)
    {
        player = playerTr;
        damagePerTick = Mathf.Max(1, Mathf.RoundToInt(dps * 5f)); // 0.5초 간격 기준
        tickInterval = 5f;
        nextTickTime = Time.time + tickInterval;
    }

    void Awake()
    {
        room = GetComponent<Room>() ?? GetComponentInParent<Room>();
    }

    void OnEnable()
    {
        nextTickTime = Time.time + tickInterval;
    }

    void Update()
    {
        if (!enabled || !player || damagePerTick <= 0 || room == null) return;
        if (!IsInsideRoom(player.position)) return;
        
        // 일정 간격으로 데미지
        if (Time.time >= nextTickTime)
        {
            nextTickTime = Time.time + tickInterval;
            
            var playerComp = player.GetComponent<Player>();
            if (playerComp)
            {
                playerComp.TakeDamage(damagePerTick);
            }
        }
    }

    private bool IsInsideRoom(Vector2 wp)
    {
        var cols = room.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) if (c && c.OverlapPoint(wp)) return true;
        return false;
    }
}
