using qwq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IWeapon
{
    IDamageable enemy;
    private void Update()
    {
        Attack();
    }
    public void Attack()
    {
        if (enemy == null)
            return;
        Vector2 direction = enemy.Object.transform.position - transform.position;
        transform.position += (Vector3)direction.normalized * 3*Time.deltaTime;
        if (direction.magnitude < 1)
        {

            enemy.TakeDamage(10);
            Destroy(this.gameObject);
        }
    }
    public void Fire(IDamageable target)
    {
        enemy = target;
    }
}
