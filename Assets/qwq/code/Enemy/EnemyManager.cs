using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("EnemyManager 已停用：请使用 WaveManager 统一出怪。", this);
        enabled = false;
    }
}