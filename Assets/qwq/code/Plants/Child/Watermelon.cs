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
        ctx.plant = gameObject;

        root = new WatermelonRoot(null, ctx);
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
    }

    //public override void Backward(float t)
    //{
    //    Debug.Log(name);
    //    if (ctx.backward_t > 0)
    //        return;

    //    ctx.backward_t = t;
    //    ctx.isBackward = true;
    //    ctx.backward++;
    //}
}

[Serializable]
public class WatermelonCtx : PlantsCtx
{
    public GameObject bullet;

    public Transform bulletTransform;
    public int attack;

    [Header("状态1")]
    public GameObject obj1;
    public Animator animator1;
    public float grow1 = 5;
    public float catalysis_1 = 0.2f;
    public int attack1 = 5;
    public float bulletRange1 = 1;
    public float AttackSpeed1 = 3f;

    [Header("状态2")]
    public GameObject obj2;
    public float grow2 = 5;
    public int attack2 = 8;
    public float bulletRange2 = 1;
    public float AttackSpeed2 = 3f;
    public float catalysis_2 = 0.2f;

    [Header("状态3")]
    public GameObject obj3;
    public int attack3 = 10;
    public float bulletRange3 = 1;
    public float AttackSpeed3 = 4f;


    [Header("逆龄盛放")]
    public GameObject specialEffects4;
    public float attackCooldown = 5f;
    public int attack4 = 5;
    public float AttackSpeed4 = 1f;
    public float bulletRange4 = 1f;

    [Header("枯荣过载")]

    public GameObject specialEffects5;

    public int attack5 = 5;
    public float attackSpeed5 = 2f;
    public float bulletRange5 = 1f;


    public void Attack(int attack, float range, Vector2 target)
    {
        if (this.bullet == null)
            return;
        GameObject bullet = GameObject.Instantiate(this.bullet, bulletTransform.position, bulletTransform.rotation);
        bullet.GetComponent<WatermelonBullet>().Initialize(attack, range, target);
    }
}

namespace HSM
{
    public class WatermelonRoot : State
    {
        public WatermelonState1 state1;
        public WatermelonState2 state2;
        public WatermelonState3 state3;
        public WatermelonState4 state4;
        public WatermelonState5 state5;
        public WatermelonCtx Ctx;

        public State state6;

        public WatermelonRoot(StateMachine machine, WatermelonCtx ctx) : base(machine, null)
        {
            Ctx = ctx;
            state1 = new WatermelonState1(machine, this, ctx);
            state2 = new WatermelonState2(machine, this, ctx);
            state3 = new WatermelonState3(machine, this, ctx);
            state4 = new WatermelonState4(machine, this, ctx);
            state5 = new WatermelonState5(machine, this, ctx);
        }

        protected override State GetInitialState()
        {
            return state1;
        }

        protected override State GetTransition()
        {
            if (Ctx.partialBacktracking_t > 0 && ActiveChild != state4){
                state6 = ActiveChild;
                return state4;
            }
         
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
            grow = Ctx.grow1;
        }

        protected override State GetTransition()
        {

            if (grow <= 0)
            {
                return ((WatermelonRoot)Parent).state2;
            }
            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj1.SetActive(true);
            t = Ctx.AttackSpeed1;
            Ctx.animator1.speed = Ctx.AttackSpeed1;

            if (Ctx.enemys.Count >= 0)
                Ctx.animator1.SetTrigger("attack");
        }

