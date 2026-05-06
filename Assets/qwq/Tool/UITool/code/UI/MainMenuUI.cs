using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MainMenuUI : BaseUI
{
    [Header("UI References")]
    [Tooltip("开始游戏按钮引用")]
    [SerializeField] private Button playButton;

    [Tooltip("设置按钮引用")]
    [SerializeField] private Button settingsButton;

    [Tooltip("退出游戏按钮引用")]
    [SerializeField] private Button quitButton;

    /// <summary>
    /// 初始化方法
    /// </summary>
    private void Start()
    {
        // 设置按钮点击事件
        playButton.onClick.AddListener(OnPlayClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    /// <summary>
    /// UI显示时的回调（重写基类方法）
    /// </summary>
    protected override void OnShow()
    {
        base.OnShow(); // 调用基类实现（良好实践）

        Debug.Log("主菜单显示");
        // 在此处添加主菜单显示时的特定逻辑
        // 例如：播放背景音乐、更新玩家信息等
    }

    /// <summary>
    /// UI隐藏时的回调（重写基类方法）
    /// </summary>
    protected override void OnHide()
    {
        base.OnHide(); // 调用基类实现（良好实践）

        Debug.Log("主菜单隐藏");
        // 在此处添加主菜单隐藏时的清理逻辑
        // 例如：暂停背景音乐、保存设置等
    }

    /// <summary>
    /// 开始游戏按钮点击处理
    /// </summary>
    private void OnPlayClicked()
    {
        Debug.Log("开始游戏");

        // 隐藏主菜单
        Hide();

        // 实际游戏开始逻辑...
        // GameManager.Instance.StartGame();
    }

    /// <summary>
    /// 设置按钮点击处理
    /// </summary>
    private void OnSettingsClicked()
    {
        Debug.Log("打开设置");

        // 切换到设置UI
        SwitchTo("Settings");
    }

    /// <summary>
    /// 退出游戏按钮点击处理
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("退出游戏");

        // 实际退出逻辑...
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
