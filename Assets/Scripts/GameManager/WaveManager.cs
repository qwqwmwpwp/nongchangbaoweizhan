using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private bool hasAnyValidWave;
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
        // 开局先计算“本关总敌人数”，用于 UI 直接显示剩余总量
        remainingEnemyTotal = CalculateTotalEnemyCount();
        hasAnyValidWave = remainingEnemyTotal > 0;

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
                aliveEnemyCount++;
                InitEnemy(enemy, lane.startNode);

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
            if (lane == null || lane.waves == null)
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

    private void InitEnemy(GameObject enemy, RoadNode laneStartNode)
    {
        if (enemy == null) return;

        // 兼容当前项目的移动脚本，让新敌人出生后立即获得路径起点
        EnemyMove move = enemy.GetComponent<EnemyMove>();
        if (move != null && laneStartNode != null)
            move.StartMove(laneStartNode);

        // 保证每个敌人都能在销毁时回调 WaveManager，避免波次卡死
        if (enemy.GetComponent<DummyEnemy>() == null)
            enemy.AddComponent<DummyEnemy>();

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

            if (hasAnyValidWave && AreAllLanesCompleted() && aliveEnemyCount <= 0)
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
        if (laneCompleted == null)
            return true;

        for (int i = 0; i < laneCompleted.Length; i++)
        {
            if (!laneCompleted[i])
                return false;
        }
        return true;
    }

    private bool IsDefeat()
    {
        return GameFlowManager.Instance != null && GameFlowManager.Instance.IsDefeat;
    }
}

/// <summary>
/// 极简敌人生命周期桥接器：
/// 只做一件事——敌人销毁时通知 WaveManager 递减计数。
/// </summary>
public class DummyEnemy : MonoBehaviour
{
    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyDied();
    }
}
