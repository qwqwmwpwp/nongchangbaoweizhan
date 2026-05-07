using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子弹接口实现
/// </summary>
public class Bullet : MonoBehaviour, IWeapon
{
    IDamageable enemy;
    Vector2 direction;

    public float moveSpeed;
    public int damage;

    private void Awake()
    {
    }

    private void Start()
    {

        Destroy(this.gameObject, 3f);
    }

    private void Update()
    {
        Attack();
    }

    public void Attack()
    {
        transform.position += (Vector3)direction.normalized * moveSpeed * Time.deltaTime;
        if (!IsTargetValid())
            return;
        direction = enemy.obj.transform.position - transform.position;

        if (direction.magnitude < 0.2)
        {
            enemy.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }

    public void Initialize(IDamageable targer, int attack)
    {
        damage = attack;
        enemy = targer;
    }


    public void Fire(IDamageable target)
    {
        enemy = target;
    }
    private bool IsTargetValid()
    {
        if (enemy == null || enemy.Equals(null) || enemy.obj == null)
            return false;

        return true;
    }
}
