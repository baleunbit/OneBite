using UnityEngine;

public class WeaponMount : MonoBehaviour
{
    [Header("Socket 기준 로컬 보정")]
    public Vector2 localOffset = new Vector2(0.72f, 0.1f);
    public float localZRotation = -12.99f;
}
