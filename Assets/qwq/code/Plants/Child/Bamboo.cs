using HSM;
using qwq;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Bamboo : Plants
{
    public BambooCtx ctx;

    public override PlantsCtx plantsCtx => ctx;

    protected override void Awake()
    {
        ctx.plant = gameObject;
        ctx.BindOwner(this);
        root = new BambooRoot(null, ctx);
        base.Awake();
    }

    public override void Backward(float t)
    {
        base.Backward(t);
    }

    public void CollectRewindTargetEnemies(HashSet<Enemy> results)
    {
        ctx?.CollectRewindTargetEnemies(results);
    }

    [SerializeField] private bool drawGuardGizmosInSceneWhenNotSelected = true;

    private void OnDrawGizmos()
    {
        if (drawGuardGizmosInSceneWhenNotSelected)
            DrawGuardAreaGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGuardGizmosInSceneWhenNotSelected)
            DrawGuardAreaGizmo();
    }

    private void DrawGuardAreaGizmo()
    {
        if (ctx == null)
            return;

        float forwardOffset = Mathf.Max(0f, ctx.guardForwardOffset);
        Vector2 size = new Vector2(Mathf.Max(0.2f, ctx.guardBoxSize.x), Mathf.Max(0.2f, ctx.guardBoxSize.y));
        Vector3 center = transform.position + transform.forward * forwardOffset;
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(transform.forward, Vector3.up), Vector3.one);
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0.1f, size.y));
        Gizmos.matrix = prev;
    }
}

[Serializable]
public class BambooCtx : PlantsCtx
{
    [Header("Stage 1")]
    public GameObject obj1;
    public float grow1 = 10f;

    [Header("Stage 2")]
    public GameObject obj2;
    public float grow2 = 10f;

    [Header("Stage 3")]
    public GameObject obj3;

    [Header("Friendly Spawn")]
    public GameObject friendlyUnitPrefab;
    public FriendlyUnitDataSO friendlyUnitData;
    [Header("Stage 1 Friendly Unit")]
    public GameObject stage1FriendlyUnitPrefab;
    public FriendlyUnitDataSO stage1FriendlyUnitData;
    [Header("Stage 2 Friendly Unit")]
    public GameObject stage2FriendlyUnitPrefab;
    public FriendlyUnitDataSO stage2FriendlyUnitData;
    [Header("Stage 3 Friendly Unit")]
    public GameObject stage3FriendlyUnitPrefab;
    public FriendlyUnitDataSO stage3FriendlyUnitData;

    public float guardForwardOffset = 2f;
    public Vector2 guardBoxSize = new Vector2(4f, 3f);
    public float guardMoveSpeedScale = 1f;
    public float detectRadius = 6f;
    public float chaseRadius = 8f;
    public int defaultMaxFriendlyCount = 3;
    public BambooEnemyTriggerDetector triggerDetector;
    public Transform[] returnPoints = new Transform[3];

    [Header("Stage 1 Spawn")]
    public int stage1SpawnLimit = 1;
    public float stage1SpawnInterval = 2f;

    [Header("Stage 2 Spawn")]
    public int stage2SpawnLimit = 3;
    public float stage2SpawnInterval = 1f;

    [Header("Stage 3 Spawn")]
    public int stage3SpawnLimit = 2;
    public float stage3SpawnInterval = 1.5f;

    [NonSerialized] public readonly List<FriendlyUnit> spawnedUnits = new List<FriendlyUnit>();
    [NonSerialized] private readonly HashSet<Enemy> enemiesInTriggerRange = new HashSet<Enemy>();
    [NonSerialized] private Bamboo ownerBamboo;
    [NonSerialized] private bool warnedSlotsOutsideChase;

    public bool isBackward;
    public float Backward_t = 0f;

    private readonly float[] spawnTimers = new float[3];
    private bool warnedMissingSpawnConfig;

