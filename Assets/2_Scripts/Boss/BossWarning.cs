using UnityEngine;
using System.Collections;

/// <summary>
/// 보스 공격 경로를 표시하는 Warning 오브젝트
/// 깜빡이다가 사라짐
/// </summary>
public class BossWarning : MonoBehaviour
{
    [Header("깜빡임 설정")]
    public float blinkDuration = 2f;        // 총 깜빡이는 시간
    public float blinkSpeed = 10f;          // 깜빡이는 속도 (높을수록 빠름)
    public Color warningColor = new Color(1f, 0f, 0f, 0.5f);  // 경고 색상

    SpriteRenderer sr;
    
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = gameObject.AddComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Warning 시작 - 깜빡이다가 자동 삭제
    /// </summary>
    public void StartWarning(float duration = -1f)
    {
        if (duration > 0) blinkDuration = duration;
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        float elapsed = 0f;
        
        if (sr) sr.color = warningColor;

        while (elapsed < blinkDuration)
        {
            // 깜빡임 효과 (sin 함수로 알파값 조절)
            float alpha = (Mathf.Sin(elapsed * blinkSpeed) + 1f) * 0.5f;
            alpha = Mathf.Lerp(0.2f, 0.8f, alpha);  // 0.2 ~ 0.8 사이로 깜빡임
            
            if (sr)
            {
                Color c = warningColor;
                c.a = alpha;
                sr.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 깜빡임 끝나면 삭제
        Destroy(gameObject);
    }

    /// <summary>
    /// Warning 크기와 회전 설정 (보스 → 벽 방향)
    /// </summary>
    public void SetupLine(Vector2 start, Vector2 end, float width = 1f)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 위치: 시작점과 끝점의 중간
        transform.position = (start + end) / 2f;
        
        // 회전
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 크기: 길이 x 너비
        transform.localScale = new Vector3(distance, width, 1f);
    }
}

