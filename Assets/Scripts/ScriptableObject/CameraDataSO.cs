using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewCameraData", menuName = "SO文件/相机参数")] 

public class CameraDataSO : ScriptableObject
{
    [field: Header("偏移设置")]
    [field: SerializeField] public float MoveSpeed { get; private set; }
    [field :SerializeField] public float MaxMoveArea { get;private set ;}
    [ field: SerializeField] public float Smooth { get; private set; }
}
