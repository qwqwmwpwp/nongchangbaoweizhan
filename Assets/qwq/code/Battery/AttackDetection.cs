using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackDetection : MonoBehaviour
{
    [SerializeField] Battery battery;
    private void OnTriggerEnter2D(Collider2D collision)
    {

        IDamageable enemy = collision.GetComponent<IDamageable>();
        if (enemy == null) return;
        battery.enemys.Add(enemy);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        IDamageable enemy = collision.GetComponent<IDamageable>();
        if (enemy == null) return;
        battery.enemys.Remove(enemy);
    }
}
