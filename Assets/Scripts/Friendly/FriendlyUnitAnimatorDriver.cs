using UnityEngine;

[DisallowMultipleComponent]
public class FriendlyUnitAnimatorDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Manual Animator State Names")]
    [SerializeField] private string idleStateName = "";
    [SerializeField] private string moveStateName = "";
    [SerializeField] private string attackStateName = "";
    [SerializeField] private string deathStateName = "";
    [SerializeField] private int layerIndex = 0;

    [Header("Facing")]
    [SerializeField] private bool flipFacingByNegativeScaleX = false;
    [SerializeField] private float flipHorizontalDeadZone = 0.0008f;

    private int idleStateHash;
    private int moveStateHash;
    private int attackStateHash;
    private int deathStateHash;
    private int currentStateHash;
    private float cachedAbsScaleX = 1f;
    private float lastWorldPosX;
    private FriendlyUnitStateController stateController;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        stateController = GetComponentInParent<FriendlyUnitStateController>();
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

    public void Bind(FriendlyUnitStateController controller)
    {
        stateController = controller;
    }

    public bool PlayIdle(bool restart = false)
    {
        if (idleStateHash == 0)
            return false;

        PlayState(idleStateHash, restart);
        return true;
    }

    public bool PlayMove(bool restart = false)
    {
        if (moveStateHash == 0)
            return false;

        PlayState(moveStateHash, restart);
        return true;
    }

    public bool PlayAttack(bool restart = true)
    {
        if (attackStateHash == 0)
            return false;

        PlayState(attackStateHash, restart);
        return true;
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

    public void OnAttackHit()
    {
        if (stateController != null && stateController.OnAttackHit() && AudioManager.Instance != null)
            AudioManager.Instance.PlayMeleeAttackSound();
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
        float dx = x - lastWorldPosX;
        lastWorldPosX = x;
        if (Mathf.Abs(dx) < flipHorizontalDeadZone)
            return;

        Vector3 localScale = transform.localScale;
        localScale.x = cachedAbsScaleX * Mathf.Sign(dx);
        transform.localScale = localScale;
    }

    private void PlayState(int stateHash, bool restart)
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
        moveStateHash = string.IsNullOrEmpty(moveStateName) ? 0 : Animator.StringToHash(moveStateName);
        attackStateHash = string.IsNullOrEmpty(attackStateName) ? 0 : Animator.StringToHash(attackStateName);
        deathStateHash = string.IsNullOrEmpty(deathStateName) ? 0 : Animator.StringToHash(deathStateName);
    }
}
