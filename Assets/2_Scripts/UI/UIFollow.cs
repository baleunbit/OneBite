// UIFollowTarget.cs
// - 체력/경험치 같이 "플레이어 따라다니는" UI를 겹치지 않게 쌓아 올리기용
// - 같은 타겟(플레이어)에 여러 바를 붙일 때 orderIndex만 다르게 주면 자동 간격 유지
// - Canvas가 Screen Space(Overlay/Camera)면 스크린 좌표로, World Space면 월드 좌표로 붙음

using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIFollow : MonoBehaviour
{
    public Transform target;                // 따라갈 대상(플레이어)
    public Vector3 worldOffset = new Vector3(0, 1.2f, 0); // 월드 기준 오프셋(월드 스페이스용)
    public Vector2 screenOffset = new Vector2(0, 0);      // 스크린 기준 오프셋(스크린 스페이스용)

    [Header("겹침 방지(스택)")]
    public int orderIndex = 0;              // 0=첫번째(예: 체력), 1=두번째(예: 경험치)
    public float stackSpacing = 24f;        // 바 간 간격(스크린 스페이스)
    public float worldStackSpacing = 0.25f; // 바 간 간격(월드 스페이스 Y)

    [Header("부드러운 추적")]
    public float smooth = 20f;              // 0이면 즉시 이동

    RectTransform rt;
    Canvas rootCanvas;
    Camera uiCam;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            uiCam = rootCanvas.worldCamera;
    }

    void LateUpdate()
    {
        if (!target || !rt) return;
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera && !uiCam)
            uiCam = rootCanvas.worldCamera;

        if (!rootCanvas || rootCanvas.renderMode == RenderMode.WorldSpace)
        {
            // 월드 스페이스 캔버스: 실제 위치를 타겟 위치 근처로
            Vector3 basePos = target.position + worldOffset;
            basePos += new Vector3(0f, orderIndex * worldStackSpacing, 0f); // 위로 쌓기
            Vector3 cur = rt.position;
            Vector3 dst = (smooth > 0f) ? Vector3.Lerp(cur, basePos, Time.unscaledDeltaTime * smooth) : basePos;
            rt.position = dst;
        }
        else
        {
            // 스크린 스페이스(Overlay/Camera): 앵커드 포지션으로 배치
            Vector2 screenPos;
            if (uiCam)
                screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, target.position);
            else
                screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);

            // 기본 오프셋 + 스택 간격(위로 쌓기)
            Vector2 targetScreen = screenPos + screenOffset + new Vector2(0f, orderIndex * stackSpacing);

            Vector2 localPoint;
            RectTransform canvasRT = rootCanvas.transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, targetScreen, uiCam, out localPoint);

            Vector2 cur = rt.anchoredPosition;
            Vector2 dst = (smooth > 0f) ? Vector2.Lerp(cur, localPoint, Time.unscaledDeltaTime * smooth) : localPoint;
            rt.anchoredPosition = dst;
        }
    }
}
