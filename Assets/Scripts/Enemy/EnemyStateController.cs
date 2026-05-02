using UnityEngine;

[DisallowMultipleComponent]
public class EnemyStateController : MonoBehaviour
{
    private qwq.Enemy owner;
    private EnemyMove enemyMove;
    private EnemyAnimatorDriver animatorDriver;
    private EnemyFriendlyDetector friendlyDetector;
    private FriendlyUnit currentTarget;
    private EnemyStateBase currentState;
    private float battleAnimCooldown;

    private EnemyPathMoveState pathMoveState;
    private EnemyChaseFriendlyState chaseFriendlyState;
    private EnemyBattleState battleState;

    public void Bind(qwq.Enemy enemy, EnemyMove move, EnemyFriendlyDetector detector, EnemyAnimatorDriver driver)
    {
        owner = enemy;
        enemyMove = move;
        friendlyDetector = detector;
        animatorDriver = driver;

        pathMoveState = new EnemyPathMoveState(this);
        chaseFriendlyState = new EnemyChaseFriendlyState(this);
        battleState = new EnemyBattleState(this);
    }

    public void StartStateMachine()
    {
        ReleaseCurrentTargetEngagement();
        currentTarget = null;
        battleAnimCooldown = 0f;
        SwitchState(pathMoveState);
    }

    public void Tick(float deltaTime)
    {
        if (owner == null)
            return;
        if (battleAnimCooldown > 0f)
            battleAnimCooldown -= deltaTime;
        currentState?.OnUpdate(deltaTime);
    }

    public void TickPathMove(float deltaTime)
    {
        enemyMove?.TickPathMove(deltaTime);
    }

    public bool TryAcquireTarget()
    {
        FriendlyUnit next = friendlyDetector != null
            ? friendlyDetector.FindNearestEngageableFriendly(transform.position, owner)
            : null;

        if (next == currentTarget)
            return next != null;

        ReleaseCurrentTargetEngagement();
        currentTarget = next;
        return currentTarget != null;
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

        if (!currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget.ReleaseMeleeEngagement(owner);
            currentTarget = null;
            return false;
        }

        return true;
    }

    public void MoveTowardsTarget(float deltaTime)
    {
        if (!HasValidTarget())
            return;

        float speed = Mathf.Max(0.1f, owner.MoveSpeed);
        transform.position = Vector3.MoveTowards(transform.position, currentTarget.transform.position, speed * deltaTime);
    }

    public void SyncPathMoveAnimation()
    {
        Vector3 moveDir = enemyMove != null ? enemyMove.CurrentMoveDirection : Vector3.zero;
        bool isMoving = moveDir.sqrMagnitude > 0.0001f;
        animatorDriver?.SetMoveWorldDelta(moveDir, isMoving);
    }

    public void SyncChaseAnimation()
    {
        if (!HasValidTarget())
        {
            animatorDriver?.SetMoveWorldDelta(Vector3.zero, false);
            return;
        }

        Vector3 delta = (currentTarget.transform.position - transform.position).normalized;
        bool isMoving = delta.sqrMagnitude > 0.0001f;
        animatorDriver?.SetMoveWorldDelta(delta, isMoving);
    }

    public void SetBattleAnimation(bool isBattle)
    {
        animatorDriver?.SetStateFlags(false, false, isBattle);
        if (isBattle)
            animatorDriver?.SetMoveWorldDelta(Vector3.zero, false);
    }

    public void TryTriggerBattleAttackAnimation()
    {
        if (!HasValidTarget())
            return;

        if (battleAnimCooldown > 0f)
            return;

        animatorDriver?.TriggerAttack();
        battleAnimCooldown = 1f / Mathf.Max(0.1f, owner.AttackSpeed);
    }

    public bool ShouldEnterBattle()
    {
        if (!HasValidTarget())
            return false;
        if (owner.BattleEnterDistance <= 0f)
            return false;

        float sqrDist = (currentTarget.transform.position - transform.position).sqrMagnitude;
        float battleDist = owner.BattleEnterDistance;
        return sqrDist <= battleDist * battleDist;
    }

    public void SwitchToPathMove(bool rebindPath)
    {
        ReleaseCurrentTargetEngagement();
        currentTarget = null;
        if (rebindPath)
            enemyMove?.RebindPathFromCurrentPosition();
        SwitchState(pathMoveState);
    }

    public void SwitchToChaseFriendly()
    {
        SwitchState(chaseFriendlyState);
    }

    public void SwitchToBattle()
    {
        SwitchState(battleState);
    }

    private void SwitchState(EnemyStateBase newState)
    {
        if (newState == null || currentState == newState)
            return;

        currentState?.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }

    private void OnDestroy()
    {
        ReleaseCurrentTargetEngagement();
        currentTarget = null;
    }

    private void ReleaseCurrentTargetEngagement()
    {
        if (currentTarget == null || currentTarget as Object == null || owner == null || owner as Object == null)
            return;
        currentTarget.ReleaseMeleeEngagement(owner);
    }
}
