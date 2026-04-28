using UnityEngine;

[CreateAssetMenu(fileName = "NewFriendlyUnitData", menuName = "SO文件/友军参数")]
public class FriendlyUnitDataSO : ScriptableObject
{
    [field: Header("基础属性")]
    [field: Tooltip("友军最大生命值。")]
    [field: SerializeField] public int MaxHealth { get; private set; } = 10;
    [field: Tooltip("友军基础攻击力。")]
    [field: SerializeField] public int Attack { get; private set; } = 1;

    [field: Header("移动")]
    [field: Tooltip("友军基础移动速度。")]
    [field: SerializeField] public int MoveSpeed { get; private set; } = 3;

    [field: Header("攻击属性")]
    [field: Tooltip("近战/远程，仅用于行为扩展预留。")]
    [field: SerializeField] public EnemyAttackType AttackType { get; private set; } = EnemyAttackType.Melee;
    [field: Tooltip("攻击判定半径。")]
    [field: SerializeField] public float AttackRange { get; private set; } = 1.5f;
    [field: Tooltip("每秒攻击次数（1 表示 1 秒 1 次）。")]
    [field: SerializeField] public float AttackSpeed { get; private set; } = 1f;

    [field: Header("击杀奖励（保留扩展）")]
    [field: Tooltip("预留字段：击杀后资源1奖励。")]
    [field: SerializeField] public int KillResource1 { get; private set; }
    [field: Tooltip("预留字段：击杀后资源2奖励。")]
    [field: SerializeField] public int KillResource2 { get; private set; }
    [field: Tooltip("预留字段：击杀后资源3奖励。")]
    [field: SerializeField] public int KillResource3 { get; private set; }
    [field: Tooltip("预留字段：兼容旧逻辑总奖励。")]
    [field: SerializeField] public int KillResourceReward { get; private set; }

    [field: Header("特殊属性")]
    [field: Tooltip("预留字段：是否抗回溯。")]
    [field: SerializeField] public bool RewindResistance { get; private set; }
}
