using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 둔화 장판 - 플레이어가 위에 있으면 이동속도 감소 + 파란색 표시
/// </summary>
public class SlowField : MonoBehaviour
{
    [Header("둔화 설정")]
    public float slowMultiplier = 0.5f;  // 이동속도 배율 (0.5 = 50% 감소)
    
    [Header("시각 효과")]
    public Color slowColor = new Color(0.3f, 0.5f, 1f, 1f);  // 파란색
    
    Dictionary<Player, Color> originalColors = new Dictionary<Player, Color>();
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
                
                // 스프라이트 색상 변경
                var sr = player.GetComponentInChildren<SpriteRenderer>();
                if (sr)
                {
                    originalColors[player] = sr.color;
                    sr.color = slowColor;
                }
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
                
                // 스프라이트 색상 복구
                var sr = player.GetComponentInChildren<SpriteRenderer>();
                if (sr && originalColors.TryGetValue(player, out Color origColor))
                {
                    sr.color = origColor;
                    originalColors.Remove(player);
                }
            }
        }
    }

    void OnDestroy()
    {
        // 장판 사라질 때 모든 영향받은 플레이어 복구
        foreach (var player in affectedPlayers)
        {
            if (player)
            {
                player.RemoveSpeedModifier(slowMultiplier);
                
                var sr = player.GetComponentInChildren<SpriteRenderer>();
                if (sr && originalColors.TryGetValue(player, out Color origColor))
                {
                    sr.color = origColor;
                }
            }
        }
        affectedPlayers.Clear();
        originalColors.Clear();
    }
}

