using System;
using UnityEngine;

namespace qwq
{
    // ����ű�ֻ�������ԡ��ܻ���������UI���ƶ�����߼������ȥ��
    public class Enemy : MonoBehaviour, IDamageable
    {
        public GameObject Object => gameObject;

        [Header("����")]
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
                Debug.LogError($"Enemy: δָ�� EnemyDataSO��{gameObject.name}��", this);
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

        /// <summary>������·���ߵ��յ㣨����һ�ڵ㣩ʱ���Ի�����ɵ��˺�����������������</summary>
        public int GetLeakDamage()
        {
            if (enemyData == null)
                return 1;
            return Mathf.Max(1, enemyData.Attack);
        }

        public void Death()
        {
            Destroy(gameObject);
        }
    }
}