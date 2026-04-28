using UnityEngine;

public class EnemyPathMoveState : EnemyStateBase
{
    public EnemyPathMoveState(EnemyStateController controller) : base(controller) { }

    public override void OnUpdate(float deltaTime)
    {
        controller.TickPathMove(deltaTime);

        if (controller.TryAcquireTarget())
            controller.SwitchToChaseFriendly();
    }
}

public class EnemyChaseFriendlyState : EnemyStateBase
{
    public EnemyChaseFriendlyState(EnemyStateController controller) : base(controller) { }

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

        if (controller.ShouldEnterBattle())
            controller.SwitchToBattle();
    }
}

public class EnemyBattleState : EnemyStateBase
{
    public EnemyBattleState(EnemyStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        // TODO: 战斗状态后续实现
    }

    public override void OnUpdate(float deltaTime)
    {
        // TODO: 战斗状态后续实现
    }

    public override void OnExit()
    {
        // TODO: 战斗状态后续实现
    }
}
