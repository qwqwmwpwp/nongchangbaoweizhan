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
        private EnemyStateController stateController;
        private EnemyAnimatorDriver animatorDriver;
        private EnemyFriendlyDetector friendlyDetector;

        [Header("行为状态机")]
        [Tooltip("友军进入此半径（与敌人身上第一个 Trigger 圆形碰撞体同步）时会被追击索敌。")]
        [SerializeField] private float chaseDetectRadius = 2.5f;
        [Tooltip("为 0：追击时不会自动切入「战斗」状态。大于 0：与当前追击友军距离 ≤ 该值时切入 Battle（战斗逻辑可后续在 EnemyBattleState 中实现）。")]
        [SerializeField] private float battleEnterDistance = 0f;
        [Tooltip("在 Scene 中绘制追击检测圆与战斗距离圆（与 Inspector 数值同步）。")]
        [SerializeField] private bool drawBehaviorGizmos = true;

        public EnemyAttackType AttackType => attackType;
        public float AttackRange => attackRange;
        public float AttackSpeed => attackSpeed;
        public int MoveSpeed => Mathf.Max(1, finalMoveSpeed > 0 ? finalMoveSpeed : baseMoveSpeed);
        public int AttackDamage => Mathf.Max(1, finalAttack);
        public float BattleEnterDistance => Mathf.Max(0f, battleEnterDistance);
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

            EnsureBehaviorComponents();
            stateController?.StartStateMachine();
        }

        private void Update()
        {
            stateController?.Tick(Time.deltaTime);
        }

        private void OnValidate()
        {
            ApplyChaseDetectRadiusToCollider();
        }

        private void OnDrawGizmos()
        {
            if (!drawBehaviorGizmos)
                return;

            float chaseR = Mathf.Max(0.05f, chaseDetectRadius);
            Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.55f);
            Gizmos.DrawWireSphere(transform.position, chaseR);

            float battleR = Mathf.Max(0f, battleEnterDistance);
            if (battleR > 0.001f)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.15f, 0.65f);
                Gizmos.DrawWireSphere(transform.position, battleR);
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

        private void EnsureBehaviorComponents()
        {
            if (cachedMove == null)
                cachedMove = GetComponent<EnemyMove>();
            if (cachedMove == null)
                cachedMove = gameObject.AddComponent<EnemyMove>();

            if (friendlyDetector == null)
                friendlyDetector = GetComponent<EnemyFriendlyDetector>();
            if (friendlyDetector == null)
                friendlyDetector = gameObject.AddComponent<EnemyFriendlyDetector>();

            EnsureFriendlyTriggerCollider();

            if (stateController == null)
                stateController = GetComponent<EnemyStateController>();
            if (stateController == null)
                stateController = gameObject.AddComponent<EnemyStateController>();

            if (animatorDriver == null)
                animatorDriver = GetComponent<EnemyAnimatorDriver>();
            if (animatorDriver == null)
                animatorDriver = gameObject.AddComponent<EnemyAnimatorDriver>();

            cachedMove.SetStateMachineDriven(true);
            stateController.Bind(this, cachedMove, friendlyDetector, animatorDriver);
        }

        /// <summary>将 Inspector 中的追击半径同步到已有 Trigger 圆碰撞体（允许缩小）；无则仅在 Ensure 时创建。</summary>
        private void ApplyChaseDetectRadiusToCollider()
        {
            CircleCollider2D[] circles = GetComponents<CircleCollider2D>();
            for (int i = 0; i < circles.Length; i++)
            {
                CircleCollider2D col = circles[i];
                if (col != null && col.isTrigger)
                {
                    col.radius = Mathf.Max(0.1f, chaseDetectRadius);
                    return;
                }
            }
        }

        private void EnsureFriendlyTriggerCollider()
        {
            ApplyChaseDetectRadiusToCollider();

            CircleCollider2D[] circles = GetComponents<CircleCollider2D>();
            for (int i = 0; i < circles.Length; i++)
            {
                CircleCollider2D col = circles[i];
                if (col != null && col.isTrigger)
                    return;
            }

            CircleCollider2D triggerCol = gameObject.AddComponent<CircleCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.radius = Mathf.Max(0.1f, chaseDetectRadius);
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