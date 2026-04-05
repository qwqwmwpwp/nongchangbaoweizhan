using System;
using UnityEngine;

namespace qwq
{
    // 这个脚本只负责：属性、受击、死亡、UI，移动相关逻辑解耦出去了
    public class Enemy : MonoBehaviour, IDamageable
    {
        public GameObject Object => gameObject;

        [Header("属性")]
        public int attack = 1;
        [SerializeField] private int hp_max = 100;
        private int hp = 10;

        [Header("UI")]
        public EnemyHealthUI enemyHealthUI;

        private void Start()
        {
            hp = hp_max;
            if (enemyHealthUI != null)
            {
                enemyHealthUI.PlayerHealthChange(hp, hp_max);
            }
        }

        public void TakeDamage(int amount)
        {
            hp -= amount;
            if (enemyHealthUI != null)
            {
                enemyHealthUI.PlayerHealthChange(hp, hp_max);
            }

            if (hp <= 0)
            {
                Death();
            }
        }

        public int Attack()
        {
            Death(); 
            return attack;
        }

        public void Death()
        {
            Destroy(gameObject);
        }
    }
}