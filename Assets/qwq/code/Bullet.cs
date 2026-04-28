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

    [SerializeField] private BulletDataSO bulletData;
    private float moveSpeed;
    private int damage;
    private float hitDistance;

    private void Awake()
    {
        if (bulletData == null)
        {
            Debug.LogError($"Bullet: 未指定 BulletDataSO（{gameObject.name}）", this);
            enabled = false;
            return;
        }

        moveSpeed = bulletData.MoveSpeed;
        damage = bulletData.Damage;
        hitDistance = bulletData.HitDistance;
    }

    private void Start()
    {
        if (bulletData == null) return;
        Destroy(gameObject, bulletData.LifeTime);
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
        if (direction.magnitude < hitDistance)
        {
            enemy.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }
    public void Fire(IDamageable target)
    {
        enemy = target;
    }
    private bool IsTargetValid()
    {
        //if (enemy == null)
        //    return false;

        //// Unity 的特殊判空检查
        //if (enemy.Object == null)
        //    return false;

        //// 或者使用 System.Object.ReferenceEquals
        //if (System.Object.ReferenceEquals(enemy.Object, null))
        //    return false;

        //return enemy.Object.activeInHierarchy;  // 确保对象是激活的

        // 1. ???????????????
        if (enemy == null)
            return false;
        // 3. ??? try-catch ??????? GameObject
        try
        {
            // ??????? transform ????? GameObject ??????????
            var temp = enemy.obj.transform.position;
            return true;
        }
        catch
        {
            // ????????????????????????
            return false;
        }
    }
}
