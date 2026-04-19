using UnityEngine;

/// <summary>
/// 场景中的能量池：监听击杀事件累加资源，并广播能量变化。
/// 挂到任意常驻物体（如 GameFlow 同级空物体）即可。
/// </summary>
public class EnergyPoolRuntime : MonoBehaviour
{
    public static EnergyPoolRuntime Instance { get; private set; }

    [SerializeField]private int maxEnergy  = 100;  // 最大能量值
    [SerializeField] private int initialEnergy;     // 初始能量值

    [SerializeField] private int _current;  // 当前能量值

    public int current => _current;
    public int max => maxEnergy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        maxEnergy = Mathf.Max(1, maxEnergy);
        _current = Mathf.Clamp(initialEnergy, 0, maxEnergy);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        GameEvent.EnemyDefeatedReward += OnEnemyDefeatedReward;
    }

    private void OnDisable()
    {
        GameEvent.EnemyDefeatedReward -= OnEnemyDefeatedReward;
    }

    private void Start()
    {
        RaiseChanged();
    }

    private void OnEnemyDefeatedReward(int resourcePoints)
    {
        if (resourcePoints <= 0) return;
        AddEnergy(resourcePoints);
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;
        _current = Mathf.Min(maxEnergy, _current + amount);
        RaiseChanged();
    }

    /// <summary>技能等消耗能量时调用；不足返回 false。</summary>
    public bool TryConsume(int amount)
    {
        if (amount <= 0) return true;
        if (_current < amount) return false;
        _current -= amount;
        RaiseChanged();
        return true;
    }

    public void SetMaxEnergy(int newMax, bool clampCurrent = true)
    {
        maxEnergy = Mathf.Max(1, newMax);
        if (clampCurrent)
            _current = Mathf.Clamp(_current, 0, maxEnergy);
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        GameEvent.TriggerEnergyChanged(_current, maxEnergy);
    }
}
