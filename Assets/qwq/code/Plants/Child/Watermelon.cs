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
    public float grow3 = 10f;
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

    public int ResolveAttack(PlantSkillOverlayMode overlayMode, int normalAttack)
    {
        return overlayMode switch
        {
            PlantSkillOverlayMode.LocalRewind => attack4,
            PlantSkillOverlayMode.Catalysis => attack5,
            _ => normalAttack
        };
    }

    public float ResolveAttackInterval(PlantSkillOverlayMode overlayMode, float normalInterval)
    {
        return overlayMode switch
        {
            PlantSkillOverlayMode.LocalRewind => AttackSpeed4,
            PlantSkillOverlayMode.Catalysis => attackSpeed5,
            _ => normalInterval
        };
    }

    public float ResolveBulletRange(PlantSkillOverlayMode overlayMode, float normalRange)
    {
        return overlayMode switch
        {
            PlantSkillOverlayMode.LocalRewind => bulletRange4,
            PlantSkillOverlayMode.Catalysis => bulletRange5,
            _ => normalRange
        };
    }

    public bool ResolveStrengthened(PlantSkillOverlayMode overlayMode, bool normalStrengthened)
    {
        return overlayMode == PlantSkillOverlayMode.Catalysis || normalStrengthened;
    }

    public void ApplySkillOverlayEffects(PlantSkillOverlayMode overlayMode)
    {
        if (specialEffects != null)
            specialEffects.SetActive(overlayMode != PlantSkillOverlayMode.None);
    }
}

namespace HSM
{
    public class WatermelonRoot : State
    {
        public WatermelonState1 state1;
        public WatermelonState2 state2;
        public WatermelonState3 state3;
        public WatermelonCtx Ctx;

        public WatermelonRoot(StateMachine machine, WatermelonCtx ctx) : base(machine, null)
        {
            Ctx = ctx;
            state1 = new WatermelonState1(machine, this, ctx);
            state2 = new WatermelonState2(machine, this, ctx);
            state3 = new WatermelonState3(machine, this, ctx);
        }

        protected override State GetInitialState() => state1;
    }

    public abstract class WatermelonLifecycleState : State
    {
        protected readonly WatermelonCtx Ctx;
        private PlantSkillOverlayMode activeOverlayMode;
        protected float attackTimer;

        protected WatermelonLifecycleState(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected void ResetAttackTimer(float normalInterval)
        {
            activeOverlayMode = Ctx.ActiveSkillOverlayMode;
            Ctx.ApplySkillOverlayEffects(activeOverlayMode);
            float interval = Mathf.Max(0.05f, Ctx.ResolveAttackInterval(activeOverlayMode, normalInterval));
            attackTimer = interval;
            if (Ctx.animator1 != null)
                Ctx.animator1.speed = interval;
        }

        protected void TickAttack(float deltaTime, float normalInterval, int normalAttack, float normalRange, bool normalStrengthened)
        {
            PlantSkillOverlayMode overlayMode = Ctx.ActiveSkillOverlayMode;
            if (overlayMode != activeOverlayMode)
            {
                activeOverlayMode = overlayMode;
                Ctx.ApplySkillOverlayEffects(activeOverlayMode);
                float resetInterval = Mathf.Max(0.05f, Ctx.ResolveAttackInterval(activeOverlayMode, normalInterval));
                attackTimer = resetInterval;
                if (Ctx.animator1 != null)
                    Ctx.animator1.speed = resetInterval;
            }

            attackTimer -= deltaTime;
            if (attackTimer > 0f)
                return;

            float interval = Mathf.Max(0.05f, Ctx.ResolveAttackInterval(activeOverlayMode, normalInterval));
            attackTimer = interval;
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(
                Ctx.ResolveAttack(activeOverlayMode, normalAttack),
                Ctx.ResolveBulletRange(activeOverlayMode, normalRange),
                Ctx.enemys[0].obj.transform.position,
                Ctx.ResolveStrengthened(activeOverlayMode, normalStrengthened));
            Ctx.animator1?.SetTrigger("attack");
        }

        protected void ClearSkillOverlayEffects()
        {
            Ctx.ApplySkillOverlayEffects(PlantSkillOverlayMode.None);
        }
    }

