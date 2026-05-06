using UnityEngine;
//以下为具体状态类
public class FriendlyIdleGuardState : FriendlyUnitStateBase
{
    public FriendlyIdleGuardState(FriendlyUnitStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        controller.PlayIdleAnimation();
    }

    public override void OnUpdate(float deltaTime)
    {
        controller.TickGuardMove(deltaTime);
        if (controller.IsGuardReady() && controller.CanAttemptAcquire())
            controller.SwitchToAcquireTarget();
    }
}

public class FriendlyAcquireTargetState : FriendlyUnitStateBase
{
    public FriendlyAcquireTargetState(FriendlyUnitStateController controller) : base(controller) { }

    public override void OnUpdate(float deltaTime)
    {
        if (!controller.TryAcquireTarget())
        {
            controller.MarkAcquireFailed();
            controller.SwitchToIdleGuard();
            return;
        }

        controller.SwitchToChase();
    }
}

public class FriendlyChaseState : FriendlyUnitStateBase
{
    public FriendlyChaseState(FriendlyUnitStateController controller) : base(controller) { }

    public override void OnUpdate(float deltaTime)
    {
        if (!controller.HasValidTarget())
        {
            controller.SwitchToReturnToGuard();
            return;
        }

        Vector3 targetPos = controller.TargetPosition;
        if (!controller.IsTargetInsideChaseArea(targetPos))
        {
            controller.ClearTarget();
            controller.SwitchToReturnToGuard();
            return;
        }

        if (controller.IsTargetInAttackRange(targetPos))
        {
            controller.SwitchToAttack();
            return;
        }

        controller.MoveTowards(targetPos, deltaTime);
    }
}

public class FriendlyAttackState : FriendlyUnitStateBase
{
    public FriendlyAttackState(FriendlyUnitStateController controller) : base(controller) { }

    public override void OnUpdate(float deltaTime)
    {
        if (!controller.HasValidTarget())
        {
            controller.ClearTarget();
            controller.SwitchToReturnToGuard();
            return;
        }

        Vector3 targetPos = controller.TargetPosition;
        if (!controller.IsTargetInsideChaseArea(targetPos))
        {
            controller.ClearTarget();
            controller.SwitchToReturnToGuard();
            return;
        }

        if (!controller.IsTargetInAttackRange(targetPos))
        {
            controller.SwitchToChase();
            return;
        }

        controller.TryStartAttackCurrentTarget();
    }
}

public class FriendlyReturnToGuardState : FriendlyUnitStateBase
{
    public FriendlyReturnToGuardState(FriendlyUnitStateController controller) : base(controller) { }

    public override void OnUpdate(float deltaTime)
    {
        controller.TickReturnMove(deltaTime);
        if (controller.IsGuardReady())
            controller.SwitchToIdleGuard();
    }
}

public class FriendlyDeathState : FriendlyUnitStateBase
{
    private bool hasDeathAnimation;

    public FriendlyDeathState(FriendlyUnitStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        controller.EnterDeathState();
        hasDeathAnimation = controller.PlayDeathAnimation();
        if (!hasDeathAnimation)
            controller.DestroyOwner();
    }

    public override void OnUpdate(float deltaTime)
    {
        if (hasDeathAnimation && controller.IsDeathAnimationFinished())
            controller.DestroyOwner();
    }
}
