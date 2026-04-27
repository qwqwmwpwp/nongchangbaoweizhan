using qwq;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlantGenerateC : MonoBehaviour
{
    public static PlantGenerateC instance;  // 单例实例
    BatteryBase Bass;                       // 当前操作的电池基类引用
    public List<GameObject> plants;               // 要生成的植物预制体
    [SerializeField] PlantGenerateM plantGenerateM;

    [SerializeField] private float rayLength = 5f;       // 射线检测长度
    [SerializeField] private LayerMask targetLayer;      // 要检测的目标层级
    [SerializeField] private Color rayColor = Color.red; // Scene视图中的射线颜色

    private void Awake()
    {
        if (instance) Destroy(this);
        else instance = this;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            BaseDetection();
    }

    private void BaseDetection()
    {
        // 将鼠标屏幕坐标转换为世界坐标
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // 计算从物体指向鼠标位置的标准化方向向量
        Vector3 direction = Vector2.zero;

        // 执行2D射线检测
        // 参数：起点，方向，长度，检测层级
        RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, direction, rayLength, targetLayer);

        // 检测到碰撞
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.GetComponent<BatteryBase>() is BatteryBase bass)
            {
                enterUI(bass);
                return;
            }
        }
    }

    public void enterUI(BatteryBase bass)
    {
        if (!bass.IsGenerated()) return;
        Bass = bass;
        plantGenerateM.enterUI(bass.transform.position, plants);

    }

    public void PlantGenerated(int n)
    {
        if (!Bass || !Bass.IsGenerated()) return;


        PlantsCtx ctx = plants[n].GetComponent<Plants>().plantsCtx;
        if (plants[n].GetComponent<Plants>().plantsCtx is PlantsCtx newCtx)
            ctx = newCtx;
        else return;

        bool isfertilizer = AttributeManager.Instance.SpendMoney(ctx.fertilizer, ctx.diamond);

        if (isfertilizer)
            Bass.battery = Instantiate(plants[n], Bass.parentObject.transform);

    }
}
