using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int currentHealth;
    public int MaxHealth;
    private void Start()
    {
        currentHealth = MaxHealth;
    }
    public void ChangeHealth(int changeHealth)
    {
        currentHealth -= changeHealth;
        if(currentHealth<=0)
        {
            Destroy(gameObject);
        }
        else if(currentHealth>=MaxHealth)
        {
            currentHealth = MaxHealth;
        }
    }
}
