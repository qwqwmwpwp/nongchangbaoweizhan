using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// 结果流程“控制层”脚本：
/// - 给 GameFlowManager 提供失败/胜利 UI 的统一入口
/// - 给 UGUI 按钮提供“重新开始”方法
/// - 在失败/胜利面板上绑定 <see cref="LoadMainMenu"/> 返回主菜单（走 SceneLoadManager）
/// </summary>
public class GameOverC : MonoBehaviour
{
    public static GameOverC instance;

    [Header("结果视图")]
    [SerializeField] private GameOverM gameOverM;

    [Header("返回主菜单")]
    [Tooltip("须与 Build Settings 中的场景名一致。")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [System.Serializable]
    private class DebugLogPayload
    {
        public string sessionId;
        public string runId;
        public string hypothesisId;
        public string location;
        public string message;
        public string data;
        public long timestamp;
    }

    private void AgentLog(string runId, string hypothesisId, string location, string message, string data)
    {
        // #region agent log
        var payload = new DebugLogPayload
        {
            sessionId = "46034e",
            runId = runId,
            hypothesisId = hypothesisId,
            location = location,
            message = message,
            data = data,
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        string line = JsonUtility.ToJson(payload) + "\n";
        string logPath = Path.Combine(Directory.GetCurrentDirectory(), "debug-46034e.log");
        File.AppendAllText(logPath, line);
        // #endregion
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// 失败时调用：显示失败界面。
    /// </summary>
    public void ShowDefeat()
    {
        AgentLog(
            "pre-fix",
            "H9",
            "GameOverC.cs:ShowDefeat",
            "show defeat called",
            $"hasGameOverM={(gameOverM != null)}, mainMenuScene={mainMenuSceneName}");
        if (gameOverM != null)
            gameOverM.ShowDefeat();
    }

    /// <summary>
    /// 胜利时调用：显示胜利界面。
    /// </summary>
    public void ShowVictory()
    {
        AgentLog(
            "pre-fix",
            "H10",
            "GameOverC.cs:ShowVictory",
            "show victory called",
            $"hasGameOverM={(gameOverM != null)}, mainMenuScene={mainMenuSceneName}");
        if (gameOverM != null)
            gameOverM.ShowVictory();
    }

    /// <summary>
    /// UGUI 按钮可直接绑定：
    /// 优先调用 SceneLoadManager 保持项目统一转场体验；
    /// 若场景里没有 SceneLoadManager，则回退到原生场景重载。
    /// </summary>
    public void RestartCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (SceneLoadManager.Instance != null)
            SceneLoadManager.Instance.LoadScene(sceneName);
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// 返回主菜单：经 SceneLoadManager 淡入淡出加载（timeScale 在加载协程内恢复为 1）。
    /// </summary>
    public void LoadMainMenu()
    {
        if (SceneLoadManager.Instance != null)
            SceneLoadManager.Instance.LoadScene(mainMenuSceneName);
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
