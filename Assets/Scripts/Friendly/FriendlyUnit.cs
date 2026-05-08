using UnityEngine;
using qwq;

[DisallowMultipleComponent]
public class FriendlyUnit : MonoBehaviour, IDamageable
{
    [SerializeField] private FriendlyUnitDataSO data;
    [Tooltip("在 Scene 中未选中本物体时也绘制索敌/攻击/追击范围（调数值时不必保持选中 Hierarchy）。")]
    [SerializeField] private bool drawGizmosInSceneWhenNotSelected = true;
    [SerializeField] private float detectRadius = 6f;
    [SerializeField] private float chaseRadius = 8f;
    [SerializeField] private LayerMask enemyLayer = ~0;

    private int attack;
    private int moveSpeed;
    private float attackRange;
    private float attackSpeed;
    private FriendlyOrbitMovement guardMover;
    private FriendlyUnitStateController stateController;
    private FriendlyUnitAnimatorDriver animatorDriver;
    private BambooCtx ownerBambooCtx;
    private Transform assignedReturnPoint;

    /// <summary>近战追击占用本单位的敌军（每名友军同时最多一名敌军可锁定）。</summary>
    private Enemy meleeEngagedBy;

    private int hp;
    private int hpMax;
    private bool isDead;

    public GameObject obj => gameObject;

    public int Attack => attack;
    public int MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public float AttackSpeed => attackSpeed;
    public float DetectRadius => detectRadius;
    public float ChaseRadius => chaseRadius;
    public LayerMask EnemyLayer => enemyLayer;
    public FriendlyOrbitMovement GuardMover => guardMover;
    public BambooCtx OwnerBambooCtx => ownerBambooCtx;
    public Transform AssignedReturnPoint => assignedReturnPoint;
    public bool IsDead => isDead;
    public bool IsInteractable => !isDead && gameObject.activeInHierarchy;

    [Header("UI")]
    [SerializeField] private FriendlyHealthUI friendlyHealthUI;

    /// <summary>敌军尝试占用本友军用于近战追击；已被其他敌军占用则返回 false。</summary>
    public bool TryClaimMeleeEngagement(Enemy attacker)
    {
        if (isDead)
            return false;

        if (attacker == null || attacker as Object == null)
            return false;

        if (meleeEngagedBy as Object == null)
        {
            meleeEngagedBy = attacker;
            return true;
        }

        return meleeEngagedBy == attacker;
    }

    /// <summary>敌军离开追击/回路径/销毁时释放占用。</summary>
    public void ReleaseMeleeEngagement(Enemy attacker)
    {
        if (attacker == null || attacker as Object == null)
            return;
        if (meleeEngagedBy == attacker)
            meleeEngagedBy = null;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        hp -= amount;
        if (hp > 0)
        {
            RefreshHealthUI();
            return;
        }

        BeginDeath();
    }

    public void DestroyAfterDeathAnimation()
    {
        Destroy(gameObject);
    }

    public void ForceDeath()
    {
        BeginDeath();
    }

    private void BeginDeath()
    {
        if (isDead)
            return;

        isDead = true;
        hp = 0;
        RefreshHealthUI();

        if (meleeEngagedBy != null && meleeEngagedBy as Object != null)
        {
            EnemyStateController esc = meleeEngagedBy.GetComponent<EnemyStateController>();
            if (esc != null)
                esc.ClearFriendlyTargetIfCurrent(this);
            else
                ReleaseMeleeEngagement(meleeEngagedBy);
        }
        meleeEngagedBy = null;

        DisableExternalInteractions();
        if (stateController != null)
            stateController.SwitchToDeath();
        else
            DestroyAfterDeathAnimation();
    }

    public void Init(
        FriendlyUnitDataSO initData,
        Transform ownerTower,
        float guardForwardOffset,
        Vector2 guardBoxSize,
        float guardMoveSpeedScale,
        float initDetectRadius,
        float initChaseRadius,
        BambooCtx bambooCtx,
        Transform returnPoint)
    {
        if (initData != null)
            data = initData;

        isDead = false;
        ApplyData();
        detectRadius = Mathf.Max(0.1f, initDetectRadius);
        chaseRadius = Mathf.Max(detectRadius, initChaseRadius);
        ownerBambooCtx = bambooCtx;
        assignedReturnPoint = returnPoint;

        guardMover = GetComponent<FriendlyOrbitMovement>();
        if (guardMover == null)
            guardMover = gameObject.AddComponent<FriendlyOrbitMovement>();
        guardMover.Init(ownerTower, guardForwardOffset, guardBoxSize, moveSpeed, guardMoveSpeedScale);

        EnsureBehaviorComponents();
        stateController.StartStateMachine();
    }

    private void Awake()
    {
        if (data != null)
            ApplyData();

        EnsureBehaviorComponents();
    }

    private void Update()
    {
        stateController?.Tick(Time.deltaTime);
    }

    private void ApplyData()
    {
        if (data == null)
            return;

        attack = Mathf.Max(1, data.Attack);
        moveSpeed = Mathf.Max(1, data.MoveSpeed);
        attackRange = Mathf.Max(0.1f, data.AttackRange);
        attackSpeed = Mathf.Max(0.1f, data.AttackSpeed);
        hpMax = Mathf.Max(1, data.MaxHealth);
        hp = hpMax;
        RefreshHealthUI();
    }

    private void DisableExternalInteractions()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void RefreshHealthUI()
    {
        if (friendlyHealthUI == null)
            return;
        friendlyHealthUI.PlayerHealthChange(hp, hpMax);
    }

    private void EnsureBehaviorComponents()
    {
        if (animatorDriver == null)
            animatorDriver = GetComponent<FriendlyUnitAnimatorDriver>();
        if (animatorDriver == null)
            animatorDriver = gameObject.AddComponent<FriendlyUnitAnimatorDriver>();

        if (stateController == null)
            stateController = GetComponent<FriendlyUnitStateController>();
        if (stateController == null)
            stateController = gameObject.AddComponent<FriendlyUnitStateController>();

        stateController.Bind(this, animatorDriver);
    }

    private void OnDrawGizmos()
    {
        if (drawGizmosInSceneWhenNotSelected)
            DrawFriendlyRangeGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmosInSceneWhenNotSelected)
            DrawFriendlyRangeGizmos();
    }

    private void DrawFriendlyRangeGizmos()
    {
        FriendlyOrbitMovement mover = guardMover != null ? guardMover : GetComponent<FriendlyOrbitMovement>();
        mover?.DrawGizmos(detectRadius, attackRange, chaseRadius);
    }
}
