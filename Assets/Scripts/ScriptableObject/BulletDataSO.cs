using UnityEngine;

[CreateAssetMenu(fileName = "NewBulletData", menuName = "SO文件/子弹参数")]
public class BulletDataSO : ScriptableObject
{
    [field: Header("移动与判定")]
    [field: SerializeField] public float MoveSpeed { get; private set; }
    [field: SerializeField] public float HitDistance { get; private set; }

    [field: Header("伤害与生命周期")]
    [field: SerializeField] public int Damage { get; private set; }
    [field: SerializeField] public float LifeTime { get; private set; }
}
