using UnityEngine;

/*
 * ========== Animator 连线方案（无 Blend Tree）==========
 * 当前无「回溯」专用动画；全局回溯播放期间由代码 ForceLocomotionIdleForRewind() 将
 * Speed=0、IsBack=false、IsBattle=false、MoveDir=0；Trigger 模式时 ResetTrigger(Attack)。状态机应能切回 Idle。
 *
 * Parameters（名称与 Inspector 一致）：
 *   Speed (Float)  0=停，1=走
 *   IsBack (Bool)  true=背向行走（背部朝位移方向），非时间回溯
 *   IsBattle (Bool)
 *   Attack (Trigger，可选；Play 攻击模式可不建此参数)
 *   MoveDir (Int，可选) 0待机 1左 2右 3上 4下；不配则 moveDirParam 留空
 *
 * 状态与过渡（建议状态名）：
 *   1) Entry -> Idle（默认）
 *   2) Idle -> MoveForward：Speed>0.01 && !IsBack && !IsBattle
 *   3) Idle -> MoveBackward：Speed>0.01 && IsBack && !IsBattle
 *   4) MoveForward / MoveBackward -> Idle：Speed<0.01 && !IsBattle（或互切：IsBack 翻转）
 *   5) Idle|MoveForward|MoveBackward -> Battle：IsBattle==true（短过渡、可打断）
 *   6) Battle -> Idle：IsBattle==false
 *   7) 攻击：Trigger 模式 BattleBase->Attack 用 Attack；Play 模式由代码 Play(attackStateName)，Animator 可不建 Attack 参数
 *   8) 攻击片段命中帧：Animation Event -> OnAttackHit（事件不能单独「开始」攻击，须先有 Trigger 或 Play）
 *
 * 左右镜像：flipFacingByNegativeScaleX 时于 LateUpdate 用「世界坐标 X 帧间差 Δx」翻面；素材默认朝右则 Δx>0 scale.x 为正，Δx<0 为负。
 * 若启用 MoveDir（四向走）：各 Walk 状态增加 MoveDir==1..4 与 Speed、!IsBattle 组合；回 Idle 用 Speed<0.01 或 MoveDir==0。
 * Animator 须与本组件挂在同一 GameObject（与 Animator 同体）。
 * ======================================================
 */

/// <summary>
/// 敌人动画参数驱动：移动方向（上下左右）与正/背向行走由本脚本计算，Animator 只做离散状态切换，无需 Blend Tree。
/// 「背向」指背朝位移方向的行走表现（BackwardWalk），不是时间回溯类技能动画。回溯时无专用片段，由 <see cref="ForceLocomotionIdleForRewind"/> 强制回到待机表现。
/// 攻击可用 <see cref="AttackAnimDriveMode.AnimatorTrigger"/> 或 <see cref="AttackAnimDriveMode.AnimatorPlayState"/>。
/// 左右镜像：启用 <c>flipFacingByNegativeScaleX</c> 时按世界 X 的帧间位移 Δx 设置 scale.x（默认素材朝右）。
/// </summary>
[DisallowMultipleComponent]
public class EnemyAnimatorDriver : MonoBehaviour
{
    private const float MoveDeadZone = 0.02f;

    public enum AttackAnimDriveMode
    {
        [Tooltip("Animator 里用 Attack Trigger 从 Battle 切入攻击状态。")]
        AnimatorTrigger,
        [Tooltip("不用 Attack 参数；由代码 Animator.Play(attackStateName) 直接播放攻击层状态。")]
        AnimatorPlayState,
    }

    [Header("引用")]
    [SerializeField] private Animator animator;

    [Header("参数名（Animator Parameters 中需同名）")]
    [SerializeField] private string speedParam = "Speed";
    [Tooltip("Int：0=待机，1=左，2=右，3=上，4=下。留空则不写入该参数（仅用 Speed + IsBack + IsBattle）。")]
    [SerializeField] private string moveDirParam = "";
    [Tooltip("为 true 时播「背向移动」行走（背部朝向位移方向），与回溯/倒放无关。")]
    [SerializeField] private string isBackParam = "IsBack";
    [SerializeField] private string isBattleParam = "IsBattle";
    [Tooltip("仅在 AttackAnimDriveMode=AnimatorTrigger 时使用。")]
    [SerializeField] private string attackTriggerParam = "Attack";

    [Header("攻击动画驱动")]
    [SerializeField] private AttackAnimDriveMode attackAnimMode = AttackAnimDriveMode.AnimatorTrigger;
    [Tooltip("Animator 中攻击状态名；若在子状态机内，用路径如 BattleSM/Attack。Play 模式必填。")]
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private int attackLayerIndex = 0;

    [Header("左右朝向（缩放，素材默认朝右）")]
    [Tooltip("为 true 时：每帧用世界坐标 X 与上一帧的差 Δx 判断左右；Δx>0 则 scale.x=+|基准|（朝右），Δx<0 则 scale.x=-|基准|（镜像朝左）。")]
    [SerializeField] private bool flipFacingByNegativeScaleX = false;
    [Tooltip("|Δx| 小于该值不翻面（纯竖直移动或抖动）。单位：世界空间米。")]
    [SerializeField] private float flipHorizontalDeadZone = 0.0008f;

    [Header("朝向（用于区分正向走 / 背向走，非回溯）")]
    [Tooltip("在 XY 平面上角色「正面」的世界向量；移动方向与正面夹角大于 90° 则 IsBack=true（背向移动动画）。")]
    [SerializeField] private bool useTransformUpAsForward = true;
    [Tooltip("若关闭 useTransformUpAsForward，则使用本向量（XY）作为正面，需归一化含义由长度体现）。")]
    [SerializeField] private Vector2 manualForwardWorld = Vector2.up;

