using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAnimatorDriver : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Animator animator;

    [Header("参数名")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string isBattleParam = "IsBattle";
    [SerializeField] private string attackTriggerParam = "Attack";

    private int _speedHash;
    private int _moveXHash;
    private int _moveYHash;
    private int _isBattleHash;
    private int _attackHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        CacheHashes();
    }

    private void OnValidate()
    {
        CacheHashes();
    }

    public void SetMoveWorldDelta(Vector3 delta, bool isMoving)
    {
        if (animator == null)
            return;

        Vector2 dir = isMoving ? new Vector2(delta.x, delta.y).normalized : Vector2.zero;
        animator.SetFloat(_moveXHash, dir.x);
        animator.SetFloat(_moveYHash, dir.y);
        animator.SetFloat(_speedHash, isMoving ? 1f : 0f);
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
        animator.SetTrigger(_attackHash);
    }

    private void CacheHashes()
    {
        _speedHash = Animator.StringToHash(speedParam);
        _moveXHash = Animator.StringToHash(moveXParam);
        _moveYHash = Animator.StringToHash(moveYParam);
        _isBattleHash = Animator.StringToHash(isBattleParam);
        _attackHash = Animator.StringToHash(attackTriggerParam);
    }
}
