using HSM;
using qwq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;


namespace qwq
{
    public class Battery : MonoBehaviour, IBatteryBackward
    {
        public BatteryCtx ctx;

        [SerializeField] private BatteryDataSO batteryData;

        StateMachine machine;
        State root;

        private void Awake()
        {
            root = new BatteryRoot(null, ctx);
            var builder = new StateMachineBuilder(root);
            machine = builder.Build();

            //if (batteryData != null)
            //    fireInterval = batteryData.FireInterval;

            ctx.transform = transform;

        }

        private void Update()
        {
            machine.Tick(Time.deltaTime);
        }

        private void OnValidate()
        {
            //if (batteryData != null)
            //    fireInterval = batteryData.FireInterval;
        }
        public void Fire(IDamageable target)
        {


        }

        public void Backward()
        {
            ctx.isBackward = true;
        }
    }
    [Serializable]
    public class BatteryCtx : IWeapon
    {
        public GameObject bullet;
        public List<IDamageable> enemys = new();
        public Transform transform;

        [field: SerializeField] public int fertilizer { get; private set; }
        [field: SerializeField] public int diamond { get; private set; }

        [Header("生长期")]
        public GameObject obj1;
        public float attackCooling1 = 1f;
        public float grow1 = 10f;

        [Header("成熟期")]
        public GameObject obj2;
        public float attackCooling2 = 1f;
        public int bulletQuantity2 = 3;
        public float attackInterval2 = 0.3f;
        public float grow2 = 10f;
        [Header("衰老期")]
        public GameObject obj3;
        public float attackCooling3 = 1.5f;
        public int bulletQuantity3 = 2;
        public float attackInterval3 = 0.3f;

        public bool isBackward;
        public List<int> states;

        public void Fire(IDamageable target)
        {
            GameObject newBullet = GameObject.Instantiate(bullet, transform.position, transform.localRotation);
            newBullet!.GetComponent<IWeapon>().Fire(target);
        }

    }
}
public interface IBatteryBackward
{
    public void Backward();
}

namespace HSM
{
    public class BatteryRoot : State
    {
        public readonly BatteryState1 state1;
        public readonly BatteryState2 state2;
        public readonly BatteryState3 state3;
        public BatteryCtx Ctx;
        float t;
        float t_max = 0.1f;
        public BatteryRoot(StateMachine m, BatteryCtx ctx) : base(m, null)
        {
            Ctx = ctx;
            state1 = new BatteryState1(m, this, ctx);
            state2 = new BatteryState2(m, this, ctx);
            state3 = new BatteryState3(m, this, ctx);

        }

        protected override State GetInitialState()
        {
            return state1;
        }

        protected override State GetTransition()
        {
            if (Ctx.isBackward&&Ctx.states.Count>0)
            {
                Ctx.isBackward = false;
                int x = 0;
                if (Ctx.states.Count >= 50)
                {
                    x = Ctx.states[Ctx.states.Count - 49];
                    Ctx.states.RemoveRange(Ctx.states.Count - 50, 50);
                }
                else { x = Ctx.states[0];
                    Ctx.states = new();
                }
                switch (x)
                {
                    case 1:
                        return state1;
                    case 2:
                        return state2;
                    case 3: 
                        return state3;
                }

            }


            return null;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (t > 0)
            {
                t -= deltaTime;
                return;
            }
            t = t_max;
            int x = 0;
            if (ActiveChild == state1) x = 1;
            else if (ActiveChild == state2) x = 2;
            else if (ActiveChild == state3) x = 3;
            if (x != 0)
                Ctx.states.Add(x);

            if (Ctx.states.Count >= 100)
                Ctx.states.RemoveRange(0, Ctx.states.Count - 100);


        }

    }

    public class BatteryState1 : State
    {
        float t;
        public float grow;
        BatteryCtx Ctx;

        public BatteryState1(StateMachine m, State parent, BatteryCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {


            if (grow <= 0)
            {
                return ((BatteryRoot)Parent).state2;

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

    public class BatteryState2 : State
    {
        BatteryCtx Ctx;
        float cooling;
        int quantity;
        float interval;
        public float grow;
        public BatteryState2(StateMachine m, State parent, BatteryCtx ctx) : base(m, parent)
        {
            Ctx = ctx;

        }

        protected override State GetTransition()
        {

            if (grow <= 0)
            {
                return ((BatteryRoot)Parent).state3;

            }

            return null;
        }


        protected override void OnEnter()
        {
            Ctx.obj2.SetActive(true);
            cooling = Ctx.attackCooling2;
            quantity = Ctx.bulletQuantity2;
            interval = 0;
            grow = Ctx.grow2;
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

    public class BatteryState3 : State
    {
        BatteryCtx Ctx;
        float cooling;
        int quantity;
        float interval;
        public BatteryState3(StateMachine m, State parent, BatteryCtx ctx) : base(m, parent)
        {
            Ctx = ctx;

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
