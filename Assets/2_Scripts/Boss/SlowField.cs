using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 속도 장판 - 플레이어가 위에 있으면 이동속도 변경 + 색상 표시
/// Inspector에서 속도 배율과 색상을 자유롭게 조절 가능
/// </summary>
public class SlowField : MonoBehaviour
{
    [Header("속도 설정")]
    [Tooltip("이동속도 배율\n0.5 = 50% 감속 (슬로우)\n1.0 = 변화 없음\n1.5 = 50% 증가\n2.0 = 2배 빠르게")]
    [Range(0.1f, 3f)]
    public float speedMultiplier = 0.5f;  // 기본값: 슬로우 (50% 감속)
    
    [Header("시각 효과")]
    [Tooltip("장판 위에 있을 때 플레이어 색상")]
    public Color fieldColor = new Color(0.3f, 0.5f, 1f, 1f);  // 기본값: 파란색
    
    HashSet<Player> affectedPlayers = new HashSet<Player>();

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))   
        {
            var player = col.GetComponent<Player>() ?? col.GetComponentInParent<Player>();
            if (player && !affectedPlayers.Contains(player))
            {
                affectedPlayers.Add(player);
                player.ApplySpeedModifier(speedMultiplier);
                player.SetColorOverride(fieldColor);  // 색상 오버라이드 설정
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
                player.RemoveSpeedModifier(speedMultiplier);
                player.ClearColorOverride();  // 색상 오버라이드 해제
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
                player.RemoveSpeedModifier(speedMultiplier);
                player.ClearColorOverride();
            }
        }
        affectedPlayers.Clear();
    }
}

