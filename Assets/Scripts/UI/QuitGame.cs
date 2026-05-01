using UnityEngine;

/// <summary>
/// 挂在「退出游戏」按钮上：构建版 <see cref="Application.Quit"/>；在编辑器内则停止 Play（结束演示）。
/// </summary>
public class QuitGame : MonoBehaviour
{
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
