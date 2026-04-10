using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 结果流程“控制层”脚本：
/// - 给 GameFlowManager 提供失败/胜利 UI 的统一入口
/// - 给 UGUI 按钮提供“重新开始”方法
/// </summary>
public class GameOverC : MonoBehaviour
{
    public static GameOverC instance;

    [Header("结果视图")]
    [SerializeField] private GameOverM gameOverM;

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
        if (gameOverM != null)
            gameOverM.ShowDefeat();
    }

    /// <summary>
    /// 胜利时调用：显示胜利界面。
    /// </summary>
    public void ShowVictory()
    {
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
            SceneManager.LoadScene(sceneName);
    }
}
