using HSM;
using qwq;
using System;
using UnityEngine;

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
}

[Serializable]
public class WatermelonCtx : PlantsCtx
{
    public GameObject bullet;
    public Transform bulletTransform;
    public int attack;
    public GameObject specialEffects;

    [Header("Stage 1")]
    public GameObject obj1;
    public Animator animator1;
    public float grow1 = 5f;
    public float catalysis_1 = 0.2f;
    public int attack1 = 5;
    public float bulletRange1 = 1f;
    public float AttackSpeed1 = 3f;

    [Header("Stage 2")]
    public GameObject obj2;
    public float grow2 = 5f;
    public int attack2 = 8;
    public float bulletRange2 = 1f;
    public float AttackSpeed2 = 3f;
    public float catalysis_2 = 0.2f;

    [Header("Stage 3")]
    public GameObject obj3;
    public int attack3 = 10;
    public float bulletRange3 = 1f;
    public float AttackSpeed3 = 4f;

    [Header("Rewind Bloom")]
    public float attackCooldown = 5f;
    public int attack4 = 5;
    public float AttackSpeed4 = 1f;
    public float bulletRange4 = 1f;

    [Header("Catalysis Overload")]
    public int attack5 = 5;
    public float attackSpeed5 = 2f;
    public float bulletRange5 = 1f;

    public void Attack(int attack, float range, Vector2 target, bool isStrengthen = false)
    {
        if (bullet == null || bulletTransform == null)
            return;

        GameObject newBullet = GameObject.Instantiate(bullet, bulletTransform.position, bulletTransform.rotation);
        WatermelonBullet bulletComp = newBullet.GetComponent<WatermelonBullet>();
        if (bulletComp != null)
            bulletComp.Initialize(attack, range, target, isStrengthen);
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

    public class WatermelonState1 : State, IPlantGrowthTimerState
    {
        private readonly WatermelonCtx Ctx;
        private float grow;
        private float attackTimer;

        public WatermelonState1(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
            grow = Ctx.grow1;
        }

        protected override State GetTransition()
        {
            if (grow <= 0f)
                return ((WatermelonRoot)Parent).state2;
            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(0, 2);

            Ctx.obj1.SetActive(true);

            Ctx.growUI.gameObject.SetActive(true);

            attackTimer = Ctx.AttackSpeed1;

            Ctx.animator1.speed = Ctx.AttackSpeed1;
            Ctx.animator1.SetTrigger("attack");

        }

        protected override void OnUpdate(float deltaTime)
        {
            Ctx.GrowUiUpdate(Ctx.grow1 - grow, Ctx.grow1);

            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.AttackSpeed1, Ctx.attack1, Ctx.bulletRange1, false);
        }

        protected override void OnExit()
        {
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(false);

            Ctx.growUI.gameObject.SetActive(false);

        }

        public void TickStoredGrowth(float deltaTime)
        {
            grow = Ctx.TickGrowthTimer(grow, Ctx.grow1, deltaTime);
        }

        public void ResetGrowthForRewind()
        {
            grow = Ctx.grow1;
        }

        private void TickAttack(float deltaTime, float interval, int attack, float range, bool strengthened)
        {
            attackTimer -= deltaTime;
            if (attackTimer > 0f)
                return;

            attackTimer = Mathf.Max(0.05f, interval);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(attack, range, Ctx.enemys[0].obj.transform.position, strengthened);
            Ctx.animator1?.SetTrigger("attack");
        }
    }

    public class WatermelonState2 : State, IPlantGrowthTimerState
    {
        private readonly WatermelonCtx Ctx;
        private float grow;
        private float attackTimer;
        private bool rewindReadyForPrevious;

        public WatermelonState2(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
            grow = Ctx.grow2;
        }

        protected override State GetTransition()
        {
            if (rewindReadyForPrevious)
            {
                ((WatermelonRoot)Parent).state1.ResetGrowthForRewind();
                rewindReadyForPrevious = false;
                return ((WatermelonRoot)Parent).state1;
            }

            if (grow <= 0f)
                return ((WatermelonRoot)Parent).state3;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(1, 2);
         
            Ctx.obj1.SetActive(true);

            Ctx.growUI.gameObject.SetActive(true);

            Ctx.animator1.speed = Ctx.AttackSpeed2;
            attackTimer = Ctx.AttackSpeed2;
        }

        protected override void OnUpdate(float deltaTime)
        {
            Ctx.GrowUiUpdate(Ctx.grow2 - grow, Ctx.grow2);



            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.AttackSpeed2, Ctx.attack2, Ctx.bulletRange2, false);
        }

        protected override void OnExit()
        {
            Ctx.growUI.gameObject.SetActive(false);
            attackTimer = 0f;
        }

