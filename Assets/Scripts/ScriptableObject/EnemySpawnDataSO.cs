using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnWeightedOption
{
    public GameObject enemyPrefab;
    [Min(0f)] public float weight = 1f;

    public bool IsValid => enemyPrefab != null && weight > 0f;
}

[CreateAssetMenu(fileName = "NewEnemySpawnData", menuName = "SO/Wave Enemy Spawn")]
public class EnemySpawnDataSO : ScriptableObject
{
    [Header("Enemy Pool")]
    [SerializeField] private List<EnemySpawnWeightedOption> enemyOptions = new List<EnemySpawnWeightedOption>();

    [Header("Spawn Rhythm")]
    [Min(0)] [SerializeField] private int spawnCount = 1;
    [Min(0f)] [SerializeField] private float spawnInterval = 0.5f;
    [Min(0f)] [SerializeField] private float delayAfterWave = 0f;

    public IReadOnlyList<EnemySpawnWeightedOption> EnemyOptions => enemyOptions;
    public int SpawnCount => Mathf.Max(0, spawnCount);
    public float SpawnInterval => Mathf.Max(0f, spawnInterval);
    public float DelayAfterWave => Mathf.Max(0f, delayAfterWave);
    public bool IsValidWave => SpawnCount > 0 && HasValidEnemyOptions;

    public bool HasValidEnemyOptions
    {
        get
        {
            if (enemyOptions == null)
                return false;

            for (int i = 0; i < enemyOptions.Count; i++)
            {
                EnemySpawnWeightedOption option = enemyOptions[i];
                if (option != null && option.IsValid)
                    return true;
            }

            return false;
        }
    }

    public GameObject PickEnemyPrefab()
    {
        if (enemyOptions == null)
            return null;

        float totalWeight = 0f;
        for (int i = 0; i < enemyOptions.Count; i++)
        {
            EnemySpawnWeightedOption option = enemyOptions[i];
            if (option != null && option.IsValid)
                totalWeight += option.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);
        GameObject fallbackPrefab = null;

        for (int i = 0; i < enemyOptions.Count; i++)
        {
            EnemySpawnWeightedOption option = enemyOptions[i];
            if (option == null || !option.IsValid)
                continue;

            fallbackPrefab = option.enemyPrefab;
            roll -= option.weight;
            if (roll <= 0f)
                return option.enemyPrefab;
        }

        return fallbackPrefab;
    }
}
