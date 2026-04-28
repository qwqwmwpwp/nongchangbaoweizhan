using System;
using UnityEngine;

namespace qwq
{
    // 这个脚本只处理敌人属性、血量和死亡逻辑，不包含移动、UI控制等
    public class Enemy : MonoBehaviour, IDamageable
    {
        public GameObject obj => gameObject;

        [Header("数据")]
        [SerializeField] private EnemyDataSO enemyData;

        private int hp;
        private int baseHpMax;
        private int baseAttack;
        private int baseMoveSpeed;
        private int finalHpMax;
        private int finalAttack;
        private int finalMoveSpeed;
        private EnemyAttackType attackType;
        private float attackRange;
        private float attackSpeed;
        private int killResource1;
        private int killResource2;
        private int killResource3;
        private EnemyBuffController buffController;
        private EnemyMove cachedMove;

        public EnemyAttackType AttackType => attackType;
        public float AttackRange => attackRange;
        public float AttackSpeed => attackSpeed;
        public int KillResource1 => killResource1;
        public int KillResource2 => killResource2;
        public int KillResource3 => killResource3;
        public bool HasRewindResistance => enemyData != null && enemyData.RewindResistance;

        [Header("UI")]
        public EnemyHealthUI enemyHealthUI;

        private void Start()
        {
            if (enemyData == null)
            {
                Debug.LogError($"Enemy: 未指定 EnemyDataSO（{gameObject.name}）", this);
                return;
            }

            baseHpMax = enemyData.MaxHealth;
            baseAttack = enemyData.Attack;
            baseMoveSpeed = enemyData.MoveSpeed;
            attackType = enemyData.AttackType;
            attackRange = enemyData.AttackRange;
            attackSpeed = enemyData.AttackSpeed;
            killResource1 = enemyData.KillResource1;
            killResource2 = enemyData.KillResource2;
            killResource3 = enemyData.KillResource3;
            hp = Mathf.Max(1, baseHpMax);

            cachedMove = GetComponent<EnemyMove>();
            buffController = GetComponent<EnemyBuffController>();
            if (buffController == null)
                buffController = gameObject.AddComponent<EnemyBuffController>();

            RefreshStatsByBuff();
            hp = finalHpMax;

            if (enemyHealthUI != null)
            {
                enemyHealthUI.PlayerHealthChange(hp, finalHpMax);
            }
        }

        public void TakeDamage(int amount)
        {
            if (enemyData == null) return;

            hp -= amount;
            if (enemyHealthUI != null)
            {
                enemyHealthUI.PlayerHealthChange(hp, finalHpMax);
            }

            if (hp <= 0)
            {
                DieFromCombat();
            }
        }

        public int AttackBase()
        {
            Death();
            return finalAttack;
        }

        /// <summary>当敌人走到终点（最后一个节点）时对基地造成的伤害，返回攻击力</summary>
        public int GetLeakDamage()
        {
            if (enemyData == null)
                return 1;
            return Mathf.Max(1, finalAttack);
        }

        public void ApplyBuff(BuffDataSO buff)
        {
            if (buff == null)
                return;

            if (buffController == null)
                buffController = GetComponent<EnemyBuffController>() ?? gameObject.AddComponent<EnemyBuffController>();

            buffController.ApplyBuff(buff);
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            hp = Mathf.Min(finalHpMax, hp + amount);
            RefreshHpUI();
        }

        public void ApplyBuffSet(BuffSetSO buffSet)
        {
            if (buffSet == null)
                return;

            if (buffController == null)
                buffController = GetComponent<EnemyBuffController>() ?? gameObject.AddComponent<EnemyBuffController>();

            buffController.ApplyBuffSet(buffSet);
        }

        public void RefreshStatsByBuff()
        {
            int oldMaxHp = finalHpMax;

            finalHpMax = CalculateFinalIntStat(baseHpMax, BuffTargetStat.MaxHealth, 1);
            finalAttack = CalculateFinalIntStat(baseAttack, BuffTargetStat.Attack, 1);
            finalMoveSpeed = CalculateFinalIntStat(baseMoveSpeed, BuffTargetStat.MoveSpeed, 0);

            if (oldMaxHp <= 0)
                hp = finalHpMax;
            else if (finalHpMax < hp)
                hp = finalHpMax;

            ApplyMoveSpeedToMover();
            RefreshHpUI();
        }

        private int CalculateFinalIntStat(int baseValue, BuffTargetStat targetStat, int minValue)
        {
            if (buffController == null)
                return Mathf.Max(minValue, baseValue);

            float flat = buffController.GetFlatValue(targetStat);
            float percent = buffController.GetPercentValue(targetStat);
            float finalFloat = baseValue + flat + baseValue * percent / 100f;
            int finalValue = Mathf.RoundToInt(finalFloat);
            return Mathf.Max(minValue, finalValue);
        }

        private void ApplyMoveSpeedToMover()
        {
            if (cachedMove == null)
                cachedMove = GetComponent<EnemyMove>();
            if (cachedMove != null)
                cachedMove.speed = finalMoveSpeed;
        }

        private void RefreshHpUI()
        {
            if (enemyHealthUI != null)
                enemyHealthUI.PlayerHealthChange(hp, finalHpMax);
        }

        public void Death()
        {
            Destroy(gameObject);
        }

        /// <summary>被伤害击杀：发资源事件后销毁；与撞基地 <see cref="AttackBase"/> 的单纯销毁区分。</summary>
        private void DieFromCombat()
        {
            if (enemyData != null)
            {
                // 兼容当前单资源奖励事件：优先使用资源1，未配置时回退旧字段。
                int reward = killResource1 > 0 ? killResource1 : enemyData.KillResourceReward;
                if (reward > 0)
                    GameEvent.TriggerEnemyDefeatedReward(reward);
            }

            Destroy(gameObject);
        }
    }
}