        public void TickStoredGrowth(float deltaTime)
        {
            grow = Ctx.TickGrowthTimer(grow, Ctx.grow2, deltaTime);
            if (Ctx.IsRewindingGrowth && Ctx.IsGrowthRewoundToStart(grow, Ctx.grow2))
                rewindReadyForPrevious = true;
        }

        public void ResetGrowthForRewind()
        {
            grow = Ctx.grow2;
            rewindReadyForPrevious = false;
        }

        private void TickAttack(float deltaTime, float interval, int attack, float range, bool strengthened)
        {
            attackTimer -= deltaTime;
            if (attackTimer > 0f)
                return;

            attackTimer = Mathf.Max(0.05f, interval);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(attack, range, Ctx.enemys[0].obj.transform.position, strengthened);
            Ctx.animator1?.SetTrigger("attack");
        }
    }

    public class WatermelonState3 : State
    {
        private readonly WatermelonCtx Ctx;
        private float attackTimer;

        public WatermelonState3(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (Ctx.IsRewindingGrowth)
            {
                ((WatermelonRoot)Parent).state2.ResetGrowthForRewind();
                return ((WatermelonRoot)Parent).state2;
            }

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(2, 2);
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(true);
            attackTimer = Ctx.AttackSpeed3;
            if (Ctx.animator1 != null)
            {
                Ctx.animator1.speed = Ctx.AttackSpeed3;
                if (Ctx.EnemyDetection())
                    Ctx.animator1.SetTrigger("attack");
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickAttack(deltaTime, Ctx.AttackSpeed3, Ctx.attack3, Ctx.bulletRange3, false);
        }

        protected override void OnExit()
        {
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(false);
        }

        private void TickAttack(float deltaTime, float interval, int attack, float range, bool strengthened)
        {
            attackTimer -= deltaTime;
            if (attackTimer > 0f)
                return;

            attackTimer = Mathf.Max(0.05f, interval);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(attack, range, Ctx.enemys[0].obj.transform.position, strengthened);
            Ctx.animator1?.SetTrigger("attack");
        }
    }

    public class WatermelonState4 : State
    {
        private readonly WatermelonCtx Ctx;
        private float attackTimer;
        private float attackCooldown;

        public WatermelonState4(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }


        protected override State GetTransition()
        {
            if (Ctx.partialBacktracking_t <= 0f)
                return ((WatermelonRoot)Parent).state6;

            return null;
        }
        

        protected override void OnEnter()
        {
            attackCooldown = Ctx.attackCooldown;
            attackTimer = Ctx.AttackSpeed4;
            Ctx.obj1.SetActive(true);
            Ctx.specialEffects.SetActive(true);

            Ctx.animator1.speed = Ctx.AttackSpeed4;
            Ctx.animator1.SetTrigger("attack");

        }

        protected override void OnExit()
        {
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(false);
            Ctx.specialEffects.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            WatermelonRoot root = (WatermelonRoot)Parent;
            if (Ctx.partialBacktracking_t > 0f && root.state6 is IPlantGrowthTimerState growthState)
                growthState.TickStoredGrowth(deltaTime);

            if (Ctx.partialBacktracking_t <= 0f)
            {
                attackCooldown -= deltaTime;
                return;
            }

            attackTimer -= deltaTime;
            if (attackTimer > 0f)
                return;

            attackTimer = Mathf.Max(0.05f, Ctx.AttackSpeed4);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(Ctx.attack4, Ctx.bulletRange4, Ctx.enemys[0].obj.transform.position);
            Ctx.animator1?.SetTrigger("attack");
        }
    }

    public class WatermelonState5 : State
    {
        private readonly WatermelonCtx Ctx;
        private float attackTimer;

        public WatermelonState5(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override State GetTransition()
        {


            if (Ctx.catalysis_t <= 0f)
                return ((WatermelonRoot)Parent).state6;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.obj1.SetActive(true);
            attackTimer = Ctx.attackSpeed5;
            Ctx.specialEffects.SetActive(true);
        }

        protected override void OnUpdate(float deltaTime)
        {
            WatermelonRoot root = (WatermelonRoot)Parent;
            if (Ctx.catalysis_t > 0f && root.state6 is IPlantGrowthTimerState growthState)
                growthState.TickStoredGrowth(deltaTime);

            attackTimer -= deltaTime;
            if (attackTimer > 0f)
                return;

            attackTimer = Mathf.Max(0.05f, Ctx.attackSpeed5);
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(Ctx.attack5, Ctx.bulletRange5, Ctx.enemys[0].obj.transform.position, true);
        }

        protected override void OnExit()
        {
            Ctx.specialEffects.SetActive(false);
            Ctx.obj1.SetActive(false);
        }
    }
}
