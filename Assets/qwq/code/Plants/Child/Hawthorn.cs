using HSM;
using qwq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

namespace qwq
{
    public class Hawthorn : Plants
    {
        public HawthornCtx ctx;
        [SerializeField] private BatteryDataSO batteryData;

        public override PlantsCtx plantsCtx => ctx;

        protected override void Awake()
        {
            ctx.plant = gameObject;
            ctx.transform = transform;

            root = new HSM.HawthornRoot(null, ctx);
            base.Awake();
        }


        protected override void Update()
        {
            base.Update();
        }

    }


    [Serializable]
    public class HawthornCtx : PlantsCtx
    {
        public GameObject bullet;//子弹
        public GameObject specialEffects;
        public Transform bulletTransform;
        public Animator animator;

        [Header("状态1")]
        public GameObject obj1;
        public float grow1 = 10f;
        
        public int attack1 = 1;
        public float attackCooling1 = 1f;//冷却

        [Header("状态2")]
        public GameObject obj2;
        public float grow2 = 10f;

        public int attack2 = 1;
        public float attackCooling2 = 1f;//冷却
        [Header("状态3")]
        public GameObject obj3;
        public int attack3 = 1;
        public float attackCooling3 = 1.5f;//冷却


        [Header("逆龄盛放")]
        public int attack4 = 5;
        public float attackCooling4 = 0.3f;//冷却

        [Header("枯荣过载")]
        public int attack5 = 5;
        public float attackCooling5 = 0.8f;//冷却
        public GameObject catalysisSpecialEffects;


        public  void Attack(IDamageable target,int attack)
        {
            GameObject newBullet = GameObject.Instantiate(bullet, bulletTransform.position, bulletTransform.localRotation);
            newBullet!.GetComponent<Bullet>().Initialize(target,attack);
        }
        public void CleanupInvalidEnemyTargets()
        {
            enemys.RemoveAll(enemy =>
                enemy == null ||
                enemy.obj == null ||
                (enemy is Enemy e && !e.IsInteractable));
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
        public readonly HawthornState4 state4;
        public readonly HawthornState5 state5;
        public State state6;
        public HawthornCtx Ctx;

        public HawthornRoot(StateMachine m, HawthornCtx ctx) : base(m, null)
        {
            Ctx = ctx;
            state1 = new HawthornState1(m, this, ctx);
            state2 = new HawthornState2(m, this, ctx);
            state3 = new HawthornState3(m, this, ctx);
            state4 = new HawthornState4(m, this, ctx);
            state5 = new HawthornState5(m, this, ctx);

        }

        protected override State GetInitialState()
        {
            return state1;
        }

        protected override State GetTransition()
        {
            if (Ctx.partialBacktracking_t > 0)
            {
                if (ActiveChild != state4)
                {
                    state6 = ActiveChild;
                    return state4;
                }
                return null;
            }

            if (Ctx.catalysis_t> 0)
            {
                if (ActiveChild != state5)
                {
                    state6 = ActiveChild;
                    return state5;
                }
                return null;
            }

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
            grow = Ctx.grow1;
        }

        protected override State GetTransition()
        {
            if (grow <= 0)
                return ((HawthornRoot)Parent).state2;

            return null;
        }

        protected override void OnEnter()
        {
            t = Ctx.attackCooling1;
            Ctx.obj1.SetActive(true);
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
                t = Ctx.attackCooling1;

                if (!Ctx.EnemyDetection())
                    return;

                Ctx.Attack(Ctx.enemys[0], Ctx.attack1);
                Ctx.animator.SetTrigger("attack");
            }
        }

    }

    public class HawthornState2 : State
    {
        HawthornCtx Ctx;
        float cooling;
        public float grow;
        public HawthornState2(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
            grow = Ctx.grow2;
        }

        protected override State GetTransition()
        {
            if (Ctx.globalBacktracking_t > 0)
                return null;

            if (grow <= 0)
                return ((HawthornRoot)Parent).state3;

            return null;
        }


        protected override void OnEnter()
        {
            Ctx.obj2.SetActive(true);
            cooling = Ctx.attackCooling2;
        }

        protected override void OnExit()
        {
            Ctx.obj2.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            //成长计时
            grow -= deltaTime;

            // 如果冷却时间未结束，继续冷却
            if (cooling > 0)
            {
                cooling -= deltaTime;
                return; // 冷却期间不执行攻击逻辑
            }
            // 重置攻击冷却时间为配置值
            cooling = Ctx.attackCooling2;

            // 检测是否有敌人，若无则退出
            if (!Ctx.EnemyDetection())
                return;

            // 对第一个检测到的敌人发动攻击
            Ctx.Attack(Ctx.enemys[0], Ctx.attack2);

            // 触发攻击动画
            Ctx.animator.SetTrigger("attack");
        }
    }

    public class HawthornState3 : State
    {
        HawthornCtx Ctx;
        float cooling;
        public HawthornState3(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;

        }

        protected override State GetTransition()
        {
            if (Ctx.globalBacktracking_t > 0)
                return ((HawthornRoot)Parent).state2;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj3.SetActive(true);
            cooling = Ctx.attackCooling3;
        }

        protected override void OnExit()
        {
            Ctx.obj3.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 如果冷却时间未结束，继续冷却
            if (cooling > 0)
            {
                cooling -= deltaTime;
                return; // 冷却期间不执行攻击逻辑
            }

            // 重置攻击冷却时间为配置值
            cooling = Ctx.attackCooling3;

            // 检测是否有敌人，若无则退出
            if (!Ctx.EnemyDetection())
                return;
            // 对第一个检测到的敌人发动攻击
            Ctx.Attack(Ctx.enemys[0], Ctx.attack3);
            // 触发攻击动画
            Ctx.animator.SetTrigger("attack");
        }

    }

    public class HawthornState4 : State
    {
        HawthornCtx Ctx;
        float cooling;
        public HawthornState4(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;

        }

        protected override State GetTransition()
        {
            if (Ctx.partialBacktracking_t <= 0)
                return ((HawthornRoot)Parent).state6;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj3.SetActive(true);
            cooling = Ctx.attackCooling4;
            Ctx.specialEffects.SetActive(true);
        }

        protected override void OnExit()
        {
            Ctx.obj3.SetActive(false);
            Ctx.specialEffects.SetActive(true);
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 如果冷却时间未结束，继续冷却
            if (cooling > 0)
            {
                cooling -= deltaTime;
                return; // 冷却期间不执行攻击逻辑
            }

            // 重置攻击冷却时间为配置值
            cooling = Ctx.attackCooling4;

            // 检测是否有敌人，若无则退出
            if (!Ctx.EnemyDetection())
                return;
            // 对第一个检测到的敌人发动攻击
            Ctx.Attack(Ctx.enemys[0], Ctx.attack4);
            // 触发攻击动画
            Ctx.animator.SetTrigger("attack");
        }
    }

    public class HawthornState5 : State
    {
        HawthornCtx Ctx;
        float cooling;
        public HawthornState5(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;

        }
    }
}

