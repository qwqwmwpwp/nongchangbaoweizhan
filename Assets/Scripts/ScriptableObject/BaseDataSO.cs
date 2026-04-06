using UnityEngine;

[CreateAssetMenu(fileName = "NewBaseData", menuName = "SO文件/基地参数")]
public class BaseDataSO : ScriptableObject
{
    [field: Header("生命")]
    [field: SerializeField] public int MaxHealth { get; private set; }
}
