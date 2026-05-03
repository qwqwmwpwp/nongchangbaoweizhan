using System.Collections;
using qwq;
using System.Collections.Generic;
using UnityEngine;

public class PlantsAttackDetection : MonoBehaviour
{
    [SerializeField] Plants battery;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 仅敌人：友军也实现 IDamageable，不能进塔索敌列表
        Enemy enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null)
            return;
        battery.plantsCtx.enemys.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponentInParent<Enemy>();
        if (enemy == null)
            return;
        battery.plantsCtx.enemys.Remove(enemy);
    }
}
