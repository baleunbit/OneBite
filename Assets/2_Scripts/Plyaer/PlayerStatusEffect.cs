// PlayerStatusEffects.cs (미끄럼/리셋)

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerStatusEffects : MonoBehaviour
{
    private Rigidbody2D rb;
    private float baseDrag, baseAngularDrag;
    private bool saved;
    private PhysicsMaterial2D slipMat;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!saved)
        {
            baseDrag = rb.linearDamping;
            baseAngularDrag = rb.angularDamping;
            saved = true;
        }
    }

    public void ClearAll()
    {
        if (!rb) return;
        rb.linearDamping = baseDrag;
        rb.angularDamping = baseAngularDrag;
        if (slipMat) rb.sharedMaterial = null;
    }

    public void SetSlippery(bool enable, float drag = 0.05f, float angularDrag = 0.05f)
    {
        if (!rb) return;
        if (enable)
        {
            rb.linearDamping = Mathf.Max(0f, drag);
            rb.angularDamping = Mathf.Max(0f, angularDrag);
            if (!slipMat)
            {
                slipMat = new PhysicsMaterial2D("SlipZero");
                slipMat.friction = 0f;
                slipMat.bounciness = 0f;
            }
            rb.sharedMaterial = slipMat;
        }
        else
        {
            ClearAll();
        }
    }
}
