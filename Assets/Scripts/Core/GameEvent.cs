using System;

public static class GameEvent
{
    /// <summary>敌人被击杀发放的资源点（漏怪撞基地不应触发）。</summary>
    public static event Action<int> EnemyDefeatedReward;
    /// <summary>能量槽当前值与上限（用于 UI 滑条等）。</summary>
    public static event Action<int, int> EnergyChanged;

    public static void TriggerEnemyDefeatedReward(int resourcePoints) => EnemyDefeatedReward?.Invoke(resourcePoints);
    public static void TriggerEnergyChanged(int current, int max) => EnergyChanged?.Invoke(current, max);
}
