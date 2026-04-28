using System.Collections.Generic;
using UnityEngine;
using qwq;

[DisallowMultipleComponent]
public class EnemyFriendlyDetector : MonoBehaviour
{
    [SerializeField] private string friendlyTag = "Friend";
    [SerializeField] private LayerMask friendlyLayer;
    private readonly HashSet<FriendlyUnit> friendlyUnitsInRange = new HashSet<FriendlyUnit>();
    private readonly List<FriendlyUnit> scratchSortedFriendlies = new List<FriendlyUnit>();
    private bool warnedLayerFallback;

    private void Awake()
    {
        EnsureFriendlyLayerMask();
    }

    private void Reset()
    {
        EnsureFriendlyLayerMask();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsFriendlyTarget(collision))
            return;

        FriendlyUnit unit = collision.GetComponentInParent<FriendlyUnit>();
        if (unit == null)
            return;
        friendlyUnitsInRange.Add(unit);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsFriendlyTarget(collision))
            return;

        FriendlyUnit unit = collision.GetComponentInParent<FriendlyUnit>();
        if (unit == null)
            return;
        friendlyUnitsInRange.Remove(unit);
    }

    public FriendlyUnit FindNearestFriendly(Vector3 fromPos)
    {
        CleanupDestroyedTargets();
        FriendlyUnit nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (FriendlyUnit unit in friendlyUnitsInRange)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy)
                continue;

            float sqr = (unit.transform.position - fromPos).sqrMagnitude;
            if (sqr >= nearestSqr)
                continue;
            nearestSqr = sqr;
            nearest = unit;
        }

        return nearest;
    }

    /// <summary>从近到远尝试占用友军近战槽；均被占用则返回 null（敌军继续沿路）。</summary>
    public FriendlyUnit FindNearestEngageableFriendly(Vector3 fromPos, Enemy seeker)
    {
        CleanupDestroyedTargets();
        scratchSortedFriendlies.Clear();

        foreach (FriendlyUnit unit in friendlyUnitsInRange)
        {
            if (unit == null || unit as Object == null || !unit.gameObject.activeInHierarchy)
                continue;
            scratchSortedFriendlies.Add(unit);
        }

        if (scratchSortedFriendlies.Count == 0)
            return null;

        scratchSortedFriendlies.Sort((a, b) =>
        {
            float da = (a.transform.position - fromPos).sqrMagnitude;
            float db = (b.transform.position - fromPos).sqrMagnitude;
            return da.CompareTo(db);
        });

        for (int i = 0; i < scratchSortedFriendlies.Count; i++)
        {
            FriendlyUnit unit = scratchSortedFriendlies[i];
            if (unit == null || unit as Object == null)
                continue;
            if (unit.TryClaimMeleeEngagement(seeker))
                return unit;
        }

        return null;
    }

    private void CleanupDestroyedTargets()
    {
        friendlyUnitsInRange.RemoveWhere(unit => unit == null || !unit.gameObject.activeInHierarchy);
    }

    private bool IsFriendlyTarget(Collider2D collision)
    {
        if (collision == null)
            return false;
        FriendlyUnit unit = collision.GetComponentInParent<FriendlyUnit>();
        if (unit == null)
            return false;
        GameObject targetObj = unit.gameObject;
        if (!targetObj.CompareTag(friendlyTag))
            return false;

        int layer = targetObj.layer;
        return (friendlyLayer.value & (1 << layer)) != 0;
    }

    private void EnsureFriendlyLayerMask()
    {
        if (friendlyLayer.value != 0)
            return;

        int friendLayer = LayerMask.NameToLayer("Friend");
        if (friendLayer >= 0)
        {
            friendlyLayer = 1 << friendLayer;
            return;
        }

        // 兜底：若项目未定义 Friend Layer，则允许全层通过并提示一次。
        friendlyLayer = ~0;
        if (!warnedLayerFallback)
        {
            warnedLayerFallback = true;
            Debug.LogWarning("EnemyFriendlyDetector: 未找到 Friend Layer，已回退为全层检测。");
        }
    }
}
