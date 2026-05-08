using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SettingsPanelUI : MonoBehaviour
{
    private const string MasterOnText = "音量：开";
    private const string MasterOffText = "音量：关";
    private const string SfxOnText = "音效：开";
    private const string SfxOffText = "音效：关";

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private ToolUIData toolUIData;

    [Header("Labels")]
    [SerializeField] private TMP_Text masterVolumeLabel;
    [SerializeField] private TMP_Text sfxLabel;

    [Header("Effect Hooks")]
    public UnityEvent<bool> onMasterVolumeChanged = new();
    public UnityEvent<bool> onSfxChanged = new();

    private bool fallbackMasterMuted;
    private bool fallbackSfxMuted;

    public bool IsMasterVolumeEnabled => !MasterMuted;
    public bool IsSfxEnabled => !SfxMuted;

    private bool MasterMuted
    {
        get => AudioManager.Instance != null ? AudioManager.Instance.IsMasterMuted : toolUIData != null ? toolUIData.masterMuted : fallbackMasterMuted;
        set
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterMuted(value);
                return;
            }

            if (toolUIData != null)
                toolUIData.masterMuted = value;
            else
                fallbackMasterMuted = value;
        }
    }

    private bool SfxMuted
    {
        get => AudioManager.Instance != null ? AudioManager.Instance.IsSfxMuted : toolUIData != null ? toolUIData.sfxMuted : fallbackSfxMuted;
        set
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxMuted(value);
                return;
            }

            if (toolUIData != null)
                toolUIData.sfxMuted = value;
            else
                fallbackSfxMuted = value;
        }
    }

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;
    }

    private void OnEnable()
    {
        RefreshState();
    }

    public void ShowPanel()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        panelRoot.SetActive(true);
        RefreshState();
    }

    public void HidePanel()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        panelRoot.SetActive(false);
    }

    public void SetMasterVolumeEnabled(bool enabled)
    {
        MasterMuted = !enabled;
        RefreshState();
        onMasterVolumeChanged.Invoke(enabled);
    }

    public void ToggleMasterVolume()
    {
        SetMasterVolumeEnabled(MasterMuted);
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxMuted = !enabled;
        RefreshState();
        onSfxChanged.Invoke(enabled);
    }

    public void ToggleSfx()
    {
        SetSfxEnabled(SfxMuted);
    }

    public void RefreshState()
    {
        if (masterVolumeLabel != null)
            masterVolumeLabel.text = MasterMuted ? MasterOffText : MasterOnText;
        if (sfxLabel != null)
            sfxLabel.text = SfxMuted ? SfxOffText : SfxOnText;
    }
}