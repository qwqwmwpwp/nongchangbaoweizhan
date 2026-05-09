using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource ambientSource;
    public AudioSource uiSource;

    [Header("UI Click Sound")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private bool autoBindButtonClickSounds = true;
    [SerializeField] private bool includeInactiveButtons = true;

    [Header("Skill Sounds")]
    [SerializeField] private AudioClip rewindSoundClip;
    [SerializeField] private AudioClip speedUpSoundClip;

    [Header("Combat Sounds")]
    [SerializeField] private AudioClip meleeAttackSoundClip;

    [Header("Game Result Sounds")]
    [SerializeField] private AudioClip victorySoundClip;
    [SerializeField] private AudioClip defeatSoundClip;

    public ToolUIData toolUIData;

    private const string MASTER_VOLUME = "MasterVolume";
    private const string MUSIC_VOLUME = "MusicVolume";
    private const string SFX_VOLUME = "SFXVolume";

    private readonly HashSet<Button> buttonClickSoundButtons = new HashSet<Button>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (toolUIData != null)
        {
            SetMasterVolume(toolUIData.masterVolumeDate);
            SetMusicVolume(toolUIData.musicVolumeDate);
            SetSFXVolume(toolUIData.SFXVolumeDate);
        }

        BindButtonClickSoundsInScene();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindButtonClickSounds();
        Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoBindButtonClickSounds) return;

        StartCoroutine(BindButtonClickSoundsNextFrame());
    }

    private IEnumerator BindButtonClickSoundsNextFrame()
    {
        yield return null;
        BindButtonClickSoundsInScene();
    }

    public void SetMasterVolume(float volume)
    {
        SetMixerVolume(MASTER_VOLUME, volume);
    }

    public void SetMusicVolume(float volume)
    {
        SetMixerVolume(MUSIC_VOLUME, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetMixerVolume(SFX_VOLUME, volume);
    }

    private void SetMixerVolume(string parameterName, float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("AudioManager: audioMixer is missing.");
            return;
        }

        audioMixer.SetFloat(parameterName, ConvertToDecibel(volume));
    }

    private float ConvertToDecibel(float volume)
    {
        return volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: tried to play a null music clip.");
            return;
        }

        if (musicSource == null)
        {
            Debug.LogWarning("AudioManager: musicSource is missing.");
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PauseMusic()
    {
        if (musicSource != null)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
            musicSource.UnPause();
    }

    public void FadeOutMusic(float duration)
    {
        if (musicSource != null)
            StartCoroutine(FadeAudioSource(musicSource, duration, 0f));
    }

    public void FadeInMusic(float duration, float targetVolume)
    {
        if (musicSource != null)
            StartCoroutine(FadeAudioSource(musicSource, duration, targetVolume));
    }

    public void PlayButtonClickSound()
    {
        PlayUISound(buttonClickClip);
    }

    public void PlayRewindSound()
    {
        PlayUISound(rewindSoundClip);
    }

    public void PlaySpeedUpSound()
    {
        PlayUISound(speedUpSoundClip);
    }

    public void PlayMeleeAttackSound()
    {
        PlayUISound(meleeAttackSoundClip);
    }

    public void PlayVictorySound()
    {
        PlayUISound(victorySoundClip);
    }

    public void PlayDefeatSound()
    {
        PlayUISound(defeatSoundClip);
    }

    public void BindButtonClickSound(Button button)
    {
        if (button == null) return;

        buttonClickSoundButtons.RemoveWhere(boundButton => boundButton == null);
        if (buttonClickSoundButtons.Contains(button)) return;

        button.onClick.AddListener(PlayButtonClickSound);
        buttonClickSoundButtons.Add(button);
    }

    public void BindButtonClickSoundsInScene()
    {
        if (!autoBindButtonClickSounds) return;

        Button[] buttons = FindObjectsOfType<Button>(includeInactiveButtons);
        foreach (Button button in buttons)
        {
            BindButtonClickSound(button);
        }
    }

    private void UnbindButtonClickSounds()
    {
        foreach (Button button in buttonClickSoundButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayButtonClickSound);
        }

        buttonClickSoundButtons.Clear();
    }

    public void PlayUISound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: tried to play a null UI sound clip.");
            return;
        }

        if (uiSource == null)
        {
            Debug.LogWarning("AudioManager: uiSource is missing.");
            return;
        }

        uiSource.PlayOneShot(clip);
    }

    public void PlayAmbientSound(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: tried to play a null ambient clip.");
            return;
        }

        if (ambientSource == null)
        {
            Debug.LogWarning("AudioManager: ambientSource is missing.");
            return;
        }

        ambientSource.clip = clip;
        ambientSource.loop = loop;
        ambientSource.Play();
    }

    private IEnumerator FadeAudioSource(AudioSource source, float duration, float targetVolume)
    {
        float startVolume = source.volume;
        float timer = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (timer < safeDuration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, timer / safeDuration);
            yield return null;
        }

        source.volume = targetVolume;
    }
}
