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
        public AudioClip attackSoundClip;


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
        public float grow3 = 10f;
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
            if (target == null || bullet == null || bulletTransform == null)
                return;

            GameObject newBullet = GameObject.Instantiate(bullet, bulletTransform.position, bulletTransform.localRotation);
            PlayAttackSound();
            Bullet bulletComp = newBullet.GetComponent<Bullet>();
            if (bulletComp != null)
                bulletComp.Initialize(target, attack);
        }

        private void PlayAttackSound()
        {
            if (AudioManager.Instance != null && attackSoundClip != null)
                AudioManager.Instance.PlayUISound(attackSoundClip);
        }

        public void CleanupInvalidEnemyTargets()
        {
            enemys.RemoveAll(enemy => enemy == null || enemy.obj == null || (enemy is Enemy e && !e.IsInteractable));
        }

        public int ResolveAttack(PlantSkillOverlayMode overlayMode, int normalAttack)
        {
            return overlayMode switch
            {
                PlantSkillOverlayMode.LocalRewind => attack4,
                PlantSkillOverlayMode.Catalysis => attack4,
                _ => normalAttack
            };
        }

        public float ResolveAttackCooling(PlantSkillOverlayMode overlayMode, float normalCooling)
        {
            return overlayMode switch
            {
                PlantSkillOverlayMode.LocalRewind => attackCooling4,
                PlantSkillOverlayMode.Catalysis => attackCooling4,
                _ => normalCooling
            };
        }

        public void ApplySkillOverlayEffects(PlantSkillOverlayMode overlayMode)
        {
            if (specialEffects != null)
                specialEffects.SetActive(overlayMode == PlantSkillOverlayMode.LocalRewind
                    || overlayMode == PlantSkillOverlayMode.Catalysis);

            if (catalysisSpecialEffects != null)
                catalysisSpecialEffects.SetActive(false);
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

        public HawthornRoot(StateMachine m, HawthornCtx ctx) : base(m, null)
        {
            Ctx = ctx;
            state1 = new HawthornState1(m, this, ctx);
            state2 = new HawthornState2(m, this, ctx);
            state3 = new HawthornState3(m, this, ctx);
        }

        protected override State GetInitialState() => state1;
    }

    public abstract class HawthornLifecycleState : State
    {
        protected readonly HawthornCtx Ctx;
        private PlantSkillOverlayMode activeOverlayMode;
        protected float cooling;

        protected HawthornLifecycleState(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent)
        {
            Ctx = ctx;
        }

        protected void ResetAttackCooling(float normalCooling)
        {
            activeOverlayMode = Ctx.ActiveSkillOverlayMode;
            Ctx.ApplySkillOverlayEffects(activeOverlayMode);
            cooling = Mathf.Max(0.05f, Ctx.ResolveAttackCooling(activeOverlayMode, normalCooling));
        }

        protected void TickAttack(float deltaTime, float normalCooling, int normalAttack)
        {
            PlantSkillOverlayMode overlayMode = Ctx.ActiveSkillOverlayMode;
            if (overlayMode != activeOverlayMode)
            {
                activeOverlayMode = overlayMode;
                Ctx.ApplySkillOverlayEffects(activeOverlayMode);
                cooling = Mathf.Max(0.05f, Ctx.ResolveAttackCooling(activeOverlayMode, normalCooling));
            }

            cooling -= deltaTime;
            if (cooling > 0f)
                return;

            cooling = Mathf.Max(0.05f, Ctx.ResolveAttackCooling(activeOverlayMode, normalCooling));
            if (!Ctx.EnemyDetection())
                return;

            Ctx.Attack(Ctx.enemys[0], Ctx.ResolveAttack(activeOverlayMode, normalAttack));
            Ctx.animator?.SetTrigger("attack");
        }

        protected void ClearSkillOverlayEffects()
        {
            Ctx.ApplySkillOverlayEffects(PlantSkillOverlayMode.None);
        }
    }

    public class HawthornState1 : HawthornLifecycleState, IPlantGrowthTimerState
    {
        public float grow;

        public HawthornState1(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent, ctx)
        {
            grow = Ctx.grow1;
        }

        protected override State GetTransition()
        {
            if (!Ctx.IsRewindingGrowth && grow <= 0f)
                return ((HawthornRoot)Parent).state2;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(0, 2);
            ResetAttackCooling(Ctx.attackCooling1);

            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow1);

            Ctx.obj1.SetActive(true);
        }

        protected override void OnExit()
        {
            ClearSkillOverlayEffects();
            Ctx.obj1.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.attackCooling1, Ctx.attack1);
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

    public class HawthornState2 : HawthornLifecycleState, IPlantGrowthTimerState
    {
        private bool rewindReadyForPrevious;
        public float grow;

        public HawthornState2(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent, ctx)
        {
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

            if (!Ctx.IsRewindingGrowth && grow <= 0f)
                return ((HawthornRoot)Parent).state3;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(1, 2);

            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow2);
            Ctx.obj2.SetActive(true);
            
            ResetAttackCooling(Ctx.attackCooling2);
        }

        protected override void OnExit()
        {
            ClearSkillOverlayEffects();
            Ctx.obj2.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.attackCooling2, Ctx.attack2);
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

    public class HawthornState3 : HawthornLifecycleState, IPlantGrowthTimerState
    {
        private float grow;
        private bool rewindReadyForPrevious;

        public HawthornState3(StateMachine m, State parent, HawthornCtx ctx) : base(m, parent, ctx)
        {
            grow = Ctx.grow3;
        }

        protected override State GetTransition()
        {
            if (rewindReadyForPrevious)
            {
                ((HawthornRoot)Parent).state2.ResetGrowthForRewind();
                rewindReadyForPrevious = false;
                return ((HawthornRoot)Parent).state2;
            }

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.SetGrowthStage(2, 2);
            grow = Ctx.grow3;
            rewindReadyForPrevious = false;
            Ctx.GrowUiUpdateFromRemaining(grow, Ctx.grow3);
            Ctx.obj3.SetActive(true);
            ResetAttackCooling(Ctx.attackCooling3);
        }

        protected override void OnExit()
        {
            ClearSkillOverlayEffects();
            if (Ctx.obj3 != null) Ctx.obj3.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            TickStoredGrowth(deltaTime);
            TickAttack(deltaTime, Ctx.attackCooling3, Ctx.attack3);
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
