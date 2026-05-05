using HSM;
using qwq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bamboo : Plants
{
    public BambooCtx ctx;

    public override PlantsCtx plantsCtx => ctx;

    protected override void Awake()
    {
        ctx.BindOwner(this);
        root = new BambooRoot(null, ctx);
        base.Awake();
    }

    public override void Backward(float t)
    {
        if (ctx.Backward_t > 0f)
            return;

        ctx.Backward_t = t;
        ctx.isBackward = true;
    }

    [Tooltip("在 Scene 中未选中竹子时也绘制驻守区域（调 ctx 数值时不必保持选中 Hierarchy）。")]
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
    [Header("生长阶段1")]
    public GameObject obj1;
    [Tooltip("阶段1持续时间（秒）。")]
    public float grow1 = 10f;

    [Header("生长阶段2")]
    public GameObject obj2;
    [Tooltip("阶段2持续时间（秒）。")]
    public float grow2 = 10f;

    [Header("衰老阶段")]
    public GameObject obj3;

    [Header("友军生成")]
    [Tooltip("友军单位预制体。")]
    public GameObject friendlyUnitPrefab;
    [Tooltip("友军属性数据（血量、攻击力、移速等）。")]
    public FriendlyUnitDataSO friendlyUnitData;
    [Tooltip("驻守区域相对竹子的前向偏移。")]
    public float guardForwardOffset = 2f;
    [Tooltip("驻守区域尺寸：X=宽度，Y=深度。")]
    public Vector2 guardBoxSize = new Vector2(4f, 3f);
    [Tooltip("待机/回防移动速度倍率。")]
    public float guardMoveSpeedScale = 1f;
    [Tooltip("友军索敌半径。")]
    public float detectRadius = 6f;
    [Tooltip("友军追击半径（以当前驻守区中心为基准）。")]
    public float chaseRadius = 8f;
    [Tooltip("全局友军数量硬上限。")]
    public int defaultMaxFriendlyCount = 3;
    [Tooltip("竹子触发器检测器（为空时会自动查找子物体上的检测器）。")]
    public BambooEnemyTriggerDetector triggerDetector;
    [Tooltip("三个友军对应的回位点（按生成顺序分配）。")]
    public Transform[] returnPoints = new Transform[3];

    [Header("阶段1生成")]
    public int stage1SpawnLimit = 1;
    public float stage1SpawnInterval = 2f;

    [Header("阶段2生成")]
    public int stage2SpawnLimit = 3;
    public float stage2SpawnInterval = 1f;

    [Header("阶段3生成")]
    public int stage3SpawnLimit = 2;
    public float stage3SpawnInterval = 1.5f;

    [NonSerialized] public readonly List<FriendlyUnit> spawnedUnits = new List<FriendlyUnit>();
    [NonSerialized] private readonly HashSet<Enemy> enemiesInTriggerRange = new HashSet<Enemy>();
    [NonSerialized] private Bamboo ownerBamboo;
    [NonSerialized] private bool warnedSlotsOutsideChase;

    public bool isBackward;
    public float Backward_t = 0;

    private readonly float[] spawnTimers = new float[3];
    private bool warnedMissingSpawnConfig;

    public void TickSpawn(int stageIndex, float deltaTime)
    {
        CleanupDestroyedUnits();
        CleanupInvalidEnemies();

        if (friendlyUnitPrefab == null || friendlyUnitData == null || transform == null)
        {
            if (!warnedMissingSpawnConfig)
            {
                warnedMissingSpawnConfig = true;
                Debug.LogWarning("BambooCtx: 缺少 friendlyUnitPrefab 或 friendlyUnitData，无法生成友军单位。");
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

        SpawnFriendlyUnit();
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
        if (enemy == null)
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
        return enemy != null && enemiesInTriggerRange.Contains(enemy);
    }

    public Enemy FindNearestEnemyInTriggerRange(Vector3 fromPos)
    {
        CleanupInvalidEnemies();
        Enemy nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (Enemy enemy in enemiesInTriggerRange)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            float sqr = (enemy.transform.position - fromPos).sqrMagnitude;
            if (sqr >= nearestSqr)
                continue;
            nearestSqr = sqr;
            nearest = enemy;
        }

        return nearest;
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

    private void SpawnFriendlyUnit()
    {
        int assignIndex = spawnedUnits.Count;
        Transform assignedReturnPoint = GetAssignedReturnPoint(assignIndex);
        Vector3 spawnPos = assignedReturnPoint != null ? assignedReturnPoint.position : transform.position;
        GameObject go = GameObject.Instantiate(friendlyUnitPrefab, spawnPos, Quaternion.identity);
        FriendlyUnit unit = go.GetComponent<FriendlyUnit>();
        if (unit == null)
            unit = go.AddComponent<FriendlyUnit>();

        unit.Init(
            friendlyUnitData,
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
        enemiesInTriggerRange.RemoveWhere(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
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

    /// <summary>
    /// 若竹子挂在含 Planting point 子物体 FriendlyReturnSlot_1..3 的层级下，则用其作为友军回位/待机锚点。
    /// </summary>
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

        Vector3 center = ownerBamboo.transform.position
            + ownerBamboo.transform.forward * Mathf.Max(0f, guardForwardOffset);
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
                Debug.LogWarning(
                    $"BambooCtx: {PlantingFriendlySlotNames[i]} 距追击圆心超过 chaseRadius={chaseRadius}，友军可能无法接敌；请拉近锚点或增大 chaseRadius。",
                    ownerBamboo);
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
public    BambooState1 state1;
public    BambooState2 state2;
public    BambooState3 state3;

    public BambooRoot(StateMachine m,BambooCtx ctx) : base(m, null)
    {
        state1 = new BambooState1(m, this, ctx);
        state2 = new BambooState2(m, this, ctx);
        state3 = new BambooState3(m, this, ctx);
    }
    protected override State GetInitialState() => state1;

    protected override State GetTransition() => null;
    
}

public class BambooState1 : State
{
    BambooCtx Ctx;
    public float grow;
    public BambooState1(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
    {
        Ctx = ctx;
    }

    protected override void OnEnter()
    {
        grow = Ctx.grow1;
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
        grow -= deltaTime;
        Ctx.TickSpawn(0, deltaTime);
    }

    protected override void OnExit()
    {
        if (Ctx.obj1 != null) Ctx.obj1.SetActive(false);
    }
}

public class BambooState2 : State
{
    BambooCtx Ctx;
    public float grow;

    public BambooState2(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
    {
        Ctx = ctx;
    }

    protected override State GetTransition()
    {
        if (Ctx.isBackward)
        {
            Ctx.isBackward = false;
            return ((BambooRoot)Parent).state1;
        }

        if (grow <= 0)
        {
            return ((BambooRoot)Parent).state3;

        }

        return null;
    }

    protected override void OnEnter()
    {
        if (Ctx.obj2 != null) Ctx.obj2.SetActive(true);
        grow = Ctx.grow2;
        Ctx.ResetSpawnTimer(1);
    }

    protected override void OnUpdate(float deltaTime)
    {
        grow -= deltaTime;
        Ctx.TickSpawn(1, deltaTime);
    }

    protected override void OnExit()
    {
        if (Ctx.obj2 != null) Ctx.obj2.SetActive(false);
    }
}

public class BambooState3 : State
{
    BambooCtx Ctx;

    public BambooState3(StateMachine machine, State parent, BambooCtx ctx) : base(machine, parent)
    {
        Ctx = ctx;
    }
    protected override State GetTransition()
    {
        if (Ctx.isBackward)
        {
            Ctx.isBackward = false;
            return ((BambooRoot)Parent).state2;
        }

        return null;
    }

    protected override void OnEnter()
    {
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