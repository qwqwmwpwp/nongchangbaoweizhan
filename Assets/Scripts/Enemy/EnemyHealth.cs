using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int currentHealth=10;
    public int maxHealth=10;
    public EnemyHealthUI enemyHealthUI; 
    private void Start()
    {
        currentHealth = maxHealth;
        enemyHealthUI.PlayerHealthChange(currentHealth, maxHealth);
    }
    public void ChangeHealth(int changeHealth)
    {
        currentHealth -= changeHealth;
        enemyHealthUI.PlayerHealthChange(currentHealth, maxHealth);
        if(currentHealth<=0)
        {
            Destroy(gameObject);
        }
        else if(currentHealth>=maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
}
