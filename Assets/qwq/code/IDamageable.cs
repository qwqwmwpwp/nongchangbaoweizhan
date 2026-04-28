using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable 
{
    public GameObject obj { get; }
    void TakeDamage(int amount);
}
public interface IWeapon
{
    void Fire(IDamageable target);
}