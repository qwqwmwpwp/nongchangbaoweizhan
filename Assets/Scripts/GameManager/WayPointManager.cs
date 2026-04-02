using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPointManager : MonoBehaviour
{
    private static WayPointManager _Instance;
    public static WayPointManager Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = FindAnyObjectByType<WayPointManager>();
                if (_Instance == null)
                {
                    GameObject go = new GameObject("WayPointManager");
                    _Instance = go.AddComponent<WayPointManager>();
                }
            }
            return _Instance;
        }
        private set
        {
            _Instance = value;
        }
    }
    public List<GameObject> points;
    private void Awake()
    {
        if(Instance!=null&&Instance!=this)
        {
            Destroy(gameObject);
            return;
        }
        _Instance = this;
    }
    public Color pathColor = Color.cyan;
    public float nodeRadius = 0.3f;
    public bool showLabels = true;
    private void OnDrawGizmos()
    {
        if (points == null || points.Count == 0) return;

        Gizmos.color = pathColor;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null) continue;

            Vector3 currentPos = points[i].transform.position;

            // 1. 画出路点球体
            Gizmos.DrawWireSphere(currentPos, nodeRadius);

            // 2. 如果不是最后一个点，画出连线和方向指示
            if (i < points.Count - 1 && points[i + 1] != null)
            {
                Vector3 nextPos = points[i + 1].transform.position;

                // 画主连线
                Gizmos.DrawLine(currentPos, nextPos);

                // 画一个简单的方向小箭头（在连线中间画个小球代替，或者指向下一个点）
                Vector3 direction = (nextPos - currentPos).normalized;
                Vector3 arrowPos = currentPos + direction * (Vector3.Distance(currentPos, nextPos) * 0.5f);
                Gizmos.DrawSphere(arrowPos, nodeRadius * 0.5f);
            }
        }
    }
}
