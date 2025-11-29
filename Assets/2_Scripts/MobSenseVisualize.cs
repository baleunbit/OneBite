using UnityEngine;

[RequireComponent(typeof(Mob))]
public class MobSenseVisualize : MonoBehaviour
{
    [Range(12, 256)] public int segments = 64;
    public float lineWidth = 0.035f;
    [Range(0f, 1f)] public float alpha = 0.35f;

    public Color ringColor = new(0.2f, 0.7f, 1f, 0.6f); // 근접(원)
    public Color fovColor = new(1f, 0.9f, 0.1f, 0.6f); // 시야(부채꼴)

    Mob mob;
    LineRenderer ring;    // 근접 원
    LineRenderer fan;     // 시야 부채꼴 (중심 포함)

    Vector2 currentForward = Vector2.up; // 현재 시야 부채꼴의 방향 (회전 중인 방향)

    void Awake()
    {
        mob = GetComponent<Mob>();
        ring = MakeLR("SenseRing");
        fan = MakeLR("SenseFOV");

        currentForward = Vector2.up;
        if (mob) mob.currentViewDirection = currentForward;
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
        UpdateForwardDirection(); // 회전 로직 실행
        DrawRing();
        DrawFan();
    }

    void UpdateForwardDirection()
    {
        if (!mob || !mob.target)
        {
            currentForward = Vector2.up;
            mob.currentViewDirection = currentForward;
            return;
        }

        Vector2 mobPos2D = transform.position;
        Vector2 target2D = mob.target.position;

        Vector2 targetDirection;

        // 1) 발각된 상태 → 플레이어 방향 강제
        if (mob.IsAlerted)
        {
            targetDirection = (target2D - mobPos2D).normalized;
        }
        // 2) 의심 상태 → 플레이어 방향으로 회전
        else if (mob.isSensing)
        {
            targetDirection = (target2D - mobPos2D).normalized;
        }
        // 3) 순찰 상태 → 순찰 이동 방향 사용
        else
        {
            targetDirection = mob.currentViewDirection;   // ★ 핵심 수정!
        }

        // 회전 처리
        float angle = Vector2.SignedAngle(currentForward, targetDirection);
        angle = Mathf.MoveTowards(0f, angle, mob.rotationSpeed * Time.deltaTime);

        currentForward = Quaternion.Euler(0, 0, angle) * currentForward;

        mob.currentViewDirection = currentForward;
    }


    void DrawRing()
    {
        float r = Mathf.Max(0.01f, mob.detectRadius);
        int N = Mathf.Max(12, segments);
        ring.positionCount = N + 1;
        var c = ringColor; c.a = alpha;
        ring.startColor = ring.endColor = c;

        Vector3 center = transform.position;
        for (int i = 0; i <= N; i++)
        {
            float t = (float)i / N * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * r + center;
            ring.SetPosition(i, p);
        }
    }

    public void DrawFan()
    {
        if (!mob || !mob.target) return;

        float dist = Mathf.Max(0.01f, mob.viewDistance);
        float half = Mathf.Clamp(mob.fovAngle * 0.5f, 0f, 180f);
        int N = Mathf.Max(12, segments / 2);

        fan.positionCount = N + 3;
        var c = fovColor; c.a = alpha;
        fan.startColor = fan.endColor = c;

        Vector3 center = transform.position;

        Vector2 forward = currentForward;

        fan.SetPosition(0, center);

        float start = -half;
        for (int i = 0; i <= N; i++)
        {
            float a = start + (half * 2f) * (i / (float)N);
            Vector2 dir = Quaternion.Euler(0, 0, a) * forward;
            fan.SetPosition(1 + i, center + (Vector3)(dir.normalized * dist));
        }

        fan.SetPosition(N + 2, center);
    }
}