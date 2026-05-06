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

    private int idleStateHash;
    private int moveStateHash;
    private int attackStateHash;
    private int deathStateHash;
    private int currentStateHash;
    private FriendlyUnitStateController stateController;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        stateController = GetComponentInParent<FriendlyUnitStateController>();
        CacheHashes();
    }

    private void OnValidate()
    {
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
        stateController?.OnAttackHit();
    }

    public void OnDeathAnimationFinished()
    {
        stateController?.OnDeathAnimationFinished();
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
