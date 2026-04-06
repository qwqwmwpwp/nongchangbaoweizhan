using System;
using UnityEngine;

namespace qwq
{
    // 这个脚本只负责：属性、受击、死亡、UI，移动相关逻辑解耦出去了
    public class Enemy : MonoBehaviour, IDamageable
    {
        public GameObject Object => gameObject;

        [Header("数据")]
        [SerializeField] private EnemyDataSO enemyData;

        private int hp;
        private int hpMax;
        private int attack;

        [Header("UI")]
        public EnemyHealthUI enemyHealthUI;

        private void Start()
        {
            if (enemyData == null)
            {
                Debug.LogError($"Enemy: 未指定 EnemyDataSO（{gameObject.name}）", this);
                return;
            }

            hpMax = enemyData.MaxHealth;
            attack = enemyData.Attack;
            hp = hpMax;

            var mover = GetComponent<EnemyMove>();
            if (mover != null)
                mover.speed = enemyData.MoveSpeed;

            if (enemyHealthUI != null)
            {
                enemyHealthUI.PlayerHealthChange(hp, hpMax);
            }
        }

        public void TakeDamage(int amount)
        {
            if (enemyData == null) return;

            hp -= amount;
            if (enemyHealthUI != null)
            {
                enemyHealthUI.PlayerHealthChange(hp, hpMax);
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