    public void TickSpawn(int stageIndex, float deltaTime)
    {
        CleanupDestroyedUnits();
        CleanupInvalidEnemies();

        GameObject prefab = GetStageFriendlyUnitPrefab(stageIndex);
        FriendlyUnitDataSO data = GetStageFriendlyUnitData(stageIndex);

        if (prefab == null || data == null || transform == null)
        {
            if (!warnedMissingSpawnConfig)
            {
                warnedMissingSpawnConfig = true;
                Debug.LogWarning("BambooCtx: Missing friendlyUnitPrefab or friendlyUnitData, cannot spawn friendly unit.");
            }
            return;
        }

        int stageLimit = GetStageSpawnLimit(stageIndex);
        if (stageLimit <= 0 || spawnedUnits.Count >= stageLimit)
            return;

        float interval = Mathf.Max(0.05f, GetStageSpawnInterval(stageIndex));
        spawnTimers[stageIndex] -= deltaTime;
        if (spawnTimers[stageIndex] > 0f)
            return;

        SpawnFriendlyUnit(prefab, data);
        spawnTimers[stageIndex] = interval;
    }

    public void ResetSpawnTimer(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= spawnTimers.Length)
            return;
        spawnTimers[stageIndex] = 0f;
    }

    public void CleanupDestroyedUnits()
    {
        for (int i = spawnedUnits.Count - 1; i >= 0; i--)
        {
            if (spawnedUnits[i] == null)
                spawnedUnits.RemoveAt(i);
        }
    }

    public void BindOwner(Bamboo bamboo)
    {
        ownerBamboo = bamboo;
        transform = bamboo != null ? bamboo.transform : null;
        warnedSlotsOutsideChase = false;
        ResolveTriggerDetector();
    }

    public void RegisterEnemyInRange(Enemy enemy)
    {
        if (enemy == null || !enemy.IsInteractable)
            return;
        enemiesInTriggerRange.Add(enemy);
    }

    public void UnregisterEnemyInRange(Enemy enemy)
    {
        if (enemy == null)
            return;
        enemiesInTriggerRange.Remove(enemy);
    }

    public bool IsEnemyInTriggerRange(Enemy enemy)
    {
        return enemy != null && enemy.IsInteractable && enemiesInTriggerRange.Contains(enemy);
    }

    public Enemy FindNearestEnemyInTriggerRange(Vector3 fromPos)
    {
        CleanupInvalidEnemies();
        Enemy nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (Enemy enemy in enemiesInTriggerRange)
        {
            if (enemy == null || !enemy.IsInteractable)
                continue;

            float sqr = (enemy.transform.position - fromPos).sqrMagnitude;
            if (sqr >= nearestSqr)
                continue;
            nearestSqr = sqr;
            nearest = enemy;
        }

        return nearest;
    }

    public void CollectRewindTargetEnemies(HashSet<Enemy> results)
    {
        if (results == null)
            return;

        CleanupInvalidEnemies();
        foreach (Enemy enemy in enemiesInTriggerRange)
        {
            if (enemy != null && enemy.IsInteractable)
                results.Add(enemy);
        }
    }

    public override void Death()
    {
        KillSpawnedUnits();
        base.Death();
    }

    private void KillSpawnedUnits()
    {
        CleanupDestroyedUnits();
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            FriendlyUnit unit = spawnedUnits[i];
            if (unit != null && !unit.IsDead)
                unit.ForceDeath();
        }
        spawnedUnits.Clear();
    }

    private int GetStageSpawnLimit(int stageIndex)
    {
        int configured = stageIndex switch
        {
            0 => stage1SpawnLimit,
            1 => stage2SpawnLimit,
            2 => stage3SpawnLimit,
            _ => 0
        };

        int hardLimit = Mathf.Max(0, defaultMaxFriendlyCount);
        if (hardLimit <= 0)
            return 0;

        configured = Mathf.Max(0, configured);
        return Mathf.Min(hardLimit, configured);
    }

    private float GetStageSpawnInterval(int stageIndex)
    {
        return stageIndex switch
        {
            0 => stage1SpawnInterval,
            1 => stage2SpawnInterval,
            2 => stage3SpawnInterval,
            _ => 1f
        };
    }

    private GameObject GetStageFriendlyUnitPrefab(int stageIndex)
    {
        GameObject stagePrefab = stageIndex switch
        {
            0 => stage1FriendlyUnitPrefab,
            1 => stage2FriendlyUnitPrefab,
            2 => stage3FriendlyUnitPrefab,
            _ => null
        };

        return stagePrefab != null ? stagePrefab : friendlyUnitPrefab;
    }

    private FriendlyUnitDataSO GetStageFriendlyUnitData(int stageIndex)
    {
        FriendlyUnitDataSO stageData = stageIndex switch
        {
            0 => stage1FriendlyUnitData,
            1 => stage2FriendlyUnitData,
            2 => stage3FriendlyUnitData,
            _ => null
        };

        return stageData != null ? stageData : friendlyUnitData;
    }

    private void SpawnFriendlyUnit(GameObject prefab, FriendlyUnitDataSO data)
    {
        int assignIndex = spawnedUnits.Count;
        Transform assignedReturnPoint = GetAssignedReturnPoint(assignIndex);
        Vector3 spawnPos = transform.position;
        GameObject go = GameObject.Instantiate(prefab, spawnPos, Quaternion.identity);
        FriendlyUnit unit = go.GetComponent<FriendlyUnit>();
        if (unit == null)
            unit = go.AddComponent<FriendlyUnit>();

        unit.Init(
            data,
            transform,
            guardForwardOffset,
            guardBoxSize,
            guardMoveSpeedScale,
            detectRadius,
            chaseRadius,
            this,
            assignedReturnPoint);
        spawnedUnits.Add(unit);
    }

    private Transform GetAssignedReturnPoint(int unitIndex)
    {
        if (returnPoints == null || returnPoints.Length == 0)
            return null;
        int index = Mathf.Clamp(unitIndex, 0, returnPoints.Length - 1);
        return returnPoints[index];
    }

    private void CleanupInvalidEnemies()
    {
        enemiesInTriggerRange.RemoveWhere(enemy => enemy == null || !enemy.IsInteractable);
    }

    private void ResolveTriggerDetector()
    {
        if (ownerBamboo == null)
            return;

        if (triggerDetector == null)
            triggerDetector = ownerBamboo.GetComponentInChildren<BambooEnemyTriggerDetector>(true);

        if (triggerDetector == null)
        {
            Collider2D triggerCollider = FindPreferredTriggerCollider(ownerBamboo);
            if (triggerCollider != null)
                triggerDetector = triggerCollider.gameObject.AddComponent<BambooEnemyTriggerDetector>();
        }

        if (triggerDetector != null)
            triggerDetector.SetOwner(ownerBamboo);

        EnsureReturnPoints();
    }

    private Collider2D FindPreferredTriggerCollider(Bamboo bamboo)
    {
        Collider2D[] colliders = bamboo.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col != null && col.isTrigger)
                return col;
        }
        return null;
    }

    private static readonly string[] PlantingFriendlySlotNames =
    {
        "FriendlyReturnSlot_1",
        "FriendlyReturnSlot_2",
        "FriendlyReturnSlot_3"
    };

    private bool TryBindReturnPointsFromPlantingPointHierarchy()
    {
        if (ownerBamboo == null)
            return false;

        for (Transform p = ownerBamboo.transform.parent; p != null; p = p.parent)
        {
            Transform s0 = p.Find(PlantingFriendlySlotNames[0]);
            Transform s1 = p.Find(PlantingFriendlySlotNames[1]);
            Transform s2 = p.Find(PlantingFriendlySlotNames[2]);
            if (s0 == null || s1 == null || s2 == null)
                continue;

            if (returnPoints == null || returnPoints.Length != 3)
                returnPoints = new Transform[3];
            returnPoints[0] = s0;
            returnPoints[1] = s1;
            returnPoints[2] = s2;
            return true;
        }

        return false;
    }

    private void WarnIfPlantingSlotsOutsideChaseRadius()
    {
        if (warnedSlotsOutsideChase || ownerBamboo == null || returnPoints == null)
            return;

        Vector3 center = ownerBamboo.transform.position + ownerBamboo.transform.forward * Mathf.Max(0f, guardForwardOffset);
        float r = Mathf.Max(0.1f, chaseRadius);
        float rSqr = r * r;

        for (int i = 0; i < returnPoints.Length; i++)
        {
            Transform t = returnPoints[i];
            if (t == null)
                continue;
            if ((t.position - center).sqrMagnitude > rSqr)
            {
                warnedSlotsOutsideChase = true;
                Debug.LogWarning($"BambooCtx: {PlantingFriendlySlotNames[i]} is outside chaseRadius={chaseRadius}; move the anchor closer or increase chaseRadius.", ownerBamboo);
                break;
            }
        }
    }

    private void EnsureReturnPoints()
    {
        if (ownerBamboo == null)
            return;
        if (returnPoints == null || returnPoints.Length != 3)
            returnPoints = new Transform[3];

        if (TryBindReturnPointsFromPlantingPointHierarchy())
        {
            WarnIfPlantingSlotsOutsideChaseRadius();
            return;
        }

        Vector3[] fallbackLocalOffsets = new Vector3[3]
        {
            new Vector3(-1.2f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(1.2f, 0f, 0f)
        };

        for (int i = 0; i < returnPoints.Length; i++)
        {
            if (returnPoints[i] != null)
                continue;

            string pointName = $"FriendlyReturnPoint_{i + 1}";
            Transform existing = ownerBamboo.transform.Find(pointName);
            if (existing == null)
            {
                GameObject go = new GameObject(pointName);
                existing = go.transform;
                existing.SetParent(ownerBamboo.transform, false);
                existing.localPosition = fallbackLocalOffsets[i];
            }
            returnPoints[i] = existing;
        }
    }
}