        protected override void OnUpdate(float deltaTime)
        {
            grow -= deltaTime;
            t -= deltaTime;
            if (t <= 0)
            {
                t = Ctx.AttackSpeed1;
                if (Ctx.enemys.Count >= 0)
                    return;
                Ctx.Attack(Ctx.attack1, Ctx.bulletRange1, Ctx.enemys[0].obj.transform.position);
                Ctx.animator1.SetTrigger("attack");

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
        Vector2 target;

        public WatermelonState2(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
            grow = Ctx.grow2;

        }

        protected override State GetTransition()
        {
            if (Ctx.globalBacktracking_t > 0)
                return null;



            if (grow <= 0)
                return ((WatermelonRoot)Parent).state3;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj1.SetActive(true);
            Ctx.animator1.speed = Ctx.AttackSpeed2;

        }

        protected override void OnUpdate(float deltaTime)
        {
            if (Ctx.globalBacktracking_t <= 0)
                grow -= deltaTime;

            t -= deltaTime;
            if (t <= 0)
            {
                t = Ctx.AttackSpeed2;
                if (Ctx.enemys.Count == 0)
                    return;
                Ctx.Attack(Ctx.attack2, Ctx.bulletRange2, Ctx.enemys[0].obj.transform.position);
                Ctx.animator1.SetTrigger("attack");
            }
        }

        protected override void OnExit()
        {
            Ctx.obj1.SetActive(false);
            t = 0;
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
            if (Ctx.globalBacktracking_t>0)//全局回溯时间大于0回溯
                return ((WatermelonRoot)Parent).state2;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj1.SetActive(true);
            t = Ctx.AttackSpeed3;
            Ctx.animator1.speed = Ctx.AttackSpeed3;

            if (Ctx.enemys.Count > 0)
                Ctx.animator1.SetTrigger("attack");
        }

        protected override void OnUpdate(float deltaTime)
        {
            t -= deltaTime;
            if (t <= 0)
            {
                t = Ctx.AttackSpeed3;
                if (Ctx.enemys.Count == 0)
                    return;

                Ctx.Attack(Ctx.attack3, Ctx.bulletRange3, Ctx.enemys[0].obj.transform.position);

                Ctx.animator1.SetTrigger("attack");
            }
        }

        protected override void OnExit()
        {
            Ctx.obj1.SetActive(false);
        }
    }

    public class WatermelonState4 : State
    {
        WatermelonCtx Ctx;
        float t;
        float attackCooldown;
        public WatermelonState4(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
           
        }

        protected override State GetTransition()
        {
            if (Ctx.partialBacktracking_t <= 0 && attackCooldown <= 0)
                return ((WatermelonRoot)Parent).state6;

            return null;
        }

        protected override void OnEnter()
        {
            attackCooldown = Ctx.attackCooldown;
            t = Ctx.AttackSpeed4;
            Ctx.obj1.SetActive(true);
            Ctx.specialEffects5.SetActive(true);
            Ctx.animator1.speed = Ctx.AttackSpeed4;

            if (Ctx.enemys.Count > 0)
                Ctx.animator1.SetTrigger("attack");
        }

        protected override void OnExit()
        {
            Ctx.obj1.SetActive(false);
            Ctx.specialEffects5.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (Ctx.partialBacktracking_t <= 0)
            {
                attackCooldown -= deltaTime;
                return;
            }

            t -= deltaTime;
            if (t <= 0)
            {
                t = Ctx.AttackSpeed4;
                if (Ctx.enemys.Count == 0)
                    return;

                Ctx.Attack(Ctx.attack4, Ctx.bulletRange4, Ctx.enemys[0].obj.transform.position);
                Ctx.animator1.SetTrigger("attack");
            }



        }
    }

    public class WatermelonState5 : State
    {
        WatermelonCtx Ctx;
        float attackSpeed;
        public WatermelonState5(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {
            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj1.SetActive(true);
            attackSpeed = Ctx.attackSpeed5;
            Ctx.specialEffects5.SetActive(true);
        }

        protected override void OnUpdate(float deltaTime)
        {
            attackSpeed -= deltaTime;
            if (attackSpeed <= 0)
            {
                attackSpeed = Ctx.attackSpeed5;
                if (Ctx.enemys.Count == 0)
                    return;

                Ctx.Attack(Ctx.attack5, Ctx.bulletRange5, Ctx.enemys[0].obj.transform.position);
            }
        }

        protected override void OnExit()
        {
            Ctx.specialEffects5.SetActive(false);
            Ctx.obj1.SetActive(false);
            Ctx.catalysisNumber++;
            if (Ctx.catalysisNumber >= 4)
            {
                Ctx.Death();
            }
        }

    }

}