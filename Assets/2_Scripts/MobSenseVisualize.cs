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

    // Mob.cs 안에는 currentViewDirection만 있음 → 이걸 기반으로 사용
    public Vector2 currentForward;

    void Awake()
    {
        mob = GetComponent<Mob>();
        ring = MakeLR("SenseRing");
        fan = MakeLR("SenseFOV");

        // Mob.cs가 정해둔 랜덤 방향 받아오기
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


    // ----------------------- 핵심 수정 -----------------------
    void UpdateForward()
    {
        Vector2 mobPos = transform.position;
        Vector2 playerPos = mob.target.position;

        Vector2 targetDir;

        if (mob.IsAlerted || mob.isSensing)
        {
            targetDir = (playerPos - mobPos).normalized;
        }
        else
        {
            targetDir = mob.currentViewDirection.normalized;
        }

        if (currentForward == Vector2.zero)
            currentForward = Vector2.up;

        // Vector2에는 RotateTowards 없음 → 직접 구현
        float maxRadians = mob.alertRotationSpeed * Mathf.Deg2Rad * Time.deltaTime;

        currentForward = Vector2.RotateTowards(
            currentForward,
            targetDir,
            maxRadians,
            0f
        );

        mob.currentViewDirection = currentForward;
    }
    // ---------------------------------------------------------


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

        float dist = mob.viewDistance;
        float half = mob.fovAngle * 0.5f;
        int N = Mathf.Max(12, segments / 2);

        fan.positionCount = N + 2;
        var c = fovColor; c.a = alpha;
        fan.startColor = fan.endColor = c;

        Vector3 center = transform.position;
        Vector2 forward = currentForward.normalized;

        fan.SetPosition(0, center);

        for (int i = 0; i <= N; i++)
        {
            float a = -half + (half * 2f) * (i / (float)N);
            Vector2 dir = Quaternion.Euler(0, 0, a) * forward;
            fan.SetPosition(i + 1, center + (Vector3)(dir * dist));
        }
    }
}
