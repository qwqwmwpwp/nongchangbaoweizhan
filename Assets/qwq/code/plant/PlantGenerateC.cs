using qwq;
using System;
using UnityEngine;

public class PlantGenerateC : MonoBehaviour
{
    public static PlantGenerateC instance;  // 单例实例
    BatteryBase Bass;                       // 当前操作的电池基类引用
    public GameObject plant1;               // 要生成的植物预制体
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
            qwq();
    }

    private void qwq()
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
            if(hit.collider.GetComponent<BatteryBase>() is BatteryBase bass)
            {
                enterUI(bass);
                Debug.Log(bass.name);
                return;
            }
        }
    }

    // 在Scene视图绘制射线（仅编辑模式下可见）
    private void OnDrawGizmos()
    {
        Gizmos.color = rayColor;  // 设置射线颜色
        // 绘制射线：从物体位置向右方向绘制
        Gizmos.DrawRay(transform.position, transform.right * rayLength);
    }

    public void enterUI(BatteryBase bass)
    {
        Bass = bass;
        plantGenerateM.enterUI(bass.transform.position);

    }
    public void PlantGenerated()
    {
        BatteryCtx ctx;
        if (plant1.GetComponent<Battery>().ctx is BatteryCtx newCtx) ctx = newCtx;
        else return;

        bool isfertilizer = AttributeManager.Instance.SpendMoney(ctx.fertilizer,ctx.diamond);

        if (isfertilizer && Bass &&Bass.IsGenerated()) 
        Instantiate(plant1, Bass.parentObject.transform);

    }
}
