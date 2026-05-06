using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-100)]
public class UIManager : MonoBehaviour
{
    // 单例实例
    public static UIManager Instance { get; private set; }

    // 存储所有注册的UI元素：键为UI名称，值为UI GameObject
    private Dictionary<string, GameObject> uiDictionary = new();

    private void Awake()
    {
        // 单例模式：确保只有一个实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可选：使UI管理器在场景加载时不被销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 注册UI方法：将UI GameObject添加到字典中
    public void RegisterUI(string uiName, GameObject uiObject)
    {
        if (uiDictionary.ContainsKey(uiName))
        {
            Debug.LogWarning($"UI {uiName} 已经注册，将被覆盖。");
            uiDictionary[uiName] = uiObject;
        }
        else
        {
            uiDictionary.Add(uiName, uiObject);
        }
    }

    // 显示UI方法：通过名称激活UI
    public void ShowUI(string uiName)
    {
        if (uiDictionary.ContainsKey(uiName))
        {
            uiDictionary[uiName].SetActive(true);
        }
        else
        {
            Debug.LogError($"UI {uiName} 未注册，无法显示。");
        }
    }

    // 隐藏UI方法：通过名称禁用UI
    public void HideUI(string uiName)
    {
        if (uiDictionary.ContainsKey(uiName))
        {
            uiDictionary[uiName].SetActive(false);
        }
        else
        {
            Debug.LogError($"UI {uiName} 未注册，无法隐藏。");
        }
    }

    // 切换UI方法：隐藏当前UI，显示另一个UI（可选功能）
    public void SwitchUI(string hideUIName, string showUIName)
    {
        HideUI(hideUIName);
        ShowUI(showUIName);
    }
}