    private int _speedHash;
    private int _moveDirHash;
    private int _isBackHash;
    private int _isBattleHash;
    private int _attackHash;
    private int _attackStateNameHash;
    private bool _hasMoveDirParam;
    private float _cachedAbsScaleX = 1f;
    private float _lastWorldPosX;
    private EnemyRewindRecorder rewindRecorder;

    private EnemyStateController stateController;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        stateController = GetComponentInParent<EnemyStateController>();
        rewindRecorder = GetComponentInParent<EnemyRewindRecorder>();
        _cachedAbsScaleX = Mathf.Abs(transform.localScale.x);
        if (_cachedAbsScaleX < 1e-4f)
            _cachedAbsScaleX = 1f;
        CacheHashes();
    }

    private void OnEnable()
    {
        _lastWorldPosX = transform.position.x;
    }

    private void OnValidate()
    {
        if (flipFacingByNegativeScaleX)
        {
            float ax = Mathf.Abs(transform.localScale.x);
            if (ax > 1e-4f)
                _cachedAbsScaleX = ax;
        }

        CacheHashes();
    }

    /// <summary>
    /// 根据世界空间移动增量更新 Speed、MoveDir（可选）、IsBack。
    /// </summary>
    public void SetMoveWorldDelta(Vector3 delta, bool isMoving)
    {
        if (animator == null)
            return;

        if (!isMoving || delta.sqrMagnitude < MoveDeadZone * MoveDeadZone)
        {
            animator.SetFloat(_speedHash, 0f);
            if (_hasMoveDirParam)
                animator.SetInteger(_moveDirHash, 0);
            animator.SetBool(_isBackHash, false);
            return;
        }

        Vector2 v = new Vector2(delta.x, delta.y);
        float absX = Mathf.Abs(v.x);
        float absY = Mathf.Abs(v.y);

        if (_hasMoveDirParam)
        {
            int dir;
            if (absX >= absY)
                dir = v.x < 0f ? 1 : 2;
            else
                dir = v.y < 0f ? 4 : 3;

            animator.SetInteger(_moveDirHash, dir);
        }

        animator.SetFloat(_speedHash, 1f);

        Vector2 forward = GetFacingWorldXY();
        if (forward.sqrMagnitude < 1e-8f)
            forward = Vector2.up;
        forward.Normalize();

        v.Normalize();
        // 背向移动：位移与角色「正面」大致相反时播背部朝向位移的走姿，与回溯技能无关。
        bool isBack = Vector2.Dot(v, forward) < 0f;
        animator.SetBool(_isBackHash, isBack);
    }

    private void LateUpdate()
    {
        if (!flipFacingByNegativeScaleX)
            return;

        float x = transform.position.x;

        if (rewindRecorder != null && rewindRecorder.IsRewinding)
        {
            _lastWorldPosX = x;
            return;
        }

        float dx = x - _lastWorldPosX;
        _lastWorldPosX = x;

        if (Mathf.Abs(dx) < flipHorizontalDeadZone)
            return;

        float sign = Mathf.Sign(dx);
        Vector3 ls = transform.localScale;
        ls.x = _cachedAbsScaleX * sign;
        transform.localScale = ls;
    }

    public void SetStateFlags(bool isPath, bool isChase, bool isBattle)
    {
        if (animator == null)
            return;
        animator.SetBool(_isBattleHash, isBattle);
    }

    public void TriggerAttack()
    {
        if (animator == null)
            return;

        if (attackAnimMode == AttackAnimDriveMode.AnimatorPlayState)
        {
            if (string.IsNullOrEmpty(attackStateName))
                return;
            animator.Play(_attackStateNameHash, attackLayerIndex, 0f);
            return;
        }

        animator.SetTrigger(_attackHash);
    }

    /// <summary>
    /// 全局回溯播放中调用：无回溯动画时统一表现为待机（参数归零并清 Attack Trigger）。
    /// </summary>
    public void ForceLocomotionIdleForRewind()
    {
        if (animator == null)
            return;

        animator.SetFloat(_speedHash, 0f);
        if (_hasMoveDirParam)
            animator.SetInteger(_moveDirHash, 0);
        animator.SetBool(_isBackHash, false);
        animator.SetBool(_isBattleHash, false);
        if (attackAnimMode == AttackAnimDriveMode.AnimatorTrigger)
            animator.ResetTrigger(_attackHash);
    }

    /// <summary>
    /// 攻击动画命中帧的 Animation Event 函数名（无参）。须与本组件挂在同一 GameObject 上（与 Animator 同体）。
    /// </summary>
    public void OnAttackHit()
    {
        stateController?.OnAttackHit();
    }

    private Vector2 GetFacingWorldXY()
    {
        if (useTransformUpAsForward)
            return new Vector2(transform.up.x, transform.up.y);
        return manualForwardWorld;
    }

    private void CacheHashes()
    {
        _speedHash = Animator.StringToHash(speedParam);
        _isBackHash = Animator.StringToHash(isBackParam);
        _isBattleHash = Animator.StringToHash(isBattleParam);
        _attackHash = Animator.StringToHash(attackTriggerParam);
        _attackStateNameHash = string.IsNullOrEmpty(attackStateName) ? 0 : Animator.StringToHash(attackStateName);

        _hasMoveDirParam = !string.IsNullOrEmpty(moveDirParam);
        _moveDirHash = _hasMoveDirParam ? Animator.StringToHash(moveDirParam) : 0;
    }
}
