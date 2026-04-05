using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] GameObject GeneratePoint;
    public GameObject Enemy;
    public RoadNode startNode;
    public float t_max = 2;
    float t = 0;
    private void Update()
    {
        Generate();
    }

    private void Generate()
    {
        if (t < t_max)
        {
            t += Time.deltaTime;
            return;
        }
        t = 0;

        //实例化敌人并承接
        GameObject newEnemy = Instantiate(Enemy, GeneratePoint.transform.position, GeneratePoint.transform.rotation);

        //从刚出生的这个新敌人身上，获取它的移动脚本
        EnemyMove mover = newEnemy.GetComponent<EnemyMove>();
        //设定初始点
        mover.StartMove(startNode);
    }
}