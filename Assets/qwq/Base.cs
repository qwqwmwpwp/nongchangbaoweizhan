using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Base : MonoBehaviour, IDamageable
{
    public GameObject Object => gameObject;
    public int hp = 10;
    public bool isGameOver;

    public void TakeDamage(int amount)
    {
        if (isGameOver)
            return;
        hp -= amount;
        if (hp <= 0)
        {
            isGameOver = true;
        }
    }
}
