using UnityEngine;

namespace qwq
{
    public class Enemy : MonoBehaviour, IDamageable
    {
        public GameObject obj => this == null ? null : gameObject;

        [Header("Data")]
        [SerializeField] private EnemyDataSO enemyData;

        private int hp;
        private int baseHpMax;
        private int baseLeakDamage;
        private int baseFriendlyAttack;
        private int baseMoveSpeed;
        private int finalHpMax;
        private int finalFriendlyAttack;
        private int finalMoveSpeed;
        private EnemyAttackType attackType;
        private float attackRange;
        private float attackSpeed;
        private float attackAnimationCooldown;
        private int killResource1;
        private int killResource2;
        private bool isDead;
        private bool rewardGranted;
        private EnemyBuffController buffController;
        private EnemyMove cachedMove;
        private EnemyStateController stateController;
        private EnemyAnimatorDriver animatorDriver;
        private EnemyFriendlyDetector friendlyDetector;

        [Header("Behavior State Machine")]
        [SerializeField] private float chaseDetectRadius = 2.5f;
        [SerializeField] private float battleEnterDistance = 0f;
        [SerializeField] private bool drawBehaviorGizmos = true;

        public EnemyAttackType AttackType => attackType;
        public float AttackRange => attackRange;
        public float AttackSpeed => attackSpeed;
        public float AttackAnimationCooldown => attackAnimationCooldown > 0f
            ? attackAnimationCooldown
            : 1f / Mathf.Max(0.1f, attackSpeed);
        public int MoveSpeed => Mathf.Max(1, finalMoveSpeed > 0 ? finalMoveSpeed : baseMoveSpeed);
        public int AttackDamage => Mathf.Max(1, finalFriendlyAttack);
        public float BattleEnterDistance => Mathf.Max(0f, battleEnterDistance);
        public int KillResource1 => killResource1;
        public int KillResource2 => killResource2;
        public bool HasRewindResistance => isDead || (enemyData != null && enemyData.RewindResistance);
        public bool IsDead => isDead;
        public bool IsInteractable => !isDead && gameObject.activeInHierarchy;

        [Header("UI")]
        public EnemyHealthUI enemyHealthUI;

        private void Start()
        {
            if (enemyData == null)
            {
                Debug.LogError($"Enemy: Missing EnemyDataSO ({gameObject.name})", this);
                return;
            }

            baseHpMax = enemyData.MaxHealth;
            baseLeakDamage = enemyData.Attack;
            baseFriendlyAttack = enemyData.FriendlyAttack;
            baseMoveSpeed = enemyData.MoveSpeed;
            attackType = enemyData.AttackType;
            attackRange = enemyData.AttackRange;
            attackSpeed = enemyData.AttackSpeed;
            attackAnimationCooldown = enemyData.AttackAnimationCooldown;
            killResource1 = enemyData.KillResource1;
            killResource2 = enemyData.KillResource2;
            hp = Mathf.Max(1, baseHpMax);

            cachedMove = GetComponent<EnemyMove>();
            buffController = GetComponent<EnemyBuffController>();
            if (buffController == null)
                buffController = gameObject.AddComponent<EnemyBuffController>();

            RefreshStatsByBuff();
            hp = finalHpMax;
            RefreshHpUI();

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
            if (isDead || enemyData == null || amount <= 0)
                return;

            hp -= amount;
            RefreshHpUI();

            if (hp <= 0)
                DieFromCombat();
        }

        public int AttackBase()
        {
            if (isDead)
                return 0;

            int damage = GetLeakDamage();
            DestroyAfterReachingBase();
            return damage;
        }

        public int GetLeakDamage()
        {
            if (isDead)
                return 0;
            if (enemyData == null)
                return 1;
            return Mathf.Max(1, baseLeakDamage);
        }

        public void ApplyBuff(BuffDataSO buff)
        {
            if (isDead || buff == null)
                return;

            if (buffController == null)
                buffController = GetComponent<EnemyBuffController>() ?? gameObject.AddComponent<EnemyBuffController>();

            buffController.ApplyBuff(buff);
        }

        public void Heal(int amount)
        {
            if (isDead || amount <= 0)
                return;

            hp = Mathf.Min(finalHpMax, hp + amount);
            RefreshHpUI();
        }

        public void ApplyBuffSet(BuffSetSO buffSet)
        {
            if (isDead || buffSet == null)
                return;

            if (buffController == null)
                buffController = GetComponent<EnemyBuffController>() ?? gameObject.AddComponent<EnemyBuffController>();

            buffController.ApplyBuffSet(buffSet);
        }

        public void RefreshStatsByBuff()
        {
            if (isDead)
                return;

            int oldMaxHp = finalHpMax;

            finalHpMax = CalculateFinalIntStat(baseHpMax, BuffTargetStat.MaxHealth, 1);
            finalFriendlyAttack = CalculateFinalIntStat(baseFriendlyAttack, BuffTargetStat.Attack, 1);
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
            BeginDeath(false);
        }

        public void DestroyAfterReachingBase()
        {
            if (isDead)
                return;

            isDead = true;
            hp = 0;
            RefreshHpUI();
            DisableExternalInteractions();
            Destroy(gameObject);
        }

        private void DieFromCombat()
        {
            BeginDeath(true);
        }

        private void BeginDeath(bool grantReward)
        {
            if (isDead)
                return;

            PlayMusic(deathClip);
            isDead = true;
            hp = 0;
            RefreshHpUI();

            if (grantReward && !rewardGranted && enemyData != null)
            {
                int fertilizerReward = killResource1 > 0 ? killResource1 : enemyData.KillResourceReward;
                int energyReward = killResource2;
                if (fertilizerReward > 0 || energyReward > 0)
                    GameEvent.TriggerEnemyDefeatedReward(fertilizerReward, energyReward);
                rewardGranted = true;
            }

            DisableExternalInteractions();
            if (stateController != null)
                stateController.SwitchToDeath();
            else
                Destroy(gameObject);
        }

        private void DisableExternalInteractions()
        {
            if (cachedMove == null)
                cachedMove = GetComponent<EnemyMove>();
            cachedMove?.SetMovementPaused(true);

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = Vector2.zero;

            if (buffController != null)
                buffController.enabled = false;
        }

        [Header("“Ù–ß")]
        [SerializeField] AudioClip attackClip;
        [SerializeField] AudioClip deathClip;

        public void PlayMusic(AudioClip audio)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound(audio);
            }
        }
    }
}

