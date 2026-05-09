using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using qwq;

[System.Serializable]
public class SpawnLaneConfig
{
    [Header("Basic")]
    public string laneId = "Lane_01";
    public bool isEnabled = true;

    [Header("Spawn And Path")]
    public Transform spawnPoint;
    public RoadNode startNode;

    [Header("Lane Waves")]
    public List<EnemySpawnDataSO> waves = new List<EnemySpawnDataSO>();

    [Tooltip("Auxiliary lanes still spawn enemies, but do not count toward remaining enemies or alive wave enemies. Victory still waits for active enemies in the scene to clear.")]
    public bool excludeFromWaveTotals;

    public bool IsEnabled => isEnabled;
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Spawn Lanes")]
    public List<SpawnLaneConfig> spawnLanes = new List<SpawnLaneConfig>();

    [Header("Timing")]
    [Min(0f)] public float firstWaveDelay = 0f;

    private int aliveEnemyCount = 0;
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
            Debug.LogError($"WaveManager: lane[{laneIndex}] spawnPoint is not assigned.", this);
            MarkLaneCompleted(laneIndex);
            yield break;
        }

        if (!HasAnyValidWave(lane))
        {
            MarkLaneCompleted(laneIndex);
            yield break;
        }

        if (firstWaveDelay > 0f)
        {
            yield return new WaitForSeconds(firstWaveDelay);
            if (IsDefeat())
            {
                MarkLaneCompleted(laneIndex);
                yield break;
            }
        }

        bool excludeFromTotals = lane.excludeFromWaveTotals;

        for (int waveIndex = 0; waveIndex < lane.waves.Count; waveIndex++)
        {
            if (IsDefeat())
            {
                MarkLaneCompleted(laneIndex);
                yield break;
            }

            EnemySpawnDataSO wave = lane.waves[waveIndex];
            if (wave == null || !wave.IsValidWave)
                continue;

            for (int i = 0; i < wave.SpawnCount; i++)
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

                GameObject enemyPrefab = wave.PickEnemyPrefab();
                if (enemyPrefab == null)
                    continue;

                GameObject enemy = Instantiate(enemyPrefab, lane.spawnPoint.position, lane.spawnPoint.rotation);
                if (!excludeFromTotals)
                    aliveEnemyCount++;

                InitEnemy(enemy, lane.startNode, excludeFromTotals);

                if (i < wave.SpawnCount - 1 && wave.SpawnInterval > 0f)
                    yield return new WaitForSeconds(wave.SpawnInterval);
            }

            if (HasValidWaveAfter(lane, waveIndex) && wave.DelayAfterWave > 0f)
                yield return new WaitForSeconds(wave.DelayAfterWave);
        }

        MarkLaneCompleted(laneIndex);
    }

    public void OnEnemyDied()
    {
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
                EnemySpawnDataSO wave = lane.waves[i];
                if (wave == null || !wave.IsValidWave)
                    continue;

                total += wave.SpawnCount;
            }
        }

        return total;
    }

    private void InitEnemy(GameObject enemy, RoadNode laneStartNode, bool suppressWaveCount)
    {
        if (enemy == null)
            return;

        EnemyMove move = enemy.GetComponent<EnemyMove>();
        if (move != null && laneStartNode != null)
            move.StartMove(laneStartNode);

        DummyEnemy dummy = enemy.GetComponent<DummyEnemy>();
        if (dummy == null)
            dummy = enemy.AddComponent<DummyEnemy>();
        dummy.SuppressWaveCountCallbacks = suppressWaveCount;

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
            if (lane != null && lane.excludeFromWaveTotals)
                continue;

            if (!laneCompleted[i])
                return false;
        }

        return true;
    }

    private static bool HasAnyValidWave(SpawnLaneConfig lane)
    {
        if (lane == null || lane.waves == null)
            return false;

        for (int i = 0; i < lane.waves.Count; i++)
        {
            EnemySpawnDataSO wave = lane.waves[i];
            if (wave != null && wave.IsValidWave)
                return true;
        }

        return false;
    }

    private static bool HasValidWaveAfter(SpawnLaneConfig lane, int waveIndex)
    {
        if (lane == null || lane.waves == null)
            return false;

        for (int i = waveIndex + 1; i < lane.waves.Count; i++)
        {
            EnemySpawnDataSO wave = lane.waves[i];
            if (wave != null && wave.IsValidWave)
                return true;
        }

        return false;
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

public class DummyEnemy : MonoBehaviour
{
    public bool SuppressWaveCountCallbacks { get; set; }

    private void OnDestroy()
    {
        if (SuppressWaveCountCallbacks)
            return;

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyDied();
    }
}
