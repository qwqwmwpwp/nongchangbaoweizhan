using UnityEngine;

/// <summary>
/// 游戏计时器：记录本局已进行时长，支持会话结束后自动停表。
/// </summary>
public class GameTimerRuntime : MonoBehaviour
{
    [SerializeField] private bool stopWhenSessionOver = true;
    [SerializeField] private bool useUnscaledTime = false;

    private float _elapsedSeconds;
    private bool _isPaused;

    public float ElapsedSeconds => _elapsedSeconds;

    private void Update()
    {
        if (_isPaused)
            return;

        if (stopWhenSessionOver && GameFlowManager.Instance != null && GameFlowManager.Instance.IsSessionOver)
            return;

        _elapsedSeconds += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    public void ResetTimer()
    {
        _elapsedSeconds = 0f;
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
    }

    public static string FormatMMSS(float totalSeconds)
    {
        int seconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        int minutes = seconds / 60;
        int remain = seconds % 60;
        return $"{minutes:00}:{remain:00}";
    }
}
