using UnityEngine;

/// <summary>
/// 结果面板的“视图层”脚本，只做显示控制：
/// 1) 失败时显示失败面板
/// 2) 胜利时显示胜利面板
/// 3) 场景初始化时默认全部隐藏，避免开局误显示
/// </summary>
public class GameOverM : MonoBehaviour
{
    [Header("结果面板对象")]
    [SerializeField] private GameObject defeatUI;
    [SerializeField] private GameObject victoryUI;

    private void Awake()
    {
        // 场景启动时先清空显示状态，后续由 GameFlowManager 决定显示哪一个结果面板
        HideAll();
    }

    /// <summary>
    /// 显示失败界面并关闭胜利界面。
    /// </summary>
    public void ShowDefeat()
    {
        if (defeatUI != null) defeatUI.SetActive(true);
        if (victoryUI != null) victoryUI.SetActive(false);
    }

    /// <summary>
    /// 显示胜利界面并关闭失败界面。
    /// </summary>
    public void ShowVictory()
    {
        if (victoryUI != null) victoryUI.SetActive(true);
        if (defeatUI != null) defeatUI.SetActive(false);
    }

    /// <summary>
    /// 同时隐藏失败/胜利面板。用于初始化或手动重置状态。
    /// </summary>
    public void HideAll()
    {
        if (defeatUI != null) defeatUI.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(false);
    }
}
