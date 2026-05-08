using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
            masterVolumeSlider.value = toolUIData.masterVolumeDate;
            musicVolumeSlider.value = toolUIData.musicVolumeDate;
            SFXVolumeSlider.value = toolUIData.SFXVolumeDate;
        }
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        SFXVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
      
    }
    void Start()
    {

    }
    private void SetMasterVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
        if (toolUIData != null)
        {
            toolUIData.masterVolumeDate = masterVolumeSlider.value;
        }
    }
    private void SetMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(volume);
        if (toolUIData != null)
            toolUIData.musicVolumeDate = musicVolumeSlider.value;
    }

    private void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(volume);
        if (toolUIData != null)
            toolUIData.SFXVolumeDate = SFXVolumeSlider.value;
    }
    private void OnDestroy()
    {
        masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        SFXVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

}
