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
                DieFromCombat();
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

        /// <summary>被伤害击杀：发资源事件后销毁；与撞基地 <see cref="Attack"/> 的单纯销毁区分。</summary>
        private void DieFromCombat()
        {
            if (enemyData != null && enemyData.KillResourceReward > 0)
                GameEvent.TriggerEnemyDefeatedReward(enemyData.KillResourceReward);

            Destroy(gameObject);
        }
    }
}