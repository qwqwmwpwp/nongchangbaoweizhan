using UnityEngine;

/// <summary>
/// 将「局部回溯预览」与「全局回溯三档」接到 UI Button（主按钮 + 二级面板）。
/// </summary>
public class RewindSkillsHubUI : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private SkillPreviewToggleUI localRewind;
    [SerializeField] private SkillPreviewToggleUI catalysisSkill;
    [SerializeField] private EnemyRewindSkillRuntime globalRewind;
    [Tooltip("全局三档按钮的父节点，初始建议设为隐藏。")]
    [SerializeField] private GameObject globalTierPanel;
    [SerializeField] private KeyCode localRewindTriggerKey = KeyCode.Q;
    [SerializeField] private KeyCode catalysisTriggerKey = KeyCode.E;
    [SerializeField] private Color catalysisPreviewColor = new Color(1f, 0.55f, 0.05f, 0.35f);
    [Header("催化预览")]
    [Tooltip("场景中已有的 CatalysisRangeWorldPreview 实例。若为空，会优先用下方 prefab 自动生成。")]
    [SerializeField] private Transform catalysisRangeWorldPreview;
    [Tooltip("CatalysisRangeWorldPreview prefab。用于避免克隆 localRewind 时继续沿用 RewindRangeWorldPreview。")]
    [SerializeField] private Transform catalysisRangeWorldPreviewPrefab;

    private void Awake()
    {
        EnsureCatalysisSkill();
    }

    private void EnsureCatalysisSkill()
    {
        if (localRewind != null)
            localRewind.SetKeyboardInputEnabled(false);

        bool createdFromLocalRewind = false;
        if (catalysisSkill == localRewind)
            catalysisSkill = null;

        if (catalysisSkill == null && localRewind != null)
        {
            catalysisSkill = Instantiate(localRewind, localRewind.transform.parent);
            catalysisSkill.name = "CatalysisSkill";
            createdFromLocalRewind = true;
        }

        if (catalysisSkill == null)
            return;

        Transform preview = ResolveCatalysisRangeWorldPreview();
        if (preview != null || createdFromLocalRewind)
            catalysisSkill.SetWorldRangePreview(preview);

        catalysisSkill.ConfigureAsCatalysisPreview(KeyCode.None, catalysisPreviewColor);
        catalysisSkill.SetKeyboardInputEnabled(false);
    }

    private Transform ResolveCatalysisRangeWorldPreview()
    {
        if (catalysisRangeWorldPreview != null)
            return catalysisRangeWorldPreview;

        GameObject scenePreview = GameObject.Find("CatalysisRangeWorldPreview");
        if (scenePreview != null)
        {
            catalysisRangeWorldPreview = scenePreview.transform;
            return catalysisRangeWorldPreview;
        }

        if (catalysisRangeWorldPreviewPrefab == null)
            return null;

        catalysisRangeWorldPreview = Instantiate(catalysisRangeWorldPreviewPrefab);
        catalysisRangeWorldPreview.name = catalysisRangeWorldPreviewPrefab.name;
        return catalysisRangeWorldPreview;
    }

    private void Update()
    {
        if (localRewindTriggerKey != KeyCode.None && Input.GetKeyDown(localRewindTriggerKey))
            OpenLocalRewindPreview();

        if (catalysisTriggerKey != KeyCode.None && Input.GetKeyDown(catalysisTriggerKey))
            OpenCatalysisPreview();
    }

    public void OpenLocalRewindPreview()
    {
        if (localRewind == null)
            return;
        localRewind.EnterRewindPreview();
    }

    public void OpenCatalysisPreview()
    {
        if (catalysisSkill == null)
            return;
        catalysisSkill.EnterCatalysisPreview();
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
