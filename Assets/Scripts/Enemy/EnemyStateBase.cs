using UnityEngine;

public abstract class EnemyStateBase
{
    protected readonly EnemyStateController controller;

    protected EnemyStateBase(EnemyStateController controller)
    {
        this.controller = controller;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate(float deltaTime) { }
    public virtual void OnExit() { }
}
