using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using qwq;

[System.Serializable]
public class WaveDetail
{
    // 当前波次使用的敌人预制体
    public GameObject enemyPrefab;
    // 当前波次要生成的敌人数量
    public int spawnCount = 1;
    // 当前波次每两只敌人的生成间隔
    public float spawnInterval = 0.5f;
}

[System.Serializable]
public class SpawnLaneConfig
{
    [Header("基础信息")]
    public string laneId = "Lane_01";
    public bool isEnabled = true;

    [Header("生成与路径")]
    public Transform spawnPoint;
    public RoadNode startNode;

    [Header("该出怪口波次")]
    public List<WaveDetail> waves = new List<WaveDetail>();

    [Tooltip("隐藏/辅助出兵口：不计入关卡总敌人数、alive 计数与 OnEnemyDied；波次仍会生成。全路结束后若场上仍有该路敌人，胜利会等待其清空。与 isEnabled 独立。")]
    public bool excludeFromWaveTotals;

    public bool IsEnabled => isEnabled;
}

/// <summary>
/// 零依赖波次管理器：
/// 1) 负责按配置刷怪
/// 2) 维护场上存活数与关卡剩余总数
/// 3) 在全部波次结束后通知胜利
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("多出怪口配置")]
    public List<SpawnLaneConfig> spawnLanes = new List<SpawnLaneConfig>();
    [Header("节奏")]
    // 同一出怪口两波之间等待时间
    public float timeBetweenWaves = 3f;

    // 当前场上存活敌人数量
    private int aliveEnemyCount = 0;
    // 关卡层面的剩余敌人数（含未出生 + 已出生存活）
    private int remainingEnemyTotal = 0;
    private bool[] laneCompleted;
    private bool victoryNotified;

    public int AliveEnemyCount => aliveEnemyCount;
    public int RemainingEnemyTotal => remainingEnemyTotal;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 开局先计算“本关总敌人数”（不含 excludeFromWaveTotals 出兵口），用于 UI 直接显示剩余总量
        remainingEnemyTotal = CalculateTotalEnemyCount();

        int laneCount = spawnLanes != null ? spawnLanes.Count : 0;
        laneCompleted = new bool[laneCount];

        for (int i = 0; i < laneCount; i++)
            StartCoroutine(SpawnLaneRoutine(i));

        StartCoroutine(VictoryWatcherRoutine());
    }

    private IEnumerator SpawnLaneRoutine(int laneIndex)
    {
        SpawnLaneConfig lane = GetLane(laneIndex);
        if (lane == null)
        {
            MarkLaneCompleted(laneIndex);
            yield break;
        }

        if (lane.spawnPoint == null)
        {
            Debug.LogError($"WaveManager: lane[{laneIndex}] spawnPoint 未设置。", this);
            MarkLaneCompleted(laneIndex);
            yield break;
        }

        bool excludeFromTotals = lane.excludeFromWaveTotals;

        for (int waveIndex = 0; waveIndex < lane.waves.Count; waveIndex++)
        {
            if (IsDefeat())
            {
                MarkLaneCompleted(laneIndex);
                yield break;
            }

            WaveDetail wave = lane.waves[waveIndex];
            if (wave == null || wave.enemyPrefab == null || wave.spawnCount <= 0)
                continue;

            for (int i = 0; i < wave.spawnCount; i++)
            {
                while (!lane.IsEnabled)
                {
                    if (IsDefeat())
                    {
                        MarkLaneCompleted(laneIndex);
                        yield break;
                    }
                    yield return null;
                }

                if (IsDefeat())
                {
                    MarkLaneCompleted(laneIndex);
                    yield break;
                }

                GameObject enemy = Instantiate(wave.enemyPrefab, lane.spawnPoint.position, lane.spawnPoint.rotation);
                if (!excludeFromTotals)
                    aliveEnemyCount++;
                InitEnemy(enemy, lane.startNode, excludeFromTotals);

                if (wave.spawnInterval > 0f)
                    yield return new WaitForSeconds(wave.spawnInterval);
                else
                    yield return null;
            }

            if (waveIndex < lane.waves.Count - 1 && timeBetweenWaves > 0f)
                yield return new WaitForSeconds(timeBetweenWaves);
        }

        MarkLaneCompleted(laneIndex);
    }

    public void OnEnemyDied()
    {
        // 这个方法由 DummyEnemy.OnDestroy 触发：敌人死亡或到达终点被销毁都会进入这里
        aliveEnemyCount--;
        if (aliveEnemyCount < 0)
            aliveEnemyCount = 0;

        remainingEnemyTotal--;
        if (remainingEnemyTotal < 0)
            remainingEnemyTotal = 0;
    }

    private int CalculateTotalEnemyCount()
    {
        int total = 0;
        if (spawnLanes == null)
            return total;

        for (int laneIndex = 0; laneIndex < spawnLanes.Count; laneIndex++)
        {
            SpawnLaneConfig lane = spawnLanes[laneIndex];
            if (lane == null || lane.waves == null || lane.excludeFromWaveTotals)
                continue;

            for (int i = 0; i < lane.waves.Count; i++)
            {
                WaveDetail wave = lane.waves[i];
                if (wave == null || wave.enemyPrefab == null || wave.spawnCount <= 0)
                    continue;
                total += wave.spawnCount;
            }
        }

        return total;
    }

    private void InitEnemy(GameObject enemy, RoadNode laneStartNode, bool suppressWaveCount)
    {
        if (enemy == null) return;

        // 兼容当前项目的移动脚本，让新敌人出生后立即获得路径起点
        EnemyMove move = enemy.GetComponent<EnemyMove>();
        if (move != null && laneStartNode != null)
            move.StartMove(laneStartNode);

        DummyEnemy dummy = enemy.GetComponent<DummyEnemy>();
        if (dummy == null)
            dummy = enemy.AddComponent<DummyEnemy>();
        dummy.SuppressWaveCountCallbacks = suppressWaveCount;

        // 敌人时间回溯：每只敌人都具备固定容量位置记录器
        if (enemy.GetComponent<EnemyRewindRecorder>() == null)
            enemy.AddComponent<EnemyRewindRecorder>();
    }

    public void SetSpawnLaneEnabled(int laneIndex, bool enabled)
    {
        SpawnLaneConfig lane = GetLane(laneIndex);
        if (lane == null)
            return;
        lane.isEnabled = enabled;
    }

    public bool IsSpawnLaneEnabled(int laneIndex)
    {
        SpawnLaneConfig lane = GetLane(laneIndex);
        return lane != null && lane.IsEnabled;
    }

    public void SetSpawnLaneEnabled(string laneId, bool enabled)
    {
        int index = FindLaneIndexById(laneId);
        if (index >= 0)
            SetSpawnLaneEnabled(index, enabled);
    }

    public bool IsSpawnLaneEnabled(string laneId)
    {
        int index = FindLaneIndexById(laneId);
        return index >= 0 && IsSpawnLaneEnabled(index);
    }

    private IEnumerator VictoryWatcherRoutine()
    {
        while (!victoryNotified)
        {
            if (IsDefeat())
                yield break;

            // 全部出怪协程结束、计数敌人清零，且场景中无任何仍激活的敌人（含 excludeFromWaveTotals 出兵口）
            if (AreAllLanesCompleted() && aliveEnemyCount <= 0 && !AnyActiveEnemyInScene())
            {
                victoryNotified = true;
                if (GameFlowManager.Instance != null)
                    GameFlowManager.Instance.NotifyVictory();
                yield break;
            }

            yield return null;
        }
    }

    private SpawnLaneConfig GetLane(int laneIndex)
    {
        if (spawnLanes == null || laneIndex < 0 || laneIndex >= spawnLanes.Count)
            return null;
        return spawnLanes[laneIndex];
    }

    private int FindLaneIndexById(string laneId)
    {
        if (string.IsNullOrWhiteSpace(laneId) || spawnLanes == null)
            return -1;

        for (int i = 0; i < spawnLanes.Count; i++)
        {
            SpawnLaneConfig lane = spawnLanes[i];
            if (lane != null && lane.laneId == laneId)
                return i;
        }
        return -1;
    }

    private void MarkLaneCompleted(int laneIndex)
    {
        if (laneCompleted == null || laneIndex < 0 || laneIndex >= laneCompleted.Length)
            return;
        laneCompleted[laneIndex] = true;
    }

    private bool AreAllLanesCompleted()
    {
        if (laneCompleted == null || spawnLanes == null)
            return true;

        for (int i = 0; i < laneCompleted.Length; i++)
        {
            SpawnLaneConfig lane = GetLane(i);
            // 隐藏/辅助出兵口：不参与“全路波次协程已结束”判定，避免 isEnabled 长期为 false 时协程卡在等待而永远不 MarkLaneCompleted，从而卡死胜利。
            if (lane != null && lane.excludeFromWaveTotals)
                continue;
            if (!laneCompleted[i])
                return false;
        }
        return true;
    }

    private bool IsDefeat()
    {
        return GameFlowManager.Instance != null && GameFlowManager.Instance.IsDefeat;
    }

    private static bool AnyActiveEnemyInScene()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].isActiveAndEnabled)
                return true;
        }
        return false;
    }
}

/// <summary>
/// 极简敌人生命周期桥接器：
/// 只做一件事——敌人销毁时通知 WaveManager 递减计数。
/// </summary>
public class DummyEnemy : MonoBehaviour
{
    /// <summary>为 true 时销毁不通知 WaveManager（用于 excludeFromWaveTotals 出兵口）。</summary>
    public bool SuppressWaveCountCallbacks { get; set; }

    private void OnDestroy()
    {
        if (SuppressWaveCountCallbacks)
            return;
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyDied();
    }
}
