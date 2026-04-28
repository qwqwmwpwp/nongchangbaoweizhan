using UnityEngine;

[DisallowMultipleComponent]
public class FriendlyOrbitMovement : MonoBehaviour
{
    private Transform ownerTower;
    private float moveSpeed;
    private float guardForwardOffset;
    private Vector2 guardBoxSize = new Vector2(3f, 2f);
    private Vector3 standbyPoint;
    private bool hasStandbyPoint;

    public void Init(Transform owner, float forwardOffset, Vector2 boxSize, float unitMoveSpeed, float moveSpeedScale)
    {
        ownerTower = owner;
        guardForwardOffset = Mathf.Max(0f, forwardOffset);
        guardBoxSize = new Vector2(Mathf.Max(0.2f, boxSize.x), Mathf.Max(0.2f, boxSize.y));
        moveSpeed = Mathf.Max(0.1f, unitMoveSpeed * Mathf.Max(0.1f, moveSpeedScale));
        PickStandbyPoint();
    }

    public void TickMoveToStandby(float deltaTime)
    {
        if (ownerTower == null)
            return;

        if (!hasStandbyPoint)
            PickStandbyPoint();

        Vector3 next = Vector3.MoveTowards(transform.position, standbyPoint, moveSpeed * deltaTime);
        transform.position = ClampToGuardArea(next);
    }

    public bool IsAtStandbyPoint(float tolerance = 0.15f)
    {
        if (!hasStandbyPoint)
            return false;
        return Vector3.Distance(transform.position, standbyPoint) <= Mathf.Max(0.01f, tolerance);
    }

    public bool IsInsideGuardArea(Vector3 worldPos)
    {
        Vector3 local = WorldToGuardLocal(worldPos);
        float halfW = guardBoxSize.x * 0.5f;
        float halfD = guardBoxSize.y * 0.5f;
        return local.x >= -halfW && local.x <= halfW && local.z >= -halfD && local.z <= halfD;
    }

    public Vector3 GetNearestPointInGuardArea(Vector3 worldPos)
    {
        Vector3 local = WorldToGuardLocal(worldPos);
        float halfW = guardBoxSize.x * 0.5f;
        float halfD = guardBoxSize.y * 0.5f;
        local.x = Mathf.Clamp(local.x, -halfW, halfW);
        local.y = 0f;
        local.z = Mathf.Clamp(local.z, -halfD, halfD);
        return GuardLocalToWorld(local);
    }

    public bool IsInsideChaseArea(Vector3 worldPos, float chaseRadius)
    {
        Vector3 center = GuardCenter();
        float radius = Mathf.Max(0.1f, chaseRadius);
        return (worldPos - center).sqrMagnitude <= radius * radius;
    }

    private void PickStandbyPoint()
    {
        if (ownerTower == null)
            return;

        float halfW = guardBoxSize.x * 0.5f;
        float halfD = guardBoxSize.y * 0.5f;
        Vector3 local = new Vector3(Random.Range(-halfW, halfW), 0f, Random.Range(-halfD, halfD));
        standbyPoint = GuardLocalToWorld(local);
        hasStandbyPoint = true;
    }

    private Vector3 ClampToGuardArea(Vector3 worldPos)
    {
        return GetNearestPointInGuardArea(worldPos);
    }

    private Vector3 GuardCenter()
    {
        if (ownerTower == null)
            return transform.position;
        return ownerTower.position + ownerTower.forward * guardForwardOffset;
    }

    private Vector3 GuardLocalToWorld(Vector3 guardLocal)
    {
        if (ownerTower == null)
            return guardLocal;
        return GuardCenter() + ownerTower.right * guardLocal.x + ownerTower.forward * guardLocal.z;
    }

    private Vector3 WorldToGuardLocal(Vector3 worldPos)
    {
        if (ownerTower == null)
            return worldPos;
        Vector3 delta = worldPos - GuardCenter();
        return new Vector3(Vector3.Dot(delta, ownerTower.right), 0f, Vector3.Dot(delta, ownerTower.forward));
    }

    public void DrawGizmos(float detectRadius, float attackRange, float chaseRadius)
    {
        Vector3 chaseCenter = GuardCenter();

        if (ownerTower != null)
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(chaseCenter, Quaternion.LookRotation(ownerTower.forward, Vector3.up), Vector3.one);
            Gizmos.color = new Color(0.25f, 0.95f, 0.45f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(guardBoxSize.x, 0.1f, guardBoxSize.y));
            Gizmos.matrix = prev;
        }

        Gizmos.color = new Color(0.25f, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, detectRadius));
        Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, attackRange));
        Gizmos.color = new Color(1f, 0.2f, 0.6f, 0.8f);
        Gizmos.DrawWireSphere(chaseCenter, Mathf.Max(0.1f, chaseRadius));

        if (hasStandbyPoint)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Gizmos.DrawSphere(standbyPoint, 0.12f);
        }
    }
}
