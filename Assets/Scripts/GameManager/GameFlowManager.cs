using UnityEngine;

/// <summary>
/// 极简游戏闭环：基地血量、失败、胜利、会话结束。
/// 与 WaveManager 配合：波次由 WaveManager 驱动；敌人抵达路径终点时由 EnemyMove 调用扣血。
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("基地")]
    // 基地最大生命值，开局会写入 currentHp
    [SerializeField] private int maxBaseHealth = 100;

    // 当前基地生命
    private int currentHp;
    // 失败标记：基地血量归零
    private bool isDefeat;
    // 胜利标记：全部波次清空后由 WaveManager 触发
    private bool isVictory;

    public int CurrentHp => currentHp;
    public int MaxBaseHealth => maxBaseHealth;
    /// <summary>基地血量归零</summary>
    public bool IsDefeat => isDefeat;
    /// <summary>最后一波最后一只怪被消灭后由 WaveManager 标记</summary>
    public bool IsVictory => isVictory;
    public bool IsSessionOver => isDefeat || isVictory;

    private void Awake()
    {
        // 简单单例，确保全场景只有一个流程管理器
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentHp = Mathf.Max(0, maxBaseHealth);
    }

    /// <summary>
    /// 基地受到伤害（路径终点漏怪、或 Base 碰撞触发等，统一入口）。
    /// </summary>
    public void TakeBaseDamage(int amount)
    {
        // 会话结束后不再响应任何扣血；防止失败后重复触发逻辑
        if (IsSessionOver || amount <= 0)
            return;

        currentHp -= amount;
        if (currentHp < 0)
            currentHp = 0;

        if (currentHp <= 0)
            EnterDefeat();
    }

    /// <summary>
    /// 由 WaveManager 在「所有波次刷完且场上敌人为 0」时调用。
    /// </summary>
    public void NotifyVictory()
    {
        // 已失败或已胜利时，忽略重复通知
        if (IsSessionOver)
            return;

        isVictory = true;
        Debug.Log("游戏胜利：已清完所有波次。");

        Time.timeScale = 0f;

        if (GameOverC.instance != null)
            GameOverC.instance.ShowVictory();
        else
            Debug.LogWarning("GameFlowManager: 场景中未找到 GameOverC，无法弹出胜利 UI。", this);
    }

    private void EnterDefeat()
    {
        if (isDefeat)
            return;

        isDefeat = true; 
        Debug.Log("游戏失败：基地被摧毁。");

        Time.timeScale = 0f;

        if (GameOverC.instance != null)
            GameOverC.instance.ShowDefeat();
        else
            Debug.LogWarning("GameFlowManager: 场景中未找到 GameOverC，无法弹出游戏结束 UI。", this);
    }
}