public class BambooRoot : State
{
    public BambooState1 state1;
    public BambooState2 state2;
    public BambooState3 state3;

    public BambooRoot(StateMachine m, BambooCtx ctx) : base(m, null)
    {
        state1 = new BambooState1(m, this, ctx);
        state2 = new BambooState2(m, this, ctx);
        state3 = new BambooState3(m, this, ctx);
    }

    protected override State GetInitialState() => state1;
}

public class BambooState1 : State, IPlantGrowthTimerState
{
    private readonly BambooCtx Ctx;
    public float grow;

    public BambooState1(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
    {
        Ctx = ctx;
    }

    protected override void OnEnter()
    {
        Ctx.SetGrowthStage(0, 2);
        grow = Ctx.grow1;
        Ctx.ClearGrowthProgress();
        if (Ctx.obj1 != null) Ctx.obj1.SetActive(true);
        Ctx.ResetSpawnTimer(0);
    }

    protected override State GetTransition()
    {
        if (grow <= 0f)
            return ((BambooRoot)Parent).state2;
        return null;
    }

    protected override void OnUpdate(float deltaTime)
    {
        TickStoredGrowth(deltaTime);
        Ctx.TickSpawn(0, deltaTime);
    }

    protected override void OnExit()
    {
        if (Ctx.obj1 != null) Ctx.obj1.SetActive(false);
    }

    public void TickStoredGrowth(float deltaTime)
    {
        grow = Ctx.TickGrowthTimer(grow, Ctx.grow1, deltaTime);
        Ctx.SetGrowthProgress(grow, Ctx.grow1);
    }
}

public class BambooState2 : State, IPlantGrowthTimerState
{
    private readonly BambooCtx Ctx;
    public float grow;
    private bool rewindReadyForPrevious;

