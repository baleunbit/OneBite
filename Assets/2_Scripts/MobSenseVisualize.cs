using UnityEngine;

[RequireComponent(typeof(Mob))]
public class MobSenseVisualize : MonoBehaviour
{
    [Range(12, 256)] public int segments = 64;
    public float lineWidth = 0.035f;
    [Range(0f, 1f)] public float alpha = 0.35f;

    public Color ringColor = new(0.2f, 0.7f, 1f, 0.6f);
    public Color fovColor = new(1f, 0.9f, 0.1f, 0.6f);

    Mob mob;
    LineRenderer ring;
    LineRenderer fan;

    public Vector2 currentForward = Vector2.up;

    void Awake()
    {
        mob = GetComponent<Mob>();
        ring = MakeLR("SenseRing");
        fan = MakeLR("SenseFOV");

        // Mob.cs가 넣어준 랜덤 시야를 그대로 따른다.
        currentForward = mob.currentViewDirection;
    }

    LineRenderer MakeLR(string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.widthMultiplier = lineWidth;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        return lr;
    }

    void LateUpdate()
    {
        UpdateForward();
        DrawRing();
        DrawFan();
    }

    void UpdateForward()
    {
        Vector2 mobPos = transform.position;
        Vector2 player = mob.target.position;

        // 1) 현재 목표 방향 계산
        Vector2 targetDirection;

        if (mob.IsAlerted || mob.isSensing)
        {
            targetDirection = (player - mobPos).normalized;
        }
        else
        {
            // 순찰 상태 → Mob.cs가 정해둔 방향을 그대로 사용
            targetDirection = mob.currentViewDirection.normalized;
        }

        // 2) forward가 0이면 기본값 보정
        if (currentForward == Vector2.zero)
            currentForward = Vector2.up;

        // 3) 서서히 회전
        float maxStep = mob.alertRotationSpeed * Time.deltaTime;
        currentForward = Vector2.MoveTowards(currentForward, targetDirection, maxStep);

        // 4) Mob.cs가 참조하는 방향에도 전달
        mob.currentViewDirection = currentForward;
    }


    void DrawRing()
    {
        float r = mob.detectRadius;
        int N = segments;

        ring.positionCount = N + 1;

        var c = ringColor; c.a = alpha;
        ring.startColor = ring.endColor = c;

        Vector3 center = transform.position;

        for (int i = 0; i <= N; i++)
        {
            float t = i / (float)N * Mathf.PI * 2f;
            Vector3 p = new(Mathf.Cos(t), Mathf.Sin(t), 0f);
            ring.SetPosition(i, center + p * r);
        }
    }

    public void ForceRedraw()
    {
        DrawRing();
        DrawFan();
    }

    public void DrawFan()
    {
        if (!mob || !mob.target) return;

        float dist = Mathf.Max(0.01f, mob.viewDistance);
        float half = Mathf.Clamp(mob.fovAngle * 0.5f, 0f, 180f);
        int N = Mathf.Max(12, segments / 2);

        // 중심 1 + 경계 N+1
        fan.positionCount = N + 2;

        // 색상
        var c = fovColor; c.a = alpha;
        fan.startColor = fan.endColor = c;

        Vector3 center = transform.position;
        Vector2 forward = currentForward;

        // ★ forward가 0벡터면 기본값 보정
        if (forward == Vector2.zero)
            forward = Vector2.up;

        // 중심
        fan.SetPosition(0, center);

        // 경계선들
        float start = -half;
        for (int i = 0; i <= N; i++)
        {
            float t = i / (float)N;
            float a = start + (half * 2f) * t;

            Vector2 dir = Quaternion.Euler(0, 0, a) * forward;
            fan.SetPosition(1 + i, center + (Vector3)(dir * dist));
        }
    }

}