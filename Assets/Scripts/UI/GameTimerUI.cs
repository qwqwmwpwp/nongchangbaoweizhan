using TMPro;
using UnityEngine;

/// <summary>
/// 将 GameTimerRuntime 的计时结果显示在 TMP 文本上。
/// </summary>
public class GameTimerUI : MonoBehaviour
{
    [SerializeField] private GameTimerRuntime timerRuntime;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private string prefix = "时间: ";

    private void Reset()
    {
        timerText = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (timerRuntime == null)
            timerRuntime = FindAnyObjectByType<GameTimerRuntime>();
    }

    private void Update()
    {
        if (timerText == null)
            return;

        if (timerRuntime == null)
        {
            timerText.text = $"{prefix}--:--";
            return;
        }

        timerText.text = $"{prefix}{GameTimerRuntime.FormatMMSS(timerRuntime.ElapsedSeconds)}";
    }
}
