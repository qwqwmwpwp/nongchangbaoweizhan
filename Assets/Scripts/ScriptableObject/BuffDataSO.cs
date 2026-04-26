using UnityEngine;

public enum BuffTargetStat
{
    MaxHealth = 0,
    MoveSpeed = 1,
    Attack = 2
}

public enum BuffModifyMode
{
    Flat = 0,
    Percent = 1
}

[CreateAssetMenu(fileName = "NewBuffData", menuName = "SO文件/Buff参数")]
public class BuffDataSO : ScriptableObject
{
    [field: Header("基础信息")]
    [field: SerializeField] public string BuffId { get; private set; } = "Buff_001";
    [field: SerializeField] public float Duration { get; private set; } = 5f;
    [field: Tooltip("为 true 时允许同类 Buff 叠加；为 false 时重复附加仅刷新持续时间。")]
    [field: SerializeField] public bool CanStack { get; private set; }

    [field: Header("效果信息")]
    [field: SerializeField] public BuffTargetStat TargetStat { get; private set; } = BuffTargetStat.Attack;
    [field: SerializeField] public BuffModifyMode ModifyMode { get; private set; } = BuffModifyMode.Flat;
    [field: Tooltip("Flat 为固定值；Percent 为百分比，10 表示 +10%。")]
    [field: SerializeField] public float Value { get; private set; } = 10f;
    [field: Tooltip("附加该 Buff 时立即回复生命值。<=0 表示不回血。")]
    [field: SerializeField] public int HealOnApply { get; private set; }
}
