using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 基地接口实现
/// </summary>
public class Base : MonoBehaviour, IDamageable
{
    public GameObject obj => this == null ? null : gameObject;

    [SerializeField] private BaseDataSO baseData;
    private int hp;
    public bool isGameOver;

    private void Start()
    {
        if (baseData == null)
        {
            Debug.LogError($"Base: Missing BaseDataSO ({gameObject.name})", this);
            return;
        }

        hp = baseData.MaxHealth;
        hp = 20;
    }

    public void TakeDamage(int amount)
    {
        if (isGameOver)
            return;

        // �?GameFlowManager 共用同一套基地血量与失败判定（避免两�?HP 不一致）
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.TakeBaseDamage(amount);
            isGameOver = GameFlowManager.Instance.IsDefeat;
            return;
        }

        if (baseData == null)
            return;
        hp -= amount;
        if (hp <= 0)
        {
            isGameOver = true;
            if (GameOverC.instance != null)
                GameOverC.instance.ShowDefeat();
        }
    }
}


