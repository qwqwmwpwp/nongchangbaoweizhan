using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RoadNode;

public class EnemyMove : MonoBehaviour
{
    public int speed = 5;
    private RoadNode targetNode;
    Vector3 moveDirection;
    private bool isMovementPaused;
    [SerializeField] private float nodeArriveDistance = 0.3f;
    public void StartMove(RoadNode startNode)
    {
        targetNode = startNode;
    }
    private void Update()
    {
        if (isMovementPaused || targetNode == null) return;

        moveDirection = (targetNode.transform.position - transform.position).normalized;
        transform.Translate(moveDirection * speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetNode.transform.position) <= nodeArriveDistance)
        {
            ChooseNextNode();
        }
    }

    public void SetMovementPaused(bool paused)
    {
        isMovementPaused = paused;
    }

    /// <summary>
    /// 当位置被外部改写（如回溯/瞬移）后，重新根据当前位置绑定路径目标，
    /// 避免沿用旧 targetNode 导致抄近路或逆向冲刺。
    /// </summary>
    public void RebindPathFromCurrentPosition()
    {
        RoadNode[] allNodes = FindObjectsByType<RoadNode>(FindObjectsSortMode.None);
        if (allNodes == null || allNodes.Length == 0)
            return;

        RoadNode nearest = null;
        float nearestSqr = float.MaxValue;
        Vector3 currentPos = transform.position;

        for (int i = 0; i < allNodes.Length; i++)
        {
            RoadNode node = allNodes[i];
            if (node == null) continue;

            float sqr = (node.transform.position - currentPos).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = node;
            }
        }

        if (nearest == null)
            return;

        float nearestDist = Mathf.Sqrt(nearestSqr);
        if (nearestDist <= nodeArriveDistance && nearest.nextNodes != null && nearest.nextNodes.Count > 0)
        {
            int randomIndex = Random.Range(0, nearest.nextNodes.Count);
            targetNode = nearest.nextNodes[randomIndex];
            return;
        }

        targetNode = nearest;
    }
    private void ChooseNextNode()
    {
        if (targetNode.nextNodes == null || targetNode.nextNodes.Count == 0)
        {
            // 路径终点 = 抵达基地：先扣基地血再销毁，DummyEnemy 仍会 OnDestroy 通知波次计数
            targetNode = null;
            ApplyDamageToBaseOnReach();
            Destroy(gameObject);
            return;
        }

        int randomIndex = Random.Range(0, targetNode.nextNodes.Count);
        targetNode = targetNode.nextNodes[randomIndex];
    }

    /// <summary>走到终点（无下一节点）时对基地造成一次伤害；若未挂 GameFlowManager 则仅销毁怪物。</summary>
    private void ApplyDamageToBaseOnReach()
    {
        int dmg = 1;
        var enemy = GetComponent<qwq.Enemy>();
        if (enemy != null)
            dmg = enemy.GetLeakDamage();

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.TakeBaseDamage(dmg);
    }
}
