using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "SO文件/敌人参数")]
public class EnemyDataSO : ScriptableObject
{
    [field: Header("生命与攻击")]
    [field: SerializeField] public int MaxHealth { get; private set; }
    [field: SerializeField] public int Attack { get; private set; }

    [field: Header("移动")]
    [field: SerializeField] public int MoveSpeed { get; private set; }

    [field: Header("击杀奖励")]
    [field: Tooltip("被玩家击杀时发放的能量/资源点数；漏怪撞基地不发放。")]
    [field: SerializeField] public int KillResourceReward { get; private set; } = 1;
}
