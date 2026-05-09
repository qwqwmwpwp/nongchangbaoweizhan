using HSM;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace qwq
{
    public enum PlantSkillOverlayMode
    {
        None,
        LocalRewind,
        Catalysis
    }

    public abstract class Plants : MonoBehaviour, IBatteryBackward
    {
        protected StateMachine machine;
        protected State root;

        public abstract PlantsCtx plantsCtx { get; }

        protected virtual void Awake()
        {
            StateMachineBuilder builder = new(root);
            machine = builder.Build();
        }

        protected virtual void OnEnable()
        {
            GameEvent.EnemyRewindRequested += HandleRewindRequested;
        }

        protected virtual void OnDisable()
        {
            GameEvent.EnemyRewindRequested -= HandleRewindRequested;
        }

        protected virtual void Update()
        {
            if (plantsCtx == null)
                return;

            plantsCtx.TickSkillTimers(Time.deltaTime);
            machine?.Tick(Time.deltaTime);
        }

        private void HandleRewindRequested(float rewindSeconds, float playbackDuration)
        {
            if (plantsCtx == null || plantsCtx.globalBacktracking_t > 0f)
                return;

            plantsCtx.globalBacktracking_t = Mathf.Max(0.05f, rewindSeconds);
            plantsCtx.RegisterRewindSkillUse();
        }

        public virtual void Backward(float t)
        {
            if (plantsCtx == null)
                return;

            plantsCtx.partialBacktracking_t = Mathf.Max(0.05f, t);
            plantsCtx.RegisterRewindSkillUse();
        }

        public void Catalysis(float t = 3f)
        {
            if (plantsCtx == null)
                return;

            Catalysis(
                t,
                plantsCtx.catalysisGrowthSpeedMultiplier,
                plantsCtx.boundaryCatalysisDeathThreshold,
                plantsCtx.catalysisOverlayMode);
        }

        public void Catalysis(
            float t,
            float growthSpeedMultiplier,
            int deathThreshold,
            PlantSkillOverlayMode overlayMode)
        {
            if (plantsCtx == null)
                return;

            plantsCtx.catalysisGrowthSpeedMultiplier = Mathf.Max(1f, growthSpeedMultiplier);
            plantsCtx.boundaryCatalysisDeathThreshold = Mathf.Max(0, deathThreshold);
            plantsCtx.catalysisOverlayMode = overlayMode;
            plantsCtx.catalysis_t = Mathf.Max(0.05f, t);
            plantsCtx.RegisterCatalysisSkillUse();
        }
    }

    [Serializable]
    public class PlantsCtx
    {
        [Header("UI")]
        public Sprite UI;//种植ui
        public Slider growUI;

        [HideInInspector] public GameObject plant;

        [HideInInspector] public Transform transform;
        public List<IDamageable> enemys = new();
        [field: SerializeField] public int fertilizer { get; private set; }
        [field: SerializeField] public int diamond { get; private set; }

        [Header("Global Rewind")]
        public float globalBacktracking_t = 0f;
        [Header("Local Rewind")]
        public float partialBacktracking_t = 0f;
        [HideInInspector] public int backward = 0;
        [Header("Growth Catalysis")]
        public int catalysisNumber = 0;
        public float catalysis_t = 0f;
        [Tooltip("Skill resource set used while catalysis is active. LocalRewind reuses the rewind bloom resources.")]
        public PlantSkillOverlayMode catalysisOverlayMode = PlantSkillOverlayMode.LocalRewind;

        [Header("Growth Skill Tuning")]
        [Tooltip("Total growth speed multiplier while catalysis_t is active.")]
        public float catalysisGrowthSpeedMultiplier = 2f;
        [Tooltip("How fast remaining growth time is restored while rewind is active.")]
        public float rewindGrowthSpeedMultiplier = 1f;
        [Tooltip("Skill uses at the first stage before the plant is destroyed. <= 0 disables this death rule.")]
        public int boundaryRewindDeathThreshold = 4;
        [Tooltip("Skill uses at the final stage before the plant is destroyed. <= 0 disables this death rule.")]
        public int boundaryCatalysisDeathThreshold = 4;
        public float growthTimerEpsilon = 0.01f;

        [HideInInspector] public int currentGrowthStageIndex;
        [HideInInspector] public int finalGrowthStageIndex = 2;

        public bool IsRewindingGrowth => partialBacktracking_t > 0f || globalBacktracking_t > 0f;
        public bool IsCatalyzingGrowth => catalysis_t > 0f;
        public PlantSkillOverlayMode ActiveSkillOverlayMode
        {
            get
            {
                if (partialBacktracking_t > 0f)
                    return PlantSkillOverlayMode.LocalRewind;
                if (catalysis_t > 0f)
                    return catalysisOverlayMode;
                return PlantSkillOverlayMode.None;
            }
        }
        public bool IsAtFirstGrowthStage => currentGrowthStageIndex <= 0;
        public bool IsAtFinalGrowthStage => currentGrowthStageIndex >= finalGrowthStageIndex;

        public void TickSkillTimers(float deltaTime)
        {
            if (partialBacktracking_t > 0f)
                partialBacktracking_t = Mathf.Max(0f, partialBacktracking_t - deltaTime);

            if (globalBacktracking_t > 0f)
                globalBacktracking_t = Mathf.Max(0f, globalBacktracking_t - deltaTime);

            if (catalysis_t > 0f)
                catalysis_t = Mathf.Max(0f, catalysis_t - deltaTime);
        }

        public void SetGrowthStage(int stageIndex, int finalStageIndex)
        {
            currentGrowthStageIndex = Mathf.Max(0, stageIndex);
            this.finalGrowthStageIndex = Mathf.Max(currentGrowthStageIndex, finalStageIndex);
        }

        public float TickGrowthTimer(float remaining, float maxRemaining, float deltaTime)
        {
            float cappedMax = Mathf.Max(0f, maxRemaining);
            if (IsRewindingGrowth)
                return Mathf.Min(cappedMax, remaining + deltaTime * Mathf.Max(0f, rewindGrowthSpeedMultiplier));

            float multiplier = IsCatalyzingGrowth ? Mathf.Max(1f, catalysisGrowthSpeedMultiplier) : 1f;
            return remaining - deltaTime * multiplier;
        }

        public bool IsGrowthRewoundToStart(float remaining, float maxRemaining)
        {
            return remaining >= Mathf.Max(0f, maxRemaining) - Mathf.Max(0f, growthTimerEpsilon);
        }

        public void RegisterRewindSkillUse()
        {
            if (!IsAtFirstGrowthStage)
                return;

            backward++;
            if (boundaryRewindDeathThreshold > 0 && backward >= boundaryRewindDeathThreshold)
                Death();
        }

        public void RegisterCatalysisSkillUse()
        {
            if (!IsAtFinalGrowthStage)
                return;

            catalysisNumber++;
            if (boundaryCatalysisDeathThreshold > 0 && catalysisNumber >= boundaryCatalysisDeathThreshold)
                Death();
        }

        public virtual void Death()
        {
            if (plant == null)
                return;

            GameObject deadPlant = plant;
            plant = null;
            GameObject.Destroy(deadPlant);
        }

        public bool EnemyDetection()
        {
            for (int i = enemys.Count - 1; i >= 0; i--)
            {
                if (enemys[i] == null || enemys[i].obj == null)
                    enemys.RemoveAt(i);
            }

            enemys.Sort((a, b) =>
            {
                float aDist = (a.obj.transform.position - transform.position).magnitude;
                float bDist = (b.obj.transform.position - transform.position).magnitude;
                return aDist.CompareTo(bDist);
            });

            return enemys.Count > 0;
        }

        public void GrowUiUpdate(float x, float max)
        {
            if (growUI == null)
                return;

            growUI.gameObject.SetActive(true);
            growUI.minValue = 0f;
            growUI.maxValue = 1f;

            float n = max > 0f ? Mathf.Clamp01(x / max) : 1f;
            growUI.value = n;
        }

        public void GrowUiUpdateFromRemaining(float remaining, float maxRemaining)
        {
            float max = Mathf.Max(0f, maxRemaining);
            GrowUiUpdate(max - remaining, max);
        }
    }

    public interface IBatteryBackward
    {
        void Backward(float t);
    }

    public interface IPlantGrowthTimerState
    {
        void TickStoredGrowth(float deltaTime);
    }
}
