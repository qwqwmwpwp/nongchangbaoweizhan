using qwq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantsAttackDetection : MonoBehaviour
{
    [SerializeField] Plants battery;
    private void OnTriggerEnter2D(Collider2D collision)
    {

        IDamageable enemy = collision.GetComponent<IDamageable>();
        if (enemy == null) return;
        battery.plantsCtx.enemys.Add(enemy);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        IDamageable enemy = collision.GetComponent<IDamageable>();
        if (enemy == null) return;
        battery.plantsCtx.enemys.Remove(enemy);
    }
}
