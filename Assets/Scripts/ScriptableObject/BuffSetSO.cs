using UnityEngine;

[CreateAssetMenu(fileName = "NewBuffSet", menuName = "SO文件/Buff组合")]
public class BuffSetSO : ScriptableObject
{
    [field: Header("Buff组合")]
    [field: Tooltip("将数组内 Buff 依次附加到目标敌人。")]
    [field: SerializeField] public BuffDataSO[] Buffs { get; private set; }
}
