using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatermelonBullet : MonoBehaviour, IWeapon
{
    List<IDamageable> enemys;
    CircleCollider2D circleCollider2D;
    float attack;
    private void Awake()
    {
        circleCollider2D = GetComponent<CircleCollider2D>();
    }
    public void Fire(IDamageable target)
    {
        enemys.Add(target);
    }

    public void Initialize(int attack,float Range )
    {
        this.attack = attack;
        circleCollider2D.radius = Range;
    }
}
