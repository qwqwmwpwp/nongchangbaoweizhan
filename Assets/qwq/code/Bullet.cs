using qwq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IWeapon
{
    IDamageable enemy;
    Vector2 direction;
 [SerializeField] private float lifeTime = 5f; // 自动销毁时间
    private void Start()
    {
        
        Destroy(gameObject, lifeTime);
    }
    private void Update()
    {
        Attack();
    }
    public void Attack()
    {
        transform.position += (Vector3)direction.normalized * 8 * Time.deltaTime;
        if (!IsTargetValid())
            return;
        direction = enemy.Object.transform.position - transform.position;
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
    private bool IsTargetValid()
    {
        // 1. 检查接口引用是否为空
        if (enemy == null)
            return false;
        // 3. 使用 try-catch 安全地检查 GameObject
        try
        {
            // 尝试访问 transform 来验证 GameObject 是否仍然存在
            var temp = enemy.Object.transform.position;
            return true;
        }
        catch
        {
            // 如果抛出异常，说明对象已被销毁
            return false;
        }
    }
}
