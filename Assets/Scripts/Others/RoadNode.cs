using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadNode : MonoBehaviour
{
    /// <summary>
    /// 挂在在节点上，可选分路
    /// </summary>
    [Header("分路数量")]
    public List<RoadNode> nextNodes = new List<RoadNode>();

    [Header("可视化设置")]
    public float nodeRadius = 0.3f;
    public Color nodeColor = Color.yellow;
    public Color lineColor = Color.cyan;
      private void OnDrawGizmos()
      {
            Gizmos.color = nodeColor;
            Gizmos.DrawWireSphere(transform.position, nodeRadius);

            if (nextNodes == null || nextNodes.Count == 0) return;

            Gizmos.color = lineColor;
            foreach (RoadNode nextNode in nextNodes)
            {
                if (nextNode == null) continue;
                Gizmos.DrawLine(transform.position, nextNode.transform.position);

                Vector3 direction = (nextNode.transform.position - transform.position).normalized;
                Vector3 arrowPos = transform.position + direction * (Vector3.Distance(transform.position, nextNode.transform.position) * 0.5f);
                Gizmos.DrawSphere(arrowPos, nodeRadius * 0.5f);
            }
       }
}
