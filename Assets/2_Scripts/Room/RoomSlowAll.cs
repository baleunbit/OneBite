// RoomSlowAll.cs (방 내부 둔화)

using System.Collections.Generic;
using UnityEngine;

public class RoomSlowAll : MonoBehaviour
{
    [SerializeField] private float addedDrag = 8f;
    private readonly List<Rigidbody2D> rbs = new();
    private readonly Dictionary<Rigidbody2D, float> baseDrag = new();

    public void Init(float addedDrag) { this.addedDrag = Mathf.Max(0f, addedDrag); }

    void OnEnable() { Cache(); Apply(); }
    void OnDisable() { Restore(); }

    private void Cache()
    {
        rbs.Clear();
        baseDrag.Clear();
        var bodies = GetComponentsInChildren<Rigidbody2D>(true);
        foreach (var rb in bodies)
        {
            if (!rb) continue;
            rbs.Add(rb);
            if (!baseDrag.ContainsKey(rb)) baseDrag[rb] = rb.linearDamping;
        }
    }

    private void Apply()
    {
        foreach (var rb in rbs)
        {
            if (!rb) continue;
            if (!baseDrag.TryGetValue(rb, out float d)) d = rb.linearDamping;
            rb.linearDamping = d + addedDrag;
        }
    }

    private void Restore()
    {
        foreach (var kv in baseDrag)
            if (kv.Key) kv.Key.linearDamping = kv.Value;
        baseDrag.Clear();
        rbs.Clear();
    }
}
