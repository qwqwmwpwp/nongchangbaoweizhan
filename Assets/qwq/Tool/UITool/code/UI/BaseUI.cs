using UnityEngine;

public abstract class BaseUI : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("UI的唯一标识名称，用于在UIManager中注册")]
    [SerializeField] private string uiName = "UnnamedUI";

    [Tooltip("是否在Awake时自动注册到UIManager")]
    [SerializeField] private bool autoRegister = true;

    [Tooltip("是否在注册后自动隐藏UI")]
    [SerializeField] private bool hideOnRegister = true;

    protected virtual bool ShouldHideOnRegister => hideOnRegister;

    protected virtual void Awake()
    {
        if (autoRegister)
        {
            RegisterUI();
        }
    }

    /// <summary>
    /// 注册UI到UIManager
    /// </summary>
    public void RegisterUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterUI(uiName, gameObject);

            if (ShouldHideOnRegister)
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("未找到 UIManager 实例。跳过 UI 注册。");
        }
    }

    /// <summary>
    /// 显示UI
    /// </summary>
    public void Show()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowUI(uiName);
            OnShow();
        }
    }

    /// <summary>
    /// 隐藏UI
    /// </summary>
    public void Hide()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideUI(uiName);
            OnHide();
        }
    }

    /// <summary>
    /// 切换UI（隐藏当前UI，显示另一个UI）
    /// </summary>
    /// <param name="showUIName">要显示的UI名称</param>
    public void SwitchTo(string showUIName)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SwitchUI(uiName, showUIName);
            OnHide();
        }
    }

    /// <summary>
    /// UI显示时的回调（子类可重写）
    /// </summary>
    protected virtual void OnShow() { }

    /// <summary>
    /// UI隐藏时的回调（子类可重写）
    /// </summary>
    protected virtual void OnHide() { }
}