using HSM;
using qwq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Watermelon : Plants
{
    public override PlantsCtx plantsCtx => ctx;

    public WatermelonCtx ctx;

    protected override void Awake()
    {
        root = new WatermelonRoot(null, ctx);
        base.Awake();
    }
}

[SerializeField]
public class WatermelonCtx :PlantsCtx
{
    public int attack;

    [Header("״̬1")]
    public float g1;
    public int attack1 = 5;
    public float Range1 = 4;
    public float AttackSpeed1= 1;
    [Header("״̬2")]
    public float g2;
    public int attack2 = 8;
    public float Range2 = 5;
    public float AttackSpeed2 = 0.5f;

    [Header("״̬3")]
    public int attack3 = 10;
    public float Range3 = 4;
    public float AttackSpeed = 1.5f;



}

namespace HSM
{
    public class WatermelonRoot : State
    {
        public WatermelonState1 state1;
        public WatermelonState1 state2;
        public WatermelonState1 state3;

        public WatermelonRoot(StateMachine machine, WatermelonCtx ctx) : base(machine, null)
        {
            state1 = new WatermelonState1(machine, this, ctx);
            state2 = new WatermelonState1(machine, this, ctx);
            state3 = new WatermelonState1(machine, this, ctx);

        }
    }

    public class WatermelonState1 : State
    {
        WatermelonCtx Ctx;
        public WatermelonState1(StateMachine machine, State parent,WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }
    }

    public class WatermelonState2 : State
    {
        WatermelonCtx Ctx;
        public WatermelonState2(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }
    }
    public class WatermelonState3 : State
    {
        WatermelonCtx Ctx;
        public WatermelonState3(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }
    }
}