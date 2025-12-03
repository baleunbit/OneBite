using UnityEngine;

[RequireComponent(typeof(Mob))]
public class MobPatrolAuto2D : MonoBehaviour
{
    public float radius = 3f;
    public float patrolSpeed = 1f;

    Mob mob;
    Vector2 origin;

    void Awake()
    {
        mob = GetComponent<Mob>();
        origin = transform.position;
    }

    public void Tick()
    {
        if (mob.isSensing || mob.IsAlerted) return;

        Vector2 pos = transform.position;

        Vector2 dir = (origin - pos);
        float dist = dir.magnitude;

        if (dist > radius)
        {
            dir = dir.normalized;
        }
        else
        {
            dir = new Vector2(Mathf.PerlinNoise(Time.time, 0) - 0.5f, Mathf.PerlinNoise(0, Time.time) - 0.5f).normalized;
        }

        transform.position = pos + dir * patrolSpeed * Time.deltaTime;
    }
}
