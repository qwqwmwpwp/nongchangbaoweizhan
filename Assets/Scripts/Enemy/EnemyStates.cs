using UnityEngine;

public class EnemyPathMoveState : EnemyStateBase
{
    public EnemyPathMoveState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        controller.SetBattleAnimation(false);
    }

    public override void OnUpdate(float deltaTime)
    {
        controller.TickPathMove(deltaTime);
        controller.SyncPathMoveAnimation();

        if (controller.TryAcquireTarget())
            controller.SwitchToChaseFriendly();
    }
}

public class EnemyChaseFriendlyState : EnemyStateBase
{
    public EnemyChaseFriendlyState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        controller.SetBattleAnimation(false);
    }

    public override void OnUpdate(float deltaTime)
    {
        if (!controller.HasValidTarget())
        {
            if (!controller.TryAcquireTarget())
            {
                controller.SwitchToPathMove(true);
                return;
            }
        }

        controller.MoveTowardsTarget(deltaTime);
        controller.SyncChaseAnimation();

        if (controller.ShouldEnterBattle())
            controller.SwitchToBattle();
    }
}

public class EnemyBattleState : EnemyStateBase
{
    public EnemyBattleState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        controller.SetBattleAnimation(true);
    }

    public override void OnUpdate(float deltaTime)
    {
        if (!controller.HasValidTarget())
        {
            if (!controller.TryAcquireTarget())
            {
                controller.SwitchToPathMove(true);
                return;
            }

            controller.SwitchToChaseFriendly();
            return;
        }

        if (!controller.ShouldEnterBattle())
        {
            controller.SwitchToChaseFriendly();
            return;
        }

        if (!controller.IsTargetInAttackRange())
        {
            controller.SwitchToChaseFriendly();
            return;
        }

        controller.TryTriggerBattleAttackAnimation();
    }

    public override void OnExit()
    {
        controller.SetBattleAnimation(false);
    }
}
