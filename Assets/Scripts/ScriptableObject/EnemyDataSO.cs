using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "SO文件/敌人参数")]
public class EnemyDataSO : ScriptableObject
{
    [field: Header("生命与攻击")]
    [field: SerializeField] public int MaxHealth { get; private set; }
    [field: SerializeField] public int Attack { get; private set; }

    [field: Header("移动")]
    [field: SerializeField] public int MoveSpeed { get; private set; }
}
