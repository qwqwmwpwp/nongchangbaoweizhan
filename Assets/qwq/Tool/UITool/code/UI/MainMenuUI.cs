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


    private void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    protected override void OnShow()
    {
        base.OnShow();
        Debug.Log("主菜单显示");
    }

    protected override void OnHide()
    {
        base.OnHide();
        Debug.Log("主菜单隐藏");
    }

    private void OnPlayClicked()
    {
        Debug.Log("开始游戏");
        Hide();
    }

    private void OnSettingsClicked()
    {
        Debug.Log("打开设置");


    }

    private void OnQuitClicked()
    {
        Debug.Log("退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}