using qwq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Detection
{
    public class BaseDetection : MonoBehaviour
    {
        [SerializeField] Enemy enemy;
        private Enemy resolvedEnemy;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            resolvedEnemy = enemy != null ? enemy : (GetComponent<Enemy>() ?? GetComponentInParent<Enemy>());
            if (resolvedEnemy == null)
                return;

            Base baseTarget = collision.GetComponent<Base>() ?? collision.GetComponentInParent<Base>();
            if (baseTarget == null)
                return;

            baseTarget.TakeDamage(resolvedEnemy.AttackBase());
        }
    }
}