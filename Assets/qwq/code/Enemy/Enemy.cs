using System;
using UnityEngine;

namespace qwq
{
    // 这个脚本只处理敌人属性、血量和死亡逻辑，不包含移动、UI控制等
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
                DieFromCombat();
            }
        }

        public int AttackBase()
        {
            Death();
            return attack;
        }

        /// <summary>当敌人走到终点（最后一个节点）时对基地造成的伤害，返回攻击力</summary>
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

        /// <summary>被伤害击杀：发资源事件后销毁；与撞基地 <see cref="AttackBase"/> 的单纯销毁区分。</summary>
        private void DieFromCombat()
        {
            if (enemyData != null && enemyData.KillResourceReward > 0)
                GameEvent.TriggerEnemyDefeatedReward(enemyData.KillResourceReward);

            Destroy(gameObject);
        }
    }
}