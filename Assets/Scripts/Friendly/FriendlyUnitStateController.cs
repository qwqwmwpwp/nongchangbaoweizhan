using UnityEngine;
using qwq;
//状态控制器
[DisallowMultipleComponent]
public class FriendlyUnitStateController : MonoBehaviour
{
    private FriendlyUnit owner;
    private FriendlyUnitAnimatorDriver animatorDriver;
    private FriendlyUnitStateBase currentState;

    private FriendlyIdleGuardState idleGuardState;
    private FriendlyAcquireTargetState acquireTargetState;
    private FriendlyChaseState chaseState;
    private FriendlyAttackState attackState;
    private FriendlyReturnToGuardState returnToGuardState;
    private FriendlyDeathState deathState;

    private IDamageable currentTarget;
    private float attackCooldown;
    private float reacquireCooldown;
    private bool isDead;

    public Vector3 TargetPosition
    {
        get
        {
            if (TryGetTargetTransform(out Transform targetTf))
                return targetTf.position;
            return owner != null ? owner.transform.position : transform.position;
        }
    }

    public void Bind(FriendlyUnit friendlyUnit, FriendlyUnitAnimatorDriver driver = null)
    {
        owner = friendlyUnit;
        animatorDriver = driver != null ? driver : GetComponent<FriendlyUnitAnimatorDriver>();
        animatorDriver?.Bind(this);
        idleGuardState = new FriendlyIdleGuardState(this);
        acquireTargetState = new FriendlyAcquireTargetState(this);
        chaseState = new FriendlyChaseState(this);
        attackState = new FriendlyAttackState(this);
        returnToGuardState = new FriendlyReturnToGuardState(this);
        deathState = new FriendlyDeathState(this);
    }

    public void StartStateMachine()
    {
        if (isDead)
            return;

        attackCooldown = 0f;
        currentTarget = null;
        SwitchState(idleGuardState);
    }

    public void Tick(float deltaTime)
    {
        if (owner == null)
            return;
        if (isDead)
        {
            currentState?.OnUpdate(deltaTime);
            return;
        }

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
    public void SwitchToDeath()
    {
        if (currentState == deathState)
            return;

        isDead = true;
        SwitchState(deathState);
    }

    public void PlayIdleAnimation()
    {
        if (isDead)
            return;

        animatorDriver?.PlayIdle();
    }

    public void TickGuardMove(float deltaTime)
    {
        Vector3 before = owner.transform.position;
        Transform returnPoint = owner.AssignedReturnPoint;
        if (returnPoint != null)
        {
            float speed = Mathf.Max(0.1f, owner.MoveSpeed);
            owner.transform.position = Vector3.MoveTowards(owner.transform.position, returnPoint.position, speed * deltaTime);
            PlayMoveAnimationIfMoved(before);
            PlayIdleAnimationIfStopped(before);
            return;
        }

        owner.GuardMover?.TickMoveToStandby(deltaTime);
        PlayMoveAnimationIfMoved(before);
        PlayIdleAnimationIfStopped(before);
    }

    public void TickReturnMove(float deltaTime)
    {
        Vector3 before = owner.transform.position;
        Transform returnPoint = owner.AssignedReturnPoint;
        if (returnPoint != null)
        {
            float speed = Mathf.Max(0.1f, owner.MoveSpeed);
            owner.transform.position = Vector3.MoveTowards(owner.transform.position, returnPoint.position, speed * deltaTime);
            PlayMoveAnimationIfMoved(before);
            PlayIdleAnimationIfStopped(before);
            return;
        }

        if (owner.GuardMover != null)
            owner.GuardMover.TickMoveToStandby(deltaTime);
        PlayMoveAnimationIfMoved(before);
        PlayIdleAnimationIfStopped(before);
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
        if (isDead)
            return false;

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
        if (isDead)
            return false;

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
            if (targetComp is Enemy enemy && !enemy.IsInteractable)
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
        Vector3 before = owner.transform.position;
        float speed = Mathf.Max(0.1f, owner.MoveSpeed);
        owner.transform.position = Vector3.MoveTowards(owner.transform.position, targetPos, speed * deltaTime);
        PlayMoveAnimationIfMoved(before);
    }

    public void TryStartAttackCurrentTarget()
    {
        if (isDead || attackCooldown > 0f || !HasValidTarget())
            return;

        attackCooldown = 1f / Mathf.Max(0.1f, owner.AttackSpeed);
        animatorDriver?.PlayAttack(true);
    }

    public bool OnAttackHit()
    {
        if (isDead || currentState != attackState)
            return false;
        if (!HasValidTarget())
            return false;

        Vector3 targetPos = TargetPosition;
        if (!IsTargetInsideChaseArea(targetPos) || !IsTargetInAttackRange(targetPos))
            return false;

        try
        {
            currentTarget.TakeDamage(owner.Attack);
            return true;
        }
        catch (MissingReferenceException)
        {
            currentTarget = null;
            return false;
        }
    }

    public void EnterDeathState()
    {
        isDead = true;
        currentTarget = null;
    }

    public bool PlayDeathAnimation()
    {
        return animatorDriver != null && animatorDriver.PlayDeath(true);
    }

    public bool IsDeathAnimationFinished()
    {
        return animatorDriver == null || animatorDriver.IsDeathAnimationFinished();
    }

    public void OnDeathAnimationFinished()
    {
        if (!isDead)
            return;

        owner?.DestroyAfterDeathAnimation();
    }

    public void DestroyOwner()
    {
        owner?.DestroyAfterDeathAnimation();
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

    private void PlayMoveAnimationIfMoved(Vector3 before)
    {
        if (isDead || animatorDriver == null || owner == null)
            return;

        Vector3 delta = owner.transform.position - before;
        if (delta.sqrMagnitude > 0.0001f)
            animatorDriver.PlayMove();
    }

    private void PlayIdleAnimationIfStopped(Vector3 before)
    {
        if (isDead || animatorDriver == null || owner == null)
            return;

        Vector3 delta = owner.transform.position - before;
        if (delta.sqrMagnitude <= 0.0001f)
            animatorDriver.PlayIdle();
    }
}
