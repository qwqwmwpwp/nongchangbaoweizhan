using HSM;
using qwq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Bamboo : Plants
{
    public BambooCtx ctx;
    public override PlantsCtx plantsCtx => ctx;

    protected override void Awake()
    {
        ctx.transform = transform;
        root = new BambooRoot(null, ctx);
        base.Awake();
    }

}

[Serializable]
public class BambooCtx : PlantsCtx
{
    [Header("生长期")]
    public GameObject obj1;
    public float grow1 = 10f;

    [Header("成熟期")]
    public GameObject obj2;
    public float grow2 = 10f;
    [Header("衰老期")]
    public GameObject obj3;

    public bool isBackward;
    public float Backward_t = 0;

    public List<GameObject> soldier;

}

namespace HSM{

public class BambooRoot : State
{
    public BambooState1 state1;
    public BambooState2 state2;
    public BambooState3 state3;

    public BambooRoot(StateMachine m, BambooCtx ctx) : base(m, null)
    {
        state1 = new BambooState1(m, this, ctx);
        state2 = new BambooState2(m, this, ctx);
        state3 = new BambooState3(m, this, ctx);
    }
    protected override State GetInitialState() => state1;

    protected override State GetTransition() => null;

}

public class BambooState1 : State
{
    BambooCtx Ctx;
    public float grow;
    public BambooState1(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
    {
        Ctx = ctx;
    }

    protected override void OnEnter()
    {
        grow = Ctx.grow1;
        Ctx.obj1.SetActive(true);
    }

}

public class BambooState2 : State
{
    BambooCtx Ctx;
    public float grow;

    public BambooState2(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
    {
        Ctx = ctx;
    }

    protected override State GetTransition()
    {
        if (Ctx.isBackward)
        {
            Ctx.isBackward = false;
            grow = Ctx.grow1;
        }

        if (grow <= 0)
        {
            return ((BambooRoot)Parent).state3;

        }

        return null;
    }

    protected override void OnEnter()
    {
        Ctx.obj2.SetActive(true);
        grow = Ctx.grow2;
    }
}

    public class BambooState3 : State
    {
        BambooCtx Ctx;

        public BambooState3(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }
        protected override State GetTransition()
        {
            if (Ctx.isBackward)
            {
                Ctx.isBackward = false;
                return ((BambooRoot)Parent).state2;
            }

            return null;
        }
    }
}