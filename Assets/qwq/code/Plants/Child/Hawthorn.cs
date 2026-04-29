using HSM;
using qwq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace qwq
{
    public class Hawthorn : Plants
    {
        public HawthornCtx ctx;
        [SerializeField] private BatteryDataSO batteryData;

        public override PlantsCtx plantsCtx => ctx;

        protected override void Awake()
        {
            ctx.transform = transform;
            root = new HSM.HawthornRoot(null, ctx);
            base.Awake();
        }


        protected override void Update()
        {
            base.Update();
        }

        public override void Backward(float t)
        {
            if (ctx. Backward_t > 0)
                return;

          ctx.  Backward_t = t;
         ctx.   isBackward = true;
        }

    }


    [Serializable]
    public class HawthornCtx : PlantsCtx
    {
        public GameObject bullet;
        [Header("������")]
        public GameObject obj1;
        public float attackCooling1 = 1f;
        public float grow1 = 10f;

        [Header("������")]
        public GameObject obj2;
        public float attackCooling2 = 1f;
        public int bulletQuantity2 = 3;
        public float attackInterval2 = 0.3f;
        public float grow2 = 10f;
        [Header("˥����")]
        public GameObject obj3;
        public float attackCooling3 = 1.5f;
        public int bulletQuantity3 = 2;
        public float attackInterval3 = 0.3f;

        public bool isBackward;
        public float Backward_t = 0;



        public override void Fire(IDamageable target)
        {
            GameObject newBullet = GameObject.Instantiate(bullet, transform.position, transform.localRotation);
            newBullet!.GetComponent<IWeapon>().Fire(target);
        }

    }
}


namespace HSM
{
    public class HawthornRoot : State
    {
        public readonly HawthornState1 state1;
        public readonly HawthornState2 state2;
        public readonly HawthornState3 state3;
        public HawthornCtx Ctx;
        float t;
        float t_max = 0.1f;
        public HawthornRoot(StateMachine m, HawthornCtx ctx) : base(m, null)
        {
            Ctx = ctx;
            state1 = new HawthornState1(m, this, ctx);
            state2 = new HawthornState2(m, this, ctx);
            state3 = new HawthornState3(m, this, ctx);

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

    public class HawthornState1 : State
    {
        float t;
        public float grow;
        HawthornCtx Ctx;

        public HawthornState1(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {
        

            if (grow <= 0)
            {
                return ((HawthornRoot)Parent).state2;

            }

            return null;
        }

        protected override void OnEnter()
        {
            t = Ctx.attackCooling1;
            Ctx.obj1.SetActive(true);
            grow = Ctx.grow1;
        }

        protected override void OnExit()
        {
            Ctx.obj1.SetActive(false);

        }

        protected override void OnUpdate(float deltaTime)
        {
            grow -= deltaTime;

            if (t > 0)
                t -= deltaTime;
            else
            {
                if (Ctx.enemys.Count < 1) return;
                if (Ctx.enemys[0] == null) Ctx.enemys.RemoveAt(0);

                t = Ctx.attackCooling1;
                Ctx.Fire(Ctx.enemys[0]);
            }
        }
    }

    public class HawthornState2 : State
    {
        HawthornCtx Ctx;
        float cooling;
        int quantity;
        float interval;
        public float grow;
        public HawthornState2(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
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
                return ((HawthornRoot)Parent).state3;

            }

            return null;
        }


        protected override void OnEnter()
        {
            Ctx.obj2.SetActive(true);
            grow = Ctx.grow2;
            cooling = Ctx.attackCooling2;
            quantity = Ctx.bulletQuantity2;
            interval = 0;
        }

        protected override void OnExit()
        {
            Ctx.obj2.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            grow -= deltaTime;

            if (cooling > 0)
            {
                cooling -= deltaTime;
                return;
            }

            if (interval > 0)
            {
                interval -= deltaTime;
                return;
            }

            if (quantity > 0)
            {
                if (Ctx.enemys.Count < 1) return;
                if (Ctx.enemys[0] == null) Ctx.enemys.RemoveAt(0);

                Ctx.Fire(Ctx.enemys[0]);
                interval = Ctx.attackInterval2;
                quantity--;
                return;
            }

            cooling = Ctx.attackCooling2;
            quantity = Ctx.bulletQuantity2;
            interval = 0;

        }
    }

    public class HawthornState3 : State
    {
        HawthornCtx Ctx;
        float cooling;
        int quantity;
        float interval;
        public HawthornState3(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;

        }

        protected override State GetTransition()
        {
            if (Ctx.isBackward)
            {
                Ctx.isBackward = false;
                return ((HawthornRoot)Parent).state2;
            }

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj3.SetActive(true);
            cooling = Ctx.attackCooling3;
            quantity = Ctx.bulletQuantity3;
            interval = 0;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (cooling > 0)
            {
                cooling -= deltaTime;
                return;
            }

            if (interval > 0)
            {
                interval -= deltaTime;
                return;
            }

            if (quantity > 0)
            {
                if (Ctx.enemys.Count < 1) return;
                if (Ctx.enemys[0] == null) Ctx.enemys.RemoveAt(0);

                Ctx.Fire(Ctx.enemys[0]);
                interval = Ctx.attackInterval3;
                quantity--;
                return;
            }

            cooling = Ctx.attackCooling3;
            quantity = Ctx.bulletQuantity3;
            interval = 0;

        }

        protected override void OnExit()
        {
            Ctx.obj3.SetActive(false);
        }
    }
   
}
