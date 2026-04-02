using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    public Slider EnemyHealth;
    public void PlayerHealthChange(int currentHealth, int maxHealth)
    {
        EnemyHealth.maxValue = maxHealth;
        EnemyHealth.value = currentHealth;
    }
}
