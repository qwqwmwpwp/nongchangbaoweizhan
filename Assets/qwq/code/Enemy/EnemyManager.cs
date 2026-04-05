using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] GameObject GeneratePoint;
    public GameObject Enemy;
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
        Instantiate(Enemy, GeneratePoint.transform.position, GeneratePoint.transform.rotation);
    }
}
