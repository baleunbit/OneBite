using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 둔화 장판 - 플레이어가 위에 있으면 이동속도 감소
/// </summary>
public class SlowField : MonoBehaviour
{
    [Header("둔화 설정")]
    public float slowMultiplier = 0.5f;  // 이동속도 배율 (0.5 = 50% 감소)
    
    HashSet<Player> affectedPlayers = new HashSet<Player>();

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            var player = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
            if (player && !affectedPlayers.Contains(player))
            {
                affectedPlayers.Add(player);
                player.ApplySpeedModifier(slowMultiplier);
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            var player = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
            if (player && affectedPlayers.Contains(player))
            {
                affectedPlayers.Remove(player);
                player.RemoveSpeedModifier(slowMultiplier);
            }
        }
    }

    void OnDestroy()
    {
        // 장판 사라질 때 모든 영향받은 플레이어 속도 복구
        foreach (var player in affectedPlayers)
        {
            if (player) player.RemoveSpeedModifier(slowMultiplier);
        }
        affectedPlayers.Clear();
    }
}

