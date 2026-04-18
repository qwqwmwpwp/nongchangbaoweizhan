using System;

public static class GameEvent
{
    /// <summary>敌人被击杀发放的资源点（漏怪撞基地不应触发）。</summary>
    public static event Action<int> EnemyDefeatedReward;
    /// <summary>能量槽当前值与上限（用于 UI 滑条等）。</summary>
    public static event Action<int, int> EnergyChanged;
    /// <summary>请求场上敌人执行一次位置回溯（参数：回溯秒数、播放时长）。</summary>
    public static event Action<float, float> EnemyRewindRequested;
    /// <summary>单个敌人开始回溯（用于 VFX/SFX 挂接）。</summary>
    public static event Action<UnityEngine.Transform> EnemyRewindStarted;
    /// <summary>单个敌人结束回溯（用于 VFX/SFX 挂接）。</summary>
    public static event Action<UnityEngine.Transform> EnemyRewindEnded;

    public static void TriggerEnemyDefeatedReward(int resourcePoints) => EnemyDefeatedReward?.Invoke(resourcePoints);
    public static void TriggerEnergyChanged(int current, int max) => EnergyChanged?.Invoke(current, max);
    public static void TriggerEnemyRewindRequested(float rewindSeconds, float playbackDuration) =>
        EnemyRewindRequested?.Invoke(rewindSeconds, playbackDuration);
    public static void TriggerEnemyRewindStarted(UnityEngine.Transform enemyTransform) => EnemyRewindStarted?.Invoke(enemyTransform);
    public static void TriggerEnemyRewindEnded(UnityEngine.Transform enemyTransform) => EnemyRewindEnded?.Invoke(enemyTransform);
}
