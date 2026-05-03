using HSM;
using qwq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class Watermelon : Plants
{
    public override PlantsCtx plantsCtx => ctx;

    public WatermelonCtx ctx;

    protected override void Awake()
    {
        ctx.transform = transform;
        root = new WatermelonRoot(null, ctx);
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
        ctx.backward_t -= Time.deltaTime;
    }

    public override void Backward(float t)
    {
        Debug.Log(name);
        if (ctx.backward_t > 0)
            return;

        ctx.backward_t = t;
        ctx.isBackward = true;
    }
}

[Serializable]
public class WatermelonCtx : PlantsCtx
{
    public GameObject bullet;

    public int attack;

    public bool isBackward;
    public float backward_t;
    [Header("状态1")]
    public GameObject obj1;
    public float grow1 = 5;
    public int attack1 = 5;
    public float Range1 = 4;
    public float AttackSpeed1 = 1;

    [Header("状态2")]
    public GameObject obj2;
    public float g2 = 5;
    public int attack2 = 8;
    public float Range2 = 5;
    public float AttackSpeed2 = 0.5f;

    [Header("状态3")]
    public GameObject obj3;
    public int attack3 = 10;
    public float Range3 = 4;
    public float AttackSpeed3 = 1.5f;



}

namespace HSM
{
    public class WatermelonRoot : State
    {
        public WatermelonState1 state1;
        public WatermelonState2 state2;
        public WatermelonState3 state3;

        public WatermelonRoot(StateMachine machine, WatermelonCtx ctx) : base(machine, null)
        {
            state1 = new WatermelonState1(machine, this, ctx);
            state2 = new WatermelonState2(machine, this, ctx);
            state3 = new WatermelonState3(machine, this, ctx);

        }

        protected override State GetInitialState()
        {
            return state1;
        }

        protected override State GetTransition()
        {
            return null;
        }
    }

    public class WatermelonState1 : State
    {
        WatermelonCtx Ctx;
        float grow;
        float t;
        public WatermelonState1(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {

            if (grow <= 0){
                return ((WatermelonRoot)Parent).state2;
            }
            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj1.SetActive(true);
            grow = Ctx.grow1;
            t = Ctx.AttackSpeed1;
          
        }

        protected override void OnUpdate(float deltaTime)
        {
            grow -= deltaTime;
            t -= deltaTime;
            if (t <= 0)
            {
                t = Ctx.AttackSpeed1;
                if (Ctx.enemys.Count == 0)
                    return;

                GameObject bullet = GameObject.Instantiate(Ctx.bullet, Ctx.transform);
                bullet.GetComponent<WatermelonBullet>().Initialize(Ctx.attack1, Ctx.Range1, Ctx.enemys[0]);
            }
        }

        protected override void OnExit()
        {
            Ctx.obj1.SetActive(false);
        }
    }

    public class WatermelonState2 : State
    {
        WatermelonCtx Ctx;
        float grow;
        float t;

        public WatermelonState2(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (Ctx.backward_t > 0)
                return null;

            if (Ctx.isBackward)
            {
                Ctx.isBackward = false;
                grow = Ctx.grow1;
                return null;
            }


            if (grow <= 0)
                return ((WatermelonRoot)Parent).state3;

            return null;
        }

        protected override void OnEnter()
        {
            grow = Ctx.g2;
            Ctx.obj2.SetActive(true);
            t = Ctx.AttackSpeed1;

        }

        protected override void OnUpdate(float deltaTime)
        {
            if (Ctx.backward_t <= 0)
                grow -= deltaTime;

            t -= deltaTime;
            if (t <= 0)
            {
                t = Ctx.AttackSpeed2;
                if (Ctx.enemys.Count == 0)
                    return;

                GameObject bullet = GameObject.Instantiate(Ctx.bullet, Ctx.transform);
                bullet.GetComponent<WatermelonBullet>().Initialize(Ctx.attack2, Ctx.Range2, Ctx.enemys[0]);
            }
        }

        protected override void OnExit()
        {
            Ctx.obj2.SetActive(false);

        }
    }
    public class WatermelonState3 : State
    {
        WatermelonCtx Ctx;
        float t;

        public WatermelonState3(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (Ctx.isBackward)
            {
                Ctx.isBackward = false;
                return ((WatermelonRoot)Parent).state2;
            }

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj3.SetActive(true);
            t = Ctx.AttackSpeed1;

        }

        protected override void OnUpdate(float deltaTime)
        {
            t -= deltaTime;
            if (t <= 0)
            {
                t = Ctx.AttackSpeed3;
                if (Ctx.enemys.Count == 0)
                    return;

                GameObject bullet = GameObject.Instantiate(Ctx.bullet, Ctx.transform);
                bullet.GetComponent<WatermelonBullet>().Initialize(Ctx.attack3, Ctx.Range3, Ctx.enemys[0]);
            }
        }

        protected override void OnExit()
        {
            Ctx.obj3.SetActive(false);
        }
    }

}