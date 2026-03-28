using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// 键盘AD操作摄像机左右移动
/// </summary>
public class CameraMove : MonoBehaviour
{
    private GameObject Camera;  
    private float inputx;
    private Vector3 newPos;
    private Vector3 currentSpeed=Vector3.zero;
    [SerializeField] private CameraDataSO cameraData;
    private void Awake()
    {
        Camera = this.gameObject;
    }
    private void Start()
    {
        newPos = gameObject.transform.position;
    }
    private void LateUpdate()
    {
        inputx = InputManager.Instance.moveInput*Time.deltaTime;
        newPos.x += inputx * cameraData.MoveSpeed;
        transform.position = Vector3.SmoothDamp(transform.position, newPos, ref currentSpeed, cameraData.Smooth);
    }
}
