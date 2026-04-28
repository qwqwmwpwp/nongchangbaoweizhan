using UnityEngine;

public abstract class FriendlyUnitStateBase
{
    protected readonly FriendlyUnitStateController controller;

    protected FriendlyUnitStateBase(FriendlyUnitStateController controller)
    {
        this.controller = controller;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate(float deltaTime) { }
    public virtual void OnExit() { }
}
