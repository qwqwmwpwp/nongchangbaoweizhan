using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public int speed = 5;
    private Transform targetTransform;
    private int wayPointIndex = 0;
    private Vector3 dic;
    private void Start()
    {
        targetTransform = WayPointManager.Instance.points[wayPointIndex].transform;
    }
    private void Update()
    {
        dic = (targetTransform.position - gameObject.transform.position).normalized;
        transform.Translate(dic * speed * Time.deltaTime);
        if(Vector3.Distance(transform.position,targetTransform.position)<=0.1&&wayPointIndex!=WayPointManager.Instance.points.Count-1)
        {
            wayPointIndex++;
            targetTransform = WayPointManager.Instance.points[wayPointIndex].transform;
        }
    }
}
