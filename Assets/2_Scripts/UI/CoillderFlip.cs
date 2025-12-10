using UnityEngine;

/// SpriteRenderer.flipX가 바뀔 때 2D 콜라이더를 좌우 미러링
[RequireComponent(typeof(SpriteRenderer))]
public class ColliderFlip2D : MonoBehaviour
{
    SpriteRenderer sr;
    Collider2D[] cols;

    // 원본 데이터 보관
    struct BoxData { public BoxCollider2D c; public Vector2 offset; }
    struct CapData { public CapsuleCollider2D c; public Vector2 offset; }
    struct CircData { public CircleCollider2D c; public Vector2 offset; }
    struct PolyData { public PolygonCollider2D c; public Vector2[][] paths; }
    struct EdgeData { public EdgeCollider2D c; public Vector2[] points; }

    BoxData[] boxes;
    CapData[] caps;
    CircData[] cirs;
    PolyData[] polys;
    EdgeData[] edges;

    bool lastFlip;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cols = GetComponents<Collider2D>();

        // 원본 스냅샷
        var boxList = new System.Collections.Generic.List<BoxData>();
        var capList = new System.Collections.Generic.List<CapData>();
        var cirList = new System.Collections.Generic.List<CircData>();
        var polyList = new System.Collections.Generic.List<PolyData>();
        var edgeList = new System.Collections.Generic.List<EdgeData>();

        foreach (var c in cols)
        {
            if (c is BoxCollider2D b)
                boxList.Add(new BoxData { c = b, offset = b.offset });
            else if (c is CapsuleCollider2D cp)
                capList.Add(new CapData { c = cp, offset = cp.offset });
            else if (c is CircleCollider2D cc)
                cirList.Add(new CircData { c = cc, offset = cc.offset });
            else if (c is PolygonCollider2D pc)
            {
                var paths = new Vector2[pc.pathCount][];
                for (int i = 0; i < pc.pathCount; i++)
                {
                    var pts = pc.GetPath(i);
                    paths[i] = (Vector2[])pts.Clone();
                }
                polyList.Add(new PolyData { c = pc, paths = paths });
            }
            else if (c is EdgeCollider2D ec)
            {
                var pts = ec.points;
                edgeList.Add(new EdgeData { c = ec, points = (Vector2[])pts.Clone() });
            }
        }

        boxes = boxList.ToArray();
        caps = capList.ToArray();
        cirs = cirList.ToArray();
        polys = polyList.ToArray();
        edges = edgeList.ToArray();

        lastFlip = sr.flipX;
        ApplyFlip(lastFlip);
    }

    void LateUpdate()
    {
        if (sr.flipX != lastFlip)
        {
            lastFlip = sr.flipX;
            ApplyFlip(lastFlip);
        }
    }

    void ApplyFlip(bool flipped)
    {
        float sign = flipped ? -1f : 1f;

        if (boxes != null)
            foreach (var b in boxes) b.c.offset = new Vector2(b.offset.x * sign, b.offset.y);

        if (caps != null)
            foreach (var cp in caps) cp.c.offset = new Vector2(cp.offset.x * sign, cp.offset.y);

        if (cirs != null)
            foreach (var cc in cirs) cc.c.offset = new Vector2(cc.offset.x * sign, cc.offset.y);

        if (polys != null)
            foreach (var p in polys)
            {
                p.c.pathCount = p.paths.Length;
                for (int i = 0; i < p.paths.Length; i++)
                {
                    var src = p.paths[i];
                    var dst = new Vector2[src.Length];
                    for (int k = 0; k < src.Length; k++)
                        dst[k] = new Vector2(src[k].x * sign, src[k].y);
                    p.c.SetPath(i, dst);
                }
            }

        if (edges != null)
            foreach (var e in edges)
            {
                var src = e.points;
                var dst = new Vector2[src.Length];
                for (int k = 0; k < src.Length; k++)
                    dst[k] = new Vector2(src[k].x * sign, src[k].y);
                e.c.points = dst;
            }
    }
}