    public class WatermelonState1 : WatermelonLifecycleState, IPlantGrowthTimerState
    {
        private float grow;

        public WatermelonState1(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent, ctx)
        {
            grow = Ctx.grow1;
        }

        protected override State GetTransition()
        {
            if (!Ctx.IsRewindingGrowth && grow <= 0f)
                return ((WatermelonRoot)Parent).state2;
            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(0, 2);

            Ctx.obj1.SetActive(true);

            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow1);

            ResetAttackTimer(Ctx.AttackSpeed1);
            Ctx.animator1?.SetTrigger("attack");

        }

        protected override void OnUpdate(float deltaTime)
        {
            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.AttackSpeed1, Ctx.attack1, Ctx.bulletRange1, false);
        }

        protected override void OnExit()
        {
            ClearSkillOverlayEffects();
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(false);
        }

        public void TickStoredGrowth(float deltaTime)
        {
            grow = Ctx.TickGrowthTimer(grow, Ctx.grow1, deltaTime);
            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow1);
        }

        public void ResetGrowthForRewind()
        {
            grow = 0f;
            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow1);
        }

    }

    public class WatermelonState2 : WatermelonLifecycleState, IPlantGrowthTimerState
    {
        private float grow;
        private bool rewindReadyForPrevious;

        public WatermelonState2(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent, ctx)
        {
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

            if (!Ctx.IsRewindingGrowth && grow <= 0f)
                return ((WatermelonRoot)Parent).state3;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(1, 2);
         
            Ctx.obj1.SetActive(true);

            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow2);

            ResetAttackTimer(Ctx.AttackSpeed2);
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.AttackSpeed2, Ctx.attack2, Ctx.bulletRange2, false);
        }

        protected override void OnExit()
        {
            ClearSkillOverlayEffects();
            attackTimer = 0f;
        }

        public void TickStoredGrowth(float deltaTime)
        {
            grow = Ctx.TickGrowthTimer(grow, Ctx.grow2, deltaTime);
            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow2);
            if (Ctx.IsRewindingGrowth && Ctx.IsGrowthRewoundToStart(grow, Ctx.grow2))
                rewindReadyForPrevious = true;
        }

        public void ResetGrowthForRewind()
        {
            grow = 0f;
            rewindReadyForPrevious = false;
            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow2);
        }

    }

    public class WatermelonState3 : WatermelonLifecycleState, IPlantGrowthTimerState
    {
        private float grow;
        private bool rewindReadyForPrevious;

        public WatermelonState3(StateMachine machine, State parent, WatermelonCtx ctx) : base(machine, parent, ctx)
        {
            grow = Ctx.grow3;
        }

        protected override State GetTransition()
        {
            if (rewindReadyForPrevious)
            {
                ((WatermelonRoot)Parent).state2.ResetGrowthForRewind();
                rewindReadyForPrevious = false;
                return ((WatermelonRoot)Parent).state2;
            }

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(2, 2);
            grow = Ctx.grow3;
            rewindReadyForPrevious = false;
            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow3);
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(true);
            ResetAttackTimer(Ctx.AttackSpeed3);
            if (Ctx.animator1 != null)
            {
                if (Ctx.EnemyDetection())
                    Ctx.animator1.SetTrigger("attack");
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.AttackSpeed3, Ctx.attack3, Ctx.bulletRange3, false);
        }

        protected override void OnExit()
        {
            ClearSkillOverlayEffects();
            if (Ctx.obj1 != null) Ctx.obj1.SetActive(false);
        }

        public void TickStoredGrowth(float deltaTime)
        {
            grow = Mathf.Max(0f, Ctx.TickGrowthTimer(grow, Ctx.grow3, deltaTime));
            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow3);
            if (Ctx.IsRewindingGrowth && Ctx.IsGrowthRewoundToStart(grow, Ctx.grow3))
                rewindReadyForPrevious = true;
        }
    }
}
