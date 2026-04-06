using UnityEngine;

[CreateAssetMenu(fileName = "NewBatteryData", menuName = "SO文件/炮台参数")]
public class BatteryDataSO : ScriptableObject
{
    [field: Header("开火")]
    [field: SerializeField] public float FireInterval { get; private set; }
}
