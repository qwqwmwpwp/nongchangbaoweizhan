using UnityEngine;
using TMPro;

/// <summary>
/// 战斗信息面板：
/// - 剩余敌人总数（未出生 + 已出生存活）
/// - 基地生命值（当前/最大）
/// 本脚本不改游戏逻辑，只负责读管理器状态并展示。
/// </summary>
public class BattleInfoPanelUI : MonoBehaviour
{
    [Header("文本组件")]
    [SerializeField] private TMP_Text remainingEnemyText;
    [SerializeField] private TMP_Text baseHpText;

    private void Update()
    {
        UpdateRemainingEnemy();
        UpdateBaseHp();
    }

    private void UpdateRemainingEnemy()
    {
        if (remainingEnemyText == null) return;

        if (WaveManager.Instance == null)
        {
            remainingEnemyText.text = "剩余敌人: --";
            return;
        }

        // RemainingEnemyTotal 是关卡剩余总敌数，适合展示“还要打多少只”
        remainingEnemyText.text = $"剩余敌人: {WaveManager.Instance.RemainingEnemyTotal}";
    }

    private void UpdateBaseHp()
    {
        if (baseHpText == null) return;

        if (GameFlowManager.Instance == null)
        {
            baseHpText.text = "基地生命: --";
            return;
        }

        // 显示格式：当前生命/最大生命
        baseHpText.text = $"基地生命: {GameFlowManager.Instance.CurrentHp}/{GameFlowManager.Instance.MaxBaseHealth}";
    }
}
