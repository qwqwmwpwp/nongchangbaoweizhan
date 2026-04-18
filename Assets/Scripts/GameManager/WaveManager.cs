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

/// <summary>
/// 零依赖波次管理器：
/// 1) 负责按配置刷怪
/// 2) 维护场上存活数与关卡剩余总数
/// 3) 在全部波次结束后通知胜利
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("波次配置")]
    public List<WaveDetail> waves = new List<WaveDetail>();
    [Header("生成与路径")]
    // 敌人出生点（位置+旋转）
    public Transform spawnPoint;
    // 敌人路径起点（用于 EnemyMove.StartMove）
    public RoadNode startNode;
    [Header("节奏")]
    // 两波之间等待时间
    public float timeBetweenWaves = 3f;

    // 当前场上存活敌人数量
    private int aliveEnemyCount = 0;
    // 关卡层面的剩余敌人数（含未出生 + 已出生存活）
    private int remainingEnemyTotal = 0;

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
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("WaveManager: spawnPoint 未设置。", this);
            yield break;
        }

        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            // 基地已失败则不再刷怪
            if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsDefeat)
                yield break;

            WaveDetail wave = waves[waveIndex];
            if (wave == null || wave.enemyPrefab == null || wave.spawnCount <= 0)
                continue;

            for (int i = 0; i < wave.spawnCount; i++)
            {
                if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsDefeat)
                    yield break;

                // 原生 Instantiate（不使用对象池）
                GameObject enemy = Instantiate(wave.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                aliveEnemyCount++;
                InitEnemy(enemy);

                if (wave.spawnInterval > 0f)
                    yield return new WaitForSeconds(wave.spawnInterval);
                else
                    yield return null;
            }

            yield return new WaitUntil(() => aliveEnemyCount <= 0);

            if (waveIndex < waves.Count - 1 && timeBetweenWaves > 0f)
                yield return new WaitForSeconds(timeBetweenWaves);
        }

        // 至少配置过一波且场上已清空、未失败 → 胜利（空列表视为未开始波次，不自动胜利）
        if (waves.Count > 0 && GameFlowManager.Instance != null && !GameFlowManager.Instance.IsDefeat)
            GameFlowManager.Instance.NotifyVictory();
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
        // 只统计“有效波次”：有预制体且数量 > 0
        int total = 0;
        for (int i = 0; i < waves.Count; i++)
        {
            WaveDetail wave = waves[i];
            if (wave == null || wave.enemyPrefab == null || wave.spawnCount <= 0)
                continue;
            total += wave.spawnCount;
        }
        return total;
    }

    private void InitEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        // 兼容当前项目的移动脚本，让新敌人出生后立即获得路径起点
        EnemyMove move = enemy.GetComponent<EnemyMove>();
        if (move != null && startNode != null)
            move.StartMove(startNode);

        // 保证每个敌人都能在销毁时回调 WaveManager，避免波次卡死
        if (enemy.GetComponent<DummyEnemy>() == null)
            enemy.AddComponent<DummyEnemy>();

        // 敌人时间回溯：每只敌人都具备固定容量位置记录器
        if (enemy.GetComponent<EnemyRewindRecorder>() == null)
            enemy.AddComponent<EnemyRewindRecorder>();
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
