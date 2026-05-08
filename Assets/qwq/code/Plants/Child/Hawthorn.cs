using HSM;
using qwq;
using System;
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
            ctx.plant = gameObject;
            ctx.transform = transform;
            root = new HSM.HawthornRoot(null, ctx);
            base.Awake();
        }
    }

    [Serializable]
    public class HawthornCtx : PlantsCtx
    {
        public GameObject bullet;
        public GameObject specialEffects;
        public Transform bulletTransform;
        public Animator animator;

        [Header("Stage 1")]
        public GameObject obj1;
        public float grow1 = 10f;
        public int attack1 = 1;
        public float attackCooling1 = 1f;

        [Header("Stage 2")]
        public GameObject obj2;
        public float grow2 = 10f;
        public int attack2 = 1;
        public float attackCooling2 = 1f;

        [Header("Stage 3")]
        public GameObject obj3;
        public int attack3 = 1;
        public float attackCooling3 = 1.5f;

        [Header("Rewind Bloom")]
        public int attack4 = 5;
        public float attackCooling4 = 0.3f;

        [Header("Catalysis Overload")]
        public int attack5 = 5;
        public float attackCooling5 = 0.8f;
        public GameObject catalysisSpecialEffects;

        public void Attack(IDamageable target, int attack)
        {
            if (!DamageableTargetUtility.IsValid(target) || bullet == null || bulletTransform == null)
                return;

            GameObject newBullet = GameObject.Instantiate(bullet, bulletTransform.position, bulletTransform.localRotation);
            Bullet bulletComp = newBullet.GetComponent<Bullet>();
            if (bulletComp != null)
                bulletComp.Initialize(target, attack);
        }

        public void CleanupInvalidEnemyTargets()
        {
            enemys.RemoveAll(enemy => !DamageableTargetUtility.IsValid(enemy) || (enemy is Enemy e && !e.IsInteractable));
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

        protected override State GetInitialState() => state1;

        protected override State GetTransition()
        {
            if (Ctx.partialBacktracking_t > 0f)
            {
                if (ActiveChild != state4)
                {
                    state6 = ActiveChild;
                    return state4;
                }
                return null;
            }

            if (Ctx.catalysis_t > 0f)
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

    public class HawthornState1 : State, IPlantGrowthTimerState
    {
        private readonly HawthornCtx Ctx;
        private float cooling;
        public float grow;

        public HawthornState1(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
            grow = Ctx.grow1;
        }

        protected override State GetTransition()
        {
            if (grow <= 0f)
                return ((HawthornRoot)Parent).state2;
            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(0, 2);
            cooling = Ctx.attackCooling1;
            Ctx.ClearGrowthProgress();
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(true);
        }

        protected override void OnExit()
        {
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.attackCooling1, Ctx.attack1);
        }

        public void TickStoredGrowth(float deltaTime)
        {
            grow = Ctx.TickGrowthTimer(grow, Ctx.grow1, deltaTime);
            Ctx.SetGrowthProgress(grow, Ctx.grow1);
        }

        public void ResetGrowthForRewind()
        {
            grow = Ctx.grow1;
        }

        private void TickAttack(float deltaTime, float interval, int attack)
        {
            cooling -= deltaTime;
            if (cooling > 0f)
                return;

            cooling = Mathf.Max(0.05f, interval);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(Ctx.enemys[0], attack);
            Ctx.animator?.SetTrigger("attack");
        }
    }

    public class HawthornState2 : State, IPlantGrowthTimerState
    {
        private readonly HawthornCtx Ctx;
        private float cooling;
        private bool rewindReadyForPrevious;
        public float grow;

        public HawthornState2(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
            grow = Ctx.grow2;
        }

        protected override State GetTransition()
        {
            if (rewindReadyForPrevious)
            {
                ((HawthornRoot)Parent).state1.ResetGrowthForRewind();
                rewindReadyForPrevious = false;
                return ((HawthornRoot)Parent).state1;
            }

            if (grow <= 0f)
                return ((HawthornRoot)Parent).state3;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(1, 2);
            if (Ctx.obj2 != null) Ctx.obj2.SetActive(true);
            cooling = Ctx.attackCooling2;
            Ctx.ClearGrowthProgress();
        }

        protected override void OnExit()
        {
            if (Ctx.obj2 != null) Ctx.obj2.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.attackCooling2, Ctx.attack2);
        }

        public void TickStoredGrowth(float deltaTime)
        {
            grow = Ctx.TickGrowthTimer(grow, Ctx.grow2, deltaTime);
            if (Ctx.IsRewindingGrowth && Ctx.IsGrowthRewoundToStart(grow, Ctx.grow2))
            {
                Ctx.ClearGrowthProgress();
                rewindReadyForPrevious = true;
            }
            else
            {
                Ctx.SetGrowthProgress(grow, Ctx.grow2);
            }
        }

        public void ResetGrowthForRewind()
        {
            grow = Ctx.grow2;
            rewindReadyForPrevious = false;
        }

        private void TickAttack(float deltaTime, float interval, int attack)
        {
            cooling -= deltaTime;
            if (cooling > 0f)
                return;

            cooling = Mathf.Max(0.05f, interval);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(Ctx.enemys[0], attack);
            Ctx.animator?.SetTrigger("attack");
        }
    }

    public class HawthornState3 : State
    {
        private readonly HawthornCtx Ctx;
        private float cooling;

        public HawthornState3(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (Ctx.IsRewindingGrowth)
            {
                ((HawthornRoot)Parent).state2.ResetGrowthForRewind();
                return ((HawthornRoot)Parent).state2;
            }

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(2, 2);
            Ctx.SetGrowthComplete();
            if (Ctx.obj3 != null) Ctx.obj3.SetActive(true);
            cooling = Ctx.attackCooling3;
        }

        protected override void OnExit()
        {
            if (Ctx.obj3 != null) Ctx.obj3.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickAttack(deltaTime, Ctx.attackCooling3, Ctx.attack3);
        }

        private void TickAttack(float deltaTime, float interval, int attack)
        {
            cooling -= deltaTime;
            if (cooling > 0f)
                return;

            cooling = Mathf.Max(0.05f, interval);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(Ctx.enemys[0], attack);
            Ctx.animator?.SetTrigger("attack");
        }
    }

    public class HawthornState4 : State
    {
        private readonly HawthornCtx Ctx;
        private float cooling;

        public HawthornState4(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (Ctx.partialBacktracking_t <= 0f)
                return ((HawthornRoot)Parent).state6;

            return null;

        }

        protected override void OnEnter()
        {
            if (Ctx.obj3 != null) Ctx.obj3.SetActive(true);
            cooling = Ctx.attackCooling4;
            if (Ctx.specialEffects != null) Ctx.specialEffects.SetActive(true);
        }

        protected override void OnExit()
        {
            if (Ctx.obj3 != null) Ctx.obj3.SetActive(false);
            if (Ctx.specialEffects != null) Ctx.specialEffects.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            HawthornRoot root = (HawthornRoot)Parent;
            if (Ctx.partialBacktracking_t > 0f && root.state6 is IPlantGrowthTimerState growthState)
                growthState.TickStoredGrowth(deltaTime);

            cooling -= deltaTime;
            if (cooling > 0f)
                return;

            cooling = Mathf.Max(0.05f, Ctx.attackCooling4);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(Ctx.enemys[0], Ctx.attack4);
            Ctx.animator?.SetTrigger("attack");
        }
    }

    public class HawthornState5 : State
    {
        private readonly HawthornCtx Ctx;
        private float cooling;

        public HawthornState5(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (Ctx.catalysis_t <= 0f)
                return ((HawthornRoot)Parent).state6;

            return null;
        }

        protected override void OnEnter()
        {
            if (Ctx.obj3 != null) Ctx.obj3.SetActive(true);
            if (Ctx.catalysisSpecialEffects != null) Ctx.catalysisSpecialEffects.SetActive(true);
            cooling = Ctx.attackCooling5;
        }

        protected override void OnExit()
        {
            if (Ctx.obj3 != null) Ctx.obj3.SetActive(false);
            if (Ctx.catalysisSpecialEffects != null) Ctx.catalysisSpecialEffects.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            HawthornRoot root = (HawthornRoot)Parent;
            if (Ctx.catalysis_t > 0f && root.state6 is IPlantGrowthTimerState growthState)
                growthState.TickStoredGrowth(deltaTime);

            cooling -= deltaTime;
            if (cooling > 0f)
                return;

            cooling = Mathf.Max(0.05f, Ctx.attackCooling5);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(Ctx.enemys[0], Ctx.attack5);
            Ctx.animator?.SetTrigger("attack");
        }
    }
}
