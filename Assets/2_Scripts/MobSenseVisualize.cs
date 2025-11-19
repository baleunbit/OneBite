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

    void Awake()
    {
        mob = GetComponent<Mob>();
        ring = MakeLR("SenseRing");
        fan = MakeLR("SenseFOV");
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

    void Update()
    {
        DrawRing();
        DrawFan();
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

    void DrawFan()
    {
        if (!mob || !mob.target) return;

        float dist = Mathf.Max(0.01f, mob.viewDistance);
        float half = Mathf.Clamp(mob.fovAngle * 0.5f, 0f, 180f);
        int N = Mathf.Max(12, segments / 2);

        fan.positionCount = N + 3;
        var c = fovColor; c.a = alpha;
        fan.startColor = fan.endColor = c;

        Vector3 center = transform.position;

        // 🔥 Vector2 변환 후 forward 계산
        Vector2 mobPos2D = new Vector2(center.x, center.y);
        Vector2 target2D = new Vector2(mob.target.position.x, mob.target.position.y);

        Vector2 forward = (target2D - mobPos2D).normalized;

        // 중심
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