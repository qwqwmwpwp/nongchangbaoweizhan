using UnityEngine;
using qwq;
//状态控制器
[DisallowMultipleComponent]
public class FriendlyUnitStateController : MonoBehaviour
{
    private FriendlyUnit owner;
    private FriendlyUnitStateBase currentState;

    private FriendlyIdleGuardState idleGuardState;
    private FriendlyAcquireTargetState acquireTargetState;
    private FriendlyChaseState chaseState;
    private FriendlyAttackState attackState;
    private FriendlyReturnToGuardState returnToGuardState;

    private IDamageable currentTarget;
    private float attackCooldown;
    private float reacquireCooldown;

    public Vector3 TargetPosition
    {
        get
        {
            if (TryGetTargetTransform(out Transform targetTf))
                return targetTf.position;
            return owner != null ? owner.transform.position : transform.position;
        }
    }

    public void Bind(FriendlyUnit friendlyUnit)
    {
        owner = friendlyUnit;
        idleGuardState = new FriendlyIdleGuardState(this);
        acquireTargetState = new FriendlyAcquireTargetState(this);
        chaseState = new FriendlyChaseState(this);
        attackState = new FriendlyAttackState(this);
        returnToGuardState = new FriendlyReturnToGuardState(this);
    }

    public void StartStateMachine()
    {
        attackCooldown = 0f;
        currentTarget = null;
        SwitchState(idleGuardState);
    }

    public void Tick(float deltaTime)
    {
        if (owner == null)
            return;

        if (attackCooldown > 0f)
            attackCooldown -= deltaTime;
        if (reacquireCooldown > 0f)
            reacquireCooldown -= deltaTime;

        currentState?.OnUpdate(deltaTime);
    }

    public void SwitchState(FriendlyUnitStateBase newState)
    {
        if (newState == null || currentState == newState)
            return;

        currentState?.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }

    public void SwitchToIdleGuard() => SwitchState(idleGuardState);
    public void SwitchToAcquireTarget() => SwitchState(acquireTargetState);
    public void SwitchToChase() => SwitchState(chaseState);
    public void SwitchToAttack() => SwitchState(attackState);
    public void SwitchToReturnToGuard() => SwitchState(returnToGuardState);

    public void TickGuardMove(float deltaTime)
    {
        Transform returnPoint = owner.AssignedReturnPoint;
        if (returnPoint != null)
        {
            float speed = Mathf.Max(0.1f, owner.MoveSpeed);
            owner.transform.position = Vector3.MoveTowards(owner.transform.position, returnPoint.position, speed * deltaTime);
            return;
        }

        owner.GuardMover?.TickMoveToStandby(deltaTime);
    }

    public void TickReturnMove(float deltaTime)
    {
        Transform returnPoint = owner.AssignedReturnPoint;
        if (returnPoint != null)
        {
            float speed = Mathf.Max(0.1f, owner.MoveSpeed);
            owner.transform.position = Vector3.MoveTowards(owner.transform.position, returnPoint.position, speed * deltaTime);
            return;
        }

        if (owner.GuardMover != null)
            owner.GuardMover.TickMoveToStandby(deltaTime);
    }

    public bool IsGuardReady()
    {
        Transform returnPoint = owner.AssignedReturnPoint;
        if (returnPoint != null)
            return Vector3.Distance(owner.transform.position, returnPoint.position) <= 0.15f;
        return owner.GuardMover == null || owner.GuardMover.IsAtStandbyPoint();
    }

    public bool TryAcquireTarget()
    {
        currentTarget = null;
        BambooCtx bambooCtx = owner.OwnerBambooCtx;
        if (bambooCtx == null)
            return false;

        Enemy enemy = bambooCtx.FindNearestEnemyInTriggerRange(owner.transform.position);
        if (enemy != null)
            currentTarget = enemy;

        return currentTarget != null;
    }

    public bool CanAttemptAcquire()
    {
        return reacquireCooldown <= 0f;
    }

    public void MarkAcquireFailed()
    {
        reacquireCooldown = 0.2f;
    }

    public bool HasValidTarget()
    {
        if (currentTarget == null)
            return false;

        if (currentTarget as Object == null)
        {
            currentTarget = null;
            return false;
        }

        if (currentTarget is Component targetComp)
        {
            if (targetComp.gameObject == null || !targetComp.gameObject.activeInHierarchy)
            {
                currentTarget = null;
                return false;
            }
            return true;
        }

        return true;
    }

    public bool IsTargetInsideChaseArea(Vector3 targetPos)
    {
        if (!HasValidTarget())
            return false;

        BambooCtx bambooCtx = owner.OwnerBambooCtx;
        if (bambooCtx == null)
            return false;

        Enemy enemy = currentTarget as Enemy;
        if (enemy == null)
            return false;

        return bambooCtx.IsEnemyInTriggerRange(enemy);
    }

    public bool IsTargetInAttackRange(Vector3 targetPos)
    {
        float sqrDist = (targetPos - owner.transform.position).sqrMagnitude;
        float attackRange = owner.AttackRange;
        return sqrDist <= attackRange * attackRange;
    }

    public void MoveTowards(Vector3 targetPos, float deltaTime)
    {
        float speed = Mathf.Max(0.1f, owner.MoveSpeed);
        owner.transform.position = Vector3.MoveTowards(owner.transform.position, targetPos, speed * deltaTime);
    }

    public void TryAttackCurrentTarget()
    {
        if (attackCooldown > 0f || !HasValidTarget())
            return;

        try
        {
            currentTarget.TakeDamage(owner.Attack);
            attackCooldown = 1f / Mathf.Max(0.1f, owner.AttackSpeed);
        }
        catch (MissingReferenceException)
        {
            currentTarget = null;
        }
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }

    private bool TryGetTargetTransform(out Transform targetTf)
    {
        targetTf = null;
        if (!HasValidTarget())
            return false;

        if (currentTarget is Component targetComp)
        {
            targetTf = targetComp.transform;
            return targetTf != null;
        }

        return false;
    }
}
