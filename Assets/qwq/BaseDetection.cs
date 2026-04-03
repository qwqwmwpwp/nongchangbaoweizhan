using qwq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Detection
{
    public class BaseDetection : MonoBehaviour
    {
        [SerializeField] Enemy enemy;
        private void OnTriggerEnter2D(Collider2D collision)
        {
            IDamageable playerBase = collision.GetComponent<IDamageable>();
            if (playerBase == null) return;
            Debug.Log("qwq");
            playerBase.TakeDamage(enemy.Attack());
        }
    }
}