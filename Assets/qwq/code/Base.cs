using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 基地接口实现
/// </summary>
public class Base : MonoBehaviour, IDamageable
{
    public GameObject Object => gameObject;

    [SerializeField] private BaseDataSO baseData;
    private int hp;
    public bool isGameOver;

    private void Start()
    {
        if (baseData == null)
        {
            Debug.LogError($"Base: 未指定 BaseDataSO（{gameObject.name}）", this);
            return;
        }

        hp = baseData.MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isGameOver)
            return;

        // 与 GameFlowManager 共用同一套基地血量与失败判定（避免两套 HP 不一致）
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
