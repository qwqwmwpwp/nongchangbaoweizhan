using UnityEngine;

/// <summary>
/// 将「局部回溯预览」与「全局回溯三档」接到 UI Button（主按钮 + 二级面板）。
/// </summary>
public class RewindSkillsHubUI : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private SkillPreviewToggleUI localRewind;
    [SerializeField] private EnemyRewindSkillRuntime globalRewind;
    [Tooltip("全局三档按钮的父节点，初始建议设为隐藏。")]
    [SerializeField] private GameObject globalTierPanel;

    public void OpenLocalRewindPreview()
    {
        if (localRewind == null)
            return;
        localRewind.EnterPreview();
    }

    public void ShowGlobalTierPanel()
    {
        if (globalTierPanel == null)
            return;
        globalTierPanel.SetActive(true);
    }

    public void HideGlobalTierPanel()
    {
        if (globalTierPanel == null)
            return;
        globalTierPanel.SetActive(false);
    }

    public void CastGlobalTier0() => TryCastGlobalTierAndClose(0);
    public void CastGlobalTier1() => TryCastGlobalTierAndClose(1);
    public void CastGlobalTier2() => TryCastGlobalTierAndClose(2);

    private void TryCastGlobalTierAndClose(int tierIndex)
    {
        if (globalRewind == null)
            return;
        if (!globalRewind.CastRewindTier(tierIndex))
            return;
        HideGlobalTierPanel();
    }
}
