using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RoadNode;

public class EnemyMove : MonoBehaviour
{
    public int speed = 5;
    private RoadNode targetNode;
    Vector3 moveDirection;
    public void StartMove(RoadNode startNode)
    {
        targetNode = startNode;
    }
    private void Update()
    {
        if (targetNode == null) return;

        moveDirection = (targetNode.transform.position - transform.position).normalized;
        transform.Translate(moveDirection * speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetNode.transform.position) <= 0.3f)
        {
            ChooseNextNode();
        }
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
