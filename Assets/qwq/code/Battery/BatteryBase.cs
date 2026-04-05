using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BatteryBase : MonoBehaviour
{
    public GameObject parentObject;
    public GameObject battery;
    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        Debug.Log($"点击了: {gameObject.name}");
        // 处理点击逻辑

        PlantGenerateC.instance.enterUI(this);
    }

    private void OnMouseEnter()
    {
        // 鼠标进入时高亮显示
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        GetComponent<Renderer>().material.color = Color.yellow; GetComponent<Renderer>().material.color = Color.yellow;
    }

    private void OnMouseExit()
    {
        // 鼠标离开时恢复颜色
        GetComponent<Renderer>().material.color = Color.white;
    }

    private void OnMouseUp()
    {
        // 鼠标抬起
        Debug.Log("鼠标抬起");
    }
    public bool IsGenerated()
    {
        if (!parentObject && battery) return false;
        else return true;
    }
}
