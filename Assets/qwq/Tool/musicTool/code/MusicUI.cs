using UnityEngine;
using UnityEngine.UI;

public class MusicUI : BaseUI 
{
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider SFXVolumeSlider;
    public ToolUIData toolUIData;

    public void OnEnable()
    {
        if (toolUIData != null)
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = toolUIData.masterVolumeDate;
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = toolUIData.musicVolumeDate;
            if (SFXVolumeSlider != null)
                SFXVolumeSlider.value = toolUIData.SFXVolumeDate;
        }

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (SFXVolumeSlider != null)
            SFXVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    void Start()
    {
        if (AudioManager.Instance != null && toolUIData != null)
        {
            AudioManager.Instance.SetMasterVolume(toolUIData.masterVolumeDate);
            AudioManager.Instance.SetMusicVolume(toolUIData.musicVolumeDate);
            AudioManager.Instance.SetSFXVolume(toolUIData.SFXVolumeDate);
            AudioManager.Instance.SetMasterMuted(toolUIData.masterMuted);
            AudioManager.Instance.SetSfxMuted(toolUIData.sfxMuted);
        }
    }

    private void SetMasterVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
        if (toolUIData != null)
        {
            toolUIData.masterVolumeDate = volume;
        }
    }

    private void SetMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(volume);
        if (toolUIData != null)
            toolUIData.musicVolumeDate = volume;
    }

    private void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(volume);
        if (toolUIData != null)
            toolUIData.SFXVolumeDate = volume;
    }

    private void OnDisable()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (SFXVolumeSlider != null)
            SFXVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    private void OnDestroy()
    {
        OnDisable();
    }
}
