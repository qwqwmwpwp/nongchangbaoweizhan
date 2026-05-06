using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAnimatorDriver : MonoBehaviour
{
    private const float MoveDeadZone = 0.02f;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Manual Animator State Names")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string moveBackStateName = "MoveBack";
    [SerializeField] private string moveForwardStateName = "MoveForward";
    [SerializeField] private string battleStateName = "Battle";
    [SerializeField] private string deathStateName = "";
    [SerializeField] private int layerIndex = 0;

    [Header("Facing")]
    [SerializeField] private bool flipFacingByNegativeScaleX = false;
    [SerializeField] private float flipHorizontalDeadZone = 0.0008f;

    private int idleStateHash;
    private int moveBackStateHash;
    private int moveForwardStateHash;
    private int battleStateHash;
    private int deathStateHash;
    private int currentStateHash;
    private float cachedAbsScaleX = 1f;
    private float lastWorldPosX;
    private bool lastVerticalMoveWasBack;
    private EnemyRewindRecorder rewindRecorder;
    private EnemyStateController stateController;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        stateController = GetComponentInParent<EnemyStateController>();
        rewindRecorder = GetComponentInParent<EnemyRewindRecorder>();
        cachedAbsScaleX = Mathf.Abs(transform.localScale.x);
        if (cachedAbsScaleX < 1e-4f)
            cachedAbsScaleX = 1f;

        CacheHashes();
    }

    private void OnEnable()
    {
        lastWorldPosX = transform.position.x;
    }

    private void OnValidate()
    {
        if (flipFacingByNegativeScaleX)
        {
            float absScaleX = Mathf.Abs(transform.localScale.x);
            if (absScaleX > 1e-4f)
                cachedAbsScaleX = absScaleX;
        }

        CacheHashes();
    }

    public void PlayIdle(bool restart = false)
    {
        PlayState(idleStateHash, restart);
    }

    public void PlayMove(Vector3 delta, bool isMoving)
    {
        if (!isMoving || delta.sqrMagnitude < MoveDeadZone * MoveDeadZone)
        {
            PlayIdle();
            return;
        }

        if (delta.y > MoveDeadZone)
            lastVerticalMoveWasBack = true;
        else if (delta.y < -MoveDeadZone)
            lastVerticalMoveWasBack = false;

        PlayState(lastVerticalMoveWasBack ? moveBackStateHash : moveForwardStateHash);
    }

    public void PlayBattle(bool restart = true)
    {
        PlayState(battleStateHash, restart);
    }

    public bool PlayDeath(bool restart = true)
    {
        if (deathStateHash == 0)
            return false;

        PlayState(deathStateHash, restart);
        return true;
    }

    public bool IsDeathAnimationFinished()
    {
        if (animator == null || deathStateHash == 0)
            return true;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool isDeathState = stateInfo.shortNameHash == deathStateHash || stateInfo.fullPathHash == deathStateHash;
        return isDeathState && !animator.IsInTransition(layerIndex) && stateInfo.normalizedTime >= 1f;
    }

    public void ForceLocomotionIdleForRewind()
    {
        PlayIdle();
    }

    public void ForceIdleAfterBattleAnimation()
    {
        PlayIdle(true);
    }

    public void OnAttackHit()
    {
        stateController?.OnAttackHit();
    }

    public void OnBattleAnimationFinished()
    {
        if (stateController != null)
        {
            stateController.OnBattleAnimationFinished();
            return;
        }

        ForceIdleAfterBattleAnimation();
    }

    public void OnDeathAnimationFinished()
    {
        stateController?.OnDeathAnimationFinished();
    }

    private void LateUpdate()
    {
        if (!flipFacingByNegativeScaleX)
            return;

        float x = transform.position.x;
        if (rewindRecorder != null && rewindRecorder.IsRewinding)
        {
            lastWorldPosX = x;
            return;
        }

        float dx = x - lastWorldPosX;
        lastWorldPosX = x;
        if (Mathf.Abs(dx) < flipHorizontalDeadZone)
            return;

        Vector3 localScale = transform.localScale;
        localScale.x = cachedAbsScaleX * Mathf.Sign(dx);
        transform.localScale = localScale;
    }

    private void PlayState(int stateHash, bool restart = false)
    {
        if (animator == null || stateHash == 0)
            return;

        if (!restart && currentStateHash == stateHash)
            return;

        animator.Play(stateHash, layerIndex, 0f);
        currentStateHash = stateHash;
    }

    private void CacheHashes()
    {
        idleStateHash = string.IsNullOrEmpty(idleStateName) ? 0 : Animator.StringToHash(idleStateName);
        moveBackStateHash = string.IsNullOrEmpty(moveBackStateName) ? 0 : Animator.StringToHash(moveBackStateName);
        moveForwardStateHash = string.IsNullOrEmpty(moveForwardStateName) ? 0 : Animator.StringToHash(moveForwardStateName);
        battleStateHash = string.IsNullOrEmpty(battleStateName) ? 0 : Animator.StringToHash(battleStateName);
        deathStateHash = string.IsNullOrEmpty(deathStateName) ? 0 : Animator.StringToHash(deathStateName);
    }
}
