using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemySpawnData", menuName = "SO文件/敌人生成参数")]
public class EnemySpawnDataSO : ScriptableObject
{
    [field: Header("生成节奏")]
    [field: SerializeField] public float SpawnInterval { get; private set; }
}
