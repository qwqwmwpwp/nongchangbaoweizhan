using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Unity音频管理器 - 简化版但详细注释
/// 功能：音乐播放、音效控制、音量调节、淡入淡出效果
/// 设计模式：单例模式（全局唯一实例）
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const float MutedVolumeDb = -80f;

    // 单例实例 - 全局访问点
    public static AudioManager Instance { get; private set; }

    [Header("音频混合器配置")]
    [Tooltip("用于控制不同音频组的音量，需在Unity编辑器中分配")]
    public AudioMixer audioMixer;

    [Header("音频源组件")]
    [Tooltip("用于播放背景音乐的音频源")]
    public AudioSource musicSource;

    [Tooltip("用于播放环境音效的音频源")]
    public AudioSource ambientSource;

    [Tooltip("用于播放UI音效的音频源")]
    public AudioSource uiSource;

    // 音频混合器参数常量
    private const string MASTER_VOLUME = "MasterVolume";
    private const string MUSIC_VOLUME = "MusicVolume";
    private const string SFX_VOLUME = "SFXVolume";

    public ToolUIData toolUIData;

    private float fallbackMasterVolume = 1f;
    private float fallbackMusicVolume = 1f;
    private float fallbackSfxVolume = 1f;
    private bool fallbackMasterMuted;
    private bool fallbackSfxMuted;

    public bool IsMasterMuted => toolUIData != null ? toolUIData.masterMuted : fallbackMasterMuted;
    public bool IsSfxMuted => toolUIData != null ? toolUIData.sfxMuted : fallbackSfxMuted;

    /// <summary>
    /// 初始化方法 - 在对象创建时调用
    /// </summary>
    private void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            // 首次创建实例
            Instance = this;

            // 跨场景保留此对象
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 如果已存在实例，销毁新创建的对象
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ApplyStoredVolumes();
    }

    #region 音量控制功能
    /// <summary>
    /// 设置主音量（影响所有音频）
    /// </summary>
    /// <param name="volume">音量值（0.0静音 - 1.0最大）</param>
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (toolUIData != null)
            toolUIData.masterVolumeDate = volume;
        else
            fallbackMasterVolume = volume;

        ApplyMasterVolume();
    }

    /// <summary>
    /// 设置音乐音量（仅影响背景音乐）
    /// </summary>
    /// <param name="volume">音量值（0.0静音 - 1.0最大）</param>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (toolUIData != null)
            toolUIData.musicVolumeDate = volume;
        else
            fallbackMusicVolume = volume;

        SetMixerFloat(MUSIC_VOLUME, ConvertToDecibel(volume));
    }

    /// <summary>
    /// 设置音效音量（影响UI音效和环境音效）
    /// </summary>
    /// <param name="volume">音量值（0.0静音 - 1.0最大）</param>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (toolUIData != null)
            toolUIData.SFXVolumeDate = volume;
        else
            fallbackSfxVolume = volume;

        ApplySfxVolume();
    }

    public void SetMasterMuted(bool muted)
    {
        if (toolUIData != null)
            toolUIData.masterMuted = muted;
        else
            fallbackMasterMuted = muted;

        ApplyMasterVolume();
    }

    public void SetSfxMuted(bool muted)
    {
        if (toolUIData != null)
            toolUIData.sfxMuted = muted;
        else
            fallbackSfxMuted = muted;

        ApplySfxVolume();
    }

    public bool ToggleMasterMuted()
    {
        bool muted = !IsMasterMuted;
        SetMasterMuted(muted);
        return !muted;
    }

    public bool ToggleSfxMuted()
    {
        bool muted = !IsSfxMuted;
        SetSfxMuted(muted);
        return !muted;
    }

    /// <summary>
    /// 将线性音量值转换为分贝值（dB）
    /// 音频工程标准转换公式
    /// </summary>
    /// <param name="volume">线性音量值（0.0-1.0）</param>
    /// <returns>对应的分贝值</returns>
    private float ConvertToDecibel(float volume)
    {
        // 当音量接近0时返回-80dB（静音）
        // 否则使用对数公式转换：dB = 20 * log10(volume)
        return volume <= 0.0001f ? MutedVolumeDb : Mathf.Log10(volume) * 20f;
    }

    private void ApplyStoredVolumes()
    {
        ApplyMasterVolume();
        SetMixerFloat(MUSIC_VOLUME, ConvertToDecibel(CurrentMusicVolume));
        ApplySfxVolume();
    }

    private void ApplyMasterVolume()
    {
        SetMixerFloat(MASTER_VOLUME, IsMasterMuted ? MutedVolumeDb : ConvertToDecibel(CurrentMasterVolume));
    }

    private void ApplySfxVolume()
    {
        SetMixerFloat(SFX_VOLUME, IsSfxMuted ? MutedVolumeDb : ConvertToDecibel(CurrentSfxVolume));
    }

    private float CurrentMasterVolume => Mathf.Clamp01(toolUIData != null ? toolUIData.masterVolumeDate : fallbackMasterVolume);
    private float CurrentMusicVolume => Mathf.Clamp01(toolUIData != null ? toolUIData.musicVolumeDate : fallbackMusicVolume);
    private float CurrentSfxVolume => Mathf.Clamp01(toolUIData != null ? toolUIData.SFXVolumeDate : fallbackSfxVolume);

    private void SetMixerFloat(string parameterName, float value)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("AudioManager: 未指定 AudioMixer，无法设置音量。", this);
            return;
        }

        audioMixer.SetFloat(parameterName, value);
    }
    #endregion

    #region 音乐控制功能
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="clip">要播放的音乐剪辑</param>
    /// <param name="loop">是否循环播放（默认true）</param>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        // 安全检查：确保音乐剪辑不为空
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: 尝试播放空音乐剪辑");
            return;
        }

        // 设置音乐源属性并播放
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    /// <summary>
    /// 停止当前播放的音乐
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
    }

    /// <summary>
    /// 暂停当前播放的音乐
    /// </summary>
    public void PauseMusic()
    {
        musicSource.Pause();
    }

    /// <summary>
    /// 恢复暂停的音乐
    /// </summary>
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    /// <summary>
    /// 淡出当前音乐（音量逐渐降低至0）
    /// </summary>
    /// <param name="duration">淡出持续时间（秒）</param>
    public void FadeOutMusic(float duration)
    {
        // 启动协程实现淡出效果
        StartCoroutine(FadeAudioSource(musicSource, duration, 0f));
    }

    /// <summary>
    /// 淡入音乐（音量从0逐渐增加到目标值）
    /// </summary>
    /// <param name="duration">淡入持续时间（秒）</param>
    /// <param name="targetVolume">目标音量值（0.0-1.0）</param>
    public void FadeInMusic(float duration, float targetVolume)
    {
        // 启动协程实现淡入效果
        StartCoroutine(FadeAudioSource(musicSource, duration, targetVolume));
    }
    #endregion

    #region 音效控制功能
    /// <summary>
    /// 播放UI音效（如按钮点击）
    /// </summary>
    /// <param name="clip">音效剪辑</param>
    public void PlayUISound(AudioClip clip)
    {
        // 安全检查：确保音效剪辑不为空
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: 尝试播放空UI音效剪辑");
            return;
        }
        // 使用PlayOneShot播放音效（可同时播放多个）
        uiSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 播放环境音效（如风声、雨声）
    /// </summary>
    /// <param name="clip">音效剪辑</param>
    /// <param name="loop">是否循环播放（默认true）</param>

    public void PlayAmbientSound(AudioClip clip, bool loop = true)
    {
        // 安全检查：确保音效剪辑不为空
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: 尝试播放空环境音效剪辑");
            return;
        }

        // 设置环境音源属性并播放
        ambientSource.clip = clip;
        ambientSource.loop = loop;
        ambientSource.Play();
    }
    #endregion

    #region 协程方法（内部使用）
    /// <summary>
    /// 音频淡入淡出协程
    /// 实现音量平滑过渡效果
    /// </summary>
    /// <param name="source">目标音频源</param>
    /// <param name="duration">过渡持续时间（秒）</param>
    /// <param name="targetVolume">目标音量值</param>
    private IEnumerator FadeAudioSource(AudioSource source, float duration, float targetVolume)
    {
        // 记录初始音量
        float startVolume = source.volume;
        // 计时器
        float timer = 0f;
        // 在持续时间内逐渐改变音量
        while (timer < duration)
        {
            // 更新计时器（基于帧时间）
            timer += Time.deltaTime;
            // 线性插值计算当前音量
            source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);

            // 等待下一帧
            yield return null;
        }
        // 确保最终音量精确设置为目标值
        source.volume = targetVolume;
    }
    #endregion
}
