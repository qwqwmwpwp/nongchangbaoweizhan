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
            targetNode = null;
            Destroy(gameObject);
            return;
        }

        int randomIndex = Random.Range(0, targetNode.nextNodes.Count);
        targetNode = targetNode.nextNodes[randomIndex];
    }
}
