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
        if (!DamageableTargetUtility.TryGetGameObject(enemy, out GameObject enemyObj))
            return;
        direction = enemyObj.transform.position - transform.position;

        if (direction.magnitude < 0.2)
        {
            AttackMusic();
            enemy.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }

    [Header("音效")]
    [SerializeField] AudioClip attackClip;
    public void AttackMusic()
    {
        if (AudioManager.Instance != null && attackClip != null)
        {
            AudioManager.Instance.PlayUISound(attackClip);
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
}