    public BambooState2(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
    {
        Ctx = ctx;
    }

    protected override State GetTransition()
    {
        if (rewindReadyForPrevious)
            return ((BambooRoot)Parent).state1;

        if (grow <= 0f)
            return ((BambooRoot)Parent).state3;

        return null;
    }

    protected override void OnEnter()
    {
        Ctx.SetGrowthStage(1, 2);
        if (Ctx.obj2 != null) Ctx.obj2.SetActive(true);
        grow = Ctx.grow2;
        rewindReadyForPrevious = false;
        Ctx.ClearGrowthProgress();
        Ctx.ResetSpawnTimer(1);
    }

    protected override void OnUpdate(float deltaTime)
    {
        TickStoredGrowth(deltaTime);
        Ctx.TickSpawn(1, deltaTime);
    }

    protected override void OnExit()
    {
        if (Ctx.obj2 != null) Ctx.obj2.SetActive(false);
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
}

public class BambooState3 : State
{
    private readonly BambooCtx Ctx;

    public BambooState3(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
    {
        Ctx = ctx;
    }

    protected override State GetTransition()
    {
        if (Ctx.IsRewindingGrowth)
            return ((BambooRoot)Parent).state2;

        return null;
    }

    protected override void OnEnter()
    {
        Ctx.SetGrowthStage(2, 2);
        Ctx.SetGrowthComplete();
        if (Ctx.obj3 != null) Ctx.obj3.SetActive(true);
        Ctx.ResetSpawnTimer(2);
    }

    protected override void OnUpdate(float deltaTime)
    {
        Ctx.TickSpawn(2, deltaTime);
    }

    protected override void OnExit()
    {
        if (Ctx.obj3 != null) Ctx.obj3.SetActive(false);
    }
}
