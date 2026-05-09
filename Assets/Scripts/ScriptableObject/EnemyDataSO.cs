using UnityEngine;

public enum EnemyAttackType
{
    Melee = 0,
    Ranged = 1
}

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "SO文件/敌人参数")]
public class EnemyDataSO : ScriptableObject
{
    [field: Header("基础属性")]
    [field: SerializeField] public int MaxHealth { get; private set; }
    [field: Tooltip("抵达基地时对基地造成的伤害。")]
    [field: SerializeField] public int Attack { get; private set; }

    [field: Header("移动")]
    [field: SerializeField] public int MoveSpeed { get; private set; }

    [field: Header("攻击属性")]
    [field: Tooltip("攻击我方兵种时造成的伤害。")]
    [field: SerializeField] public int FriendlyAttack { get; private set; } = 1;
    [field: SerializeField] public EnemyAttackType AttackType { get; private set; } = EnemyAttackType.Melee;
    [field: Tooltip("攻击距离半径。近战可配置较小，远程可配置较大。")]
    [field: SerializeField] public float AttackRange { get; private set; } = 1.5f;
    [field: Tooltip("攻击速度（每秒攻击次数）。")]
    [field: SerializeField] public float AttackSpeed { get; private set; } = 1f;
    [field: Tooltip("Attack animation cooldown in seconds. When <= 0, it falls back to AttackSpeed.")]
    [field: SerializeField] public float AttackAnimationCooldown { get; private set; } = 1f;

    [field: Header("击杀奖励")]
    [field: InspectorName("肥料（资源1）")]
    [field: Tooltip("击杀后掉落的肥料数量。")]
    [field: SerializeField] public int KillResource1 { get; private set; } = 1;
    [field: InspectorName("能量（资源2）")]
    [field: Tooltip("击杀后获得的能量数量。")]
    [field: SerializeField] public int KillResource2 { get; private set; } = 0;
    [field: Tooltip("兼容旧逻辑的总奖励值（当前默认映射为肥料）。")]
    [field: SerializeField] public int KillResourceReward { get; private set; } = 1;

    [field: Header("特殊属性")]
    [field: Tooltip("为 true 时不受敌人回溯技能影响。")]
    [field: SerializeField] public bool RewindResistance { get; private set; }
